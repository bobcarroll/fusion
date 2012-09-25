/**
 * Fusion - package management system for Windows
 * Copyright (c) 2010 Bob Carroll
 * 
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

using Microsoft.Win32.SafeHandles;

using log4net;

using FileTuple = System.Tuple<string, Fusion.Framework.FileType, string>;

namespace Fusion.Framework
{
    /// <summary>
    /// Performs merge operations on the system.
    /// </summary>
    public sealed class MergeWorker
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        private static ILog _log = LogManager.GetLogger(typeof(MergeWorker));

        private IPackageManager _pkgmgr;
        private XmlConfiguration _cfg;

        /// <summary>
        /// Event raised when auto-cleaning of packages begins.
        /// </summary>
        public event EventHandler<EventArgs> OnAutoClean;

        /// <summary>
        /// Event raised for each package at the start of installation of the files.
        /// </summary>
        public event EventHandler<MergeEventArgs> OnInstall;

        /// <summary>
        /// Event raised when parallel fetching begins.
        /// </summary>
        public event EventHandler<EventArgs> OnParallelFetch;

        /// <summary>
        /// Event raised for each package during a pretend merge.
        /// </summary>
        public event EventHandler<MergeEventArgs> OnPretendMerge;

        /// <summary>
        /// Event raised for each package at the start of a merge.
        /// </summary>
        public event EventHandler<MergeEventArgs> OnRealMerge;

        /// <summary>
        /// Event raised for each package at the start of unmerge.
        /// </summary>
        public event EventHandler<UnmergeEventArgs> OnUnmerge;

        /// <summary>
        /// Initialises the merge worker.
        /// </summary>
        /// <param name="pkgmgr">package manager instance</param>
        public MergeWorker(IPackageManager pkgmgr)
        {
            _pkgmgr = pkgmgr;
            _cfg = XmlConfiguration.LoadSeries();
        }

        /// <summary>
        /// Determines if files in the image directory would collide with existing
        /// files owned by other packages.
        /// </summary>
        /// <param name="imgdir">image directory</param>
        /// <param name="atom">package atom to test</param>
        /// <returns>true if collisions are detected, false otherwise</returns>
        private bool DetectCollisions(DirectoryInfo imgdir, Atom atom)
        {
            int striplev = imgdir.FullName.TrimEnd('\\').Count(i => i == '\\') + 1;
            string[] files = imgdir
                .GetFileSystemInfos("*", SearchOption.AllDirectories)
                .Where(i => (i.Attributes & FileAttributes.Directory) != FileAttributes.Directory)
                .Select(i => String.Join("\\", i.FullName.Split('\\').Skip(striplev)))
                .Select(i => _cfg.RootDir.FullName.TrimEnd('\\') + @"\" + i)
                .ToArray();
            string[] collisions = _pkgmgr.CheckFilesOwner(files, atom);

            foreach (string file in collisions)
                _log.ErrorFormat("File already owned: {0}", file);

            return (collisions.Length > 0);
        }

        /// <summary>
        /// Installs the package files into the system.
        /// </summary>
        /// <param name="imgdir">files directory</param>
        /// <param name="rootdir">root directory</param>
        /// <param name="installer">installer project</param>
        private void InstallImage(DirectoryInfo imgdir, DirectoryInfo rootdir, IInstallProject installer,
            out FileTuple[] created)
        {
            List<FileTuple> results = new List<FileTuple>();

            installer.PkgPreInst();

            int striplev = imgdir.FullName.TrimEnd('\\').Count(i => i == '\\') + 1;
            IEnumerable<FileSystemInfo> fsienum = imgdir
                .GetFileSystemInfos("*", SearchOption.AllDirectories)
                .OrderBy(i => i.FullName);

            foreach (FileSystemInfo fsi in fsienum) {
                string relpath = String.Join("\\", fsi.FullName.Split('\\').Skip(striplev));
                string targetpath = rootdir.FullName.TrimEnd('\\') + @"\" + relpath;
                FileType type = FileType.RegularFile;
                string digest = null;

                if (fsi.Attributes.HasFlag(FileAttributes.Directory))
                    type = FileType.Directory;
                else if (fsi.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    type = FileType.SymbolicLink;

                if (type == FileType.Directory) {
                    if (!Directory.Exists(targetpath)) {
                        Directory.CreateDirectory(targetpath);
                        _log.InfoFormat("Created new directory: {0}", targetpath);
                    }
                } else {
                    _log.InfoFormat("Copying file to {0}", targetpath);
                    File.Copy(fsi.FullName, targetpath, true);
                    digest = Md5Sum.Compute(targetpath, Md5Sum.MD5SUMMODE.BINARY);
                }

                results.Add(new FileTuple(targetpath, type, digest));
            }

            installer.PkgPostInst();
            created = results.ToArray();
        }

        /// <summary>
        /// Merges the given distributions into the system.
        /// </summary>
        /// <param name="distarr">the distributions to merge</param>
        public void Merge(IDistribution[] distarr)
        {
            this.Merge(distarr, 0);
        }

        /// <summary>
        /// Merges the given distributions into the system.
        /// </summary>
        /// <param name="distarr">the distributions to merge</param>
        /// <param name="mopts">merge option flags</param>
        public void Merge(IDistribution[] distarr, MergeOptions mopts)
        {
            if (distarr.Length == 0)
                return;

            Downloader downloader = new Downloader(_cfg.DistFilesDir);
            List<MergeEventArgs> scheduled = null;

            this.ScheduleMerges(distarr, mopts, downloader, out scheduled);

            if (this.OnParallelFetch != null)
                this.OnParallelFetch.Invoke(this, new EventArgs());

            if (!mopts.HasFlag(MergeOptions.Pretend))
                downloader.FetchAsync();

            for (int i = 0; i < scheduled.Count; i++) {
                MergeEventArgs mea = scheduled[i];
                mea.CurrentIter = i + 1;
                mea.TotalMerges = scheduled.Count;

                this.MergeOne(mea, mopts, downloader, _pkgmgr.RootDir);
            }

            if (this.OnAutoClean != null)
                this.OnAutoClean.Invoke(this, new EventArgs());

            TrashWorker.Purge(_pkgmgr);
        }

        /// <summary>
        /// Merge a single package into the system.
        /// </summary>
        /// <param name="mea">merge event arguments</param>
        /// <param name="mopts">merge options</param>
        /// <param name="downloader">the downloader</param>
        /// <param name="rootdir">root directory</param>
        private void MergeOne(MergeEventArgs mea, MergeOptions mopts, Downloader downloader, DirectoryInfo rootdir)
        {
            IDistribution dist = mea.Distribution;
            uint rc = 0;

            if (mopts.HasFlag(MergeOptions.Pretend)) {
                if (this.OnPretendMerge != null)
                    this.OnPretendMerge.Invoke(this, mea);

                return;
            }

            if (this.OnRealMerge != null)
                this.OnRealMerge.Invoke(this, mea);

            if (dist.Sources.Length > 0) {
                _log.Info("Fetching files in the background... please wait");
                _log.InfoFormat("See {0} for fetch progress", downloader.LogFile);
                downloader.WaitFor(mea.FetchHandle);

                _log.InfoFormat("Checking package digests");

                foreach (SourceFile src in dist.Sources) {
                    FileInfo distfile = new FileInfo(_cfg.DistFilesDir + @"\" + src.LocalName);

                    if (!Md5Sum.Check(distfile.FullName, src.Digest, Md5Sum.MD5SUMMODE.BINARY)) {
                        _log.ErrorFormat("Digest check failed for {0}", distfile.FullName);
                        throw new InstallException("Computed digest doesn't match expected value.");
                    }
                }
            }

            if (mopts.HasFlag(MergeOptions.FetchOnly))
                return;

            SandboxDirectory sbox = SandboxDirectory.Create();
            _log.DebugFormat("Created sandbox directory: {0}", sbox.Root.FullName);

            IInstallProject installer = dist.GetInstallProject(sbox);
            if (installer == null)
                throw new InstallException("Encountered missing or invalid installer project.");

            if ((rc = this.SpawnXtMake(sbox, installer)) != 0) {
                _log.DebugFormat("xtmake return code: {0}", rc);
                throw new InstallException("Installation failed. See previous errors.");
            }

            if (this.OnInstall != null)
                this.OnInstall.Invoke(this, mea);

            if (_cfg.CollisionDetect && this.DetectCollisions(sbox.ImageDir, dist.Atom))
                throw new InstallException("File collision(s) were detected.");

            FileTuple[] files = null;

            if (mea.Previous != null) {
                _log.Debug("Trashing files from previous installation");
                files = _pkgmgr.QueryPackageFiles(mea.Previous);

                foreach (FileTuple ft in files)
                    TrashWorker.AddFile(ft.Item1, _pkgmgr);
            }

            this.InstallImage(sbox.ImageDir, rootdir, installer, out files);

            MemoryStream ms = new MemoryStream();
            (new BinaryFormatter()).Serialize(ms, installer);
            string installerstr = Convert.ToBase64String(ms.ToArray());
            ms.Close();

            Dictionary<string, string> metadata = new Dictionary<string, string>();
            metadata.Add("repository", dist.PortsTree.Repository);

            if (mea.Selected)
                _log.InfoFormat("Recording {0} in world favourites", dist.Package.FullName);
            _pkgmgr.RecordPackage(dist, installerstr, files, metadata.ToArray(), mea.Selected);

            _log.Debug("Destroying the sandbox...");
            sbox.Delete();
        }

        /// <summary>
        /// Determines the packages needed for merging, including dependencies if necessary.
        /// </summary>
        /// <param name="distarr">packages selected for merging</param>
        /// <param name="mopts">merge options</param>
        /// <param name="downloader">the downloader</param>
        /// <param name="scheduled">output list of packages scheduled for merge</param>
        private void ScheduleMerges(IDistribution[] distarr, MergeOptions mopts, Downloader downloader, 
            out List<MergeEventArgs> scheduled)
        {
            scheduled = new List<MergeEventArgs>();

            DependencyGraph dg = DependencyGraph.Compute(distarr);
            IDistribution[] distdeparr = dg.SortedNodes.ToArray();

            IDistribution masked =
                distdeparr.Where(i => i.PortsTree.IsMasked(i)).FirstOrDefault();
            if (masked != null)
                throw new MaskedPackageException(masked.Package.FullName);

            IDistribution[] conflicts = distdeparr
                .Where(d => distdeparr.Where(
                    dd => d.Package.FullName == dd.Package.FullName && d.Slot == dd.Slot).Count() > 1)
                .ToArray();
            if (conflicts.Length > 0)
                throw new SlotConflictException(conflicts);

            for (int i = 0; i < distdeparr.Length; i++) {
                IDistribution dist = distdeparr[i];
                Atom current = _pkgmgr
                    .FindPackages(Atom.Parse(dist.Package.FullName, AtomParseOptions.WithoutVersion))
                    .Where(n => n.Slot == dist.Slot)
                    .SingleOrDefault();

                MergeEventArgs mea = new MergeEventArgs();
                mea.Previous = current;
                mea.Selected = distarr.Contains(dist);
                mea.Distribution = dist;
                mea.FetchOnly = mopts.HasFlag(MergeOptions.FetchOnly);

                if (current == null)
                    mea.Flags |= MergeFlags.New;
                if (!mea.Flags.HasFlag(MergeFlags.New) && current.Version.CompareTo(dist.Version) == 0)
                    mea.Flags |= MergeFlags.Replacing;
                if (!mea.Flags.HasFlag(MergeFlags.New) && !mea.Flags.HasFlag(MergeFlags.Replacing))
                    mea.Flags |= MergeFlags.Updating;
                if (!mea.Flags.HasFlag(MergeFlags.New) && current.Version.CompareTo(dist.Version) > 0)
                    mea.Flags |= MergeFlags.Downgrading;
                if (dist.Slot > 0)
                    mea.Flags |= MergeFlags.Slot;
                if (dist.Interactive)
                    mea.Flags |= MergeFlags.Interactive;
                /* TODO block */

                if (dist.FetchRestriction && Distribution.CheckSourcesExist(dist, _cfg.DistFilesDir))
                    mea.Flags |= MergeFlags.FetchExists;
                else if (dist.FetchRestriction)
                    mea.Flags |= MergeFlags.FetchNeeded;

                if (mea.Flags.HasFlag(MergeFlags.Replacing) && 
                        (!mea.Selected || mopts.HasFlag(MergeOptions.NoReplace)) && !mopts.HasFlag(MergeOptions.EmptyTree))
                    continue;

                if (mea.Flags.HasFlag(MergeFlags.FetchNeeded) && !mopts.HasFlag(MergeOptions.Pretend)) {
                    throw new InstallException("Fetch restriction is enabled for " + dist.ToString()
                        + "\nCopy the package archive into " + _cfg.DistFilesDir);
                }

                mea.FetchHandle = downloader.Enqueue(dist);
                scheduled.Add(mea);
            }
        }

        /// <summary>
        /// Launches the unprivileged xtmake process with the givne installer.
        /// </summary>
        /// <param name="sboxdir">sandbox directory</param>
        /// <param name="installer">installer project</param>
        /// <returns>xtmake exit code</returns>
        private uint SpawnXtMake(SandboxDirectory sbox, IInstallProject installer)
        {
            string installerbin = sbox.Root.FullName + @"\installer.bin";
            Stream stream = new FileStream(installerbin, FileMode.Create, FileAccess.Write, FileShare.Read);
            (new BinaryFormatter()).Serialize(stream, installer);
            stream.Close();

            StringBuilder sb = new StringBuilder();
            sb.Append(XmlConfiguration.BinDir + @"\xtmake.exe"); // TODO
            sb.Append(" ");
            sb.Append(installerbin);

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = XmlConfiguration.BinDir + @"\sudont.exe"; // TODO
            psi.Arguments = sb.ToString();

            _log.DebugFormat("Spawning low-privileged process: {0}", psi.Arguments);
            Process launcher = Process.Start(psi);
            launcher.WaitForExit();

            if (launcher.ExitCode == 0)
                throw new InstallException("Failed to spawn installer process.");

            Process xtinstall = null;
            try {
                xtinstall = Process.GetProcessById(launcher.ExitCode);
            } catch (ArgumentException) {
                throw new InstallException("Failed to open handle to installer process.");
            }

            IntPtr prochandle = xtinstall.Handle;
            uint rc = 0;

            while (true) {
                if (xtinstall.HasExited) {
                    GetExitCodeProcess(prochandle, out rc);
                    break;
                }

                Thread.Sleep(1000);
            }

            return rc;
        }

        /// <summary>
        /// Unmerges the packages matching the given atoms.
        /// </summary>
        /// <param name="atomarr">package atoms for unmerging</param>
        public void Unmerge(Atom[] atomarr)
        {
            foreach (Atom atom in atomarr) {
                UnmergeEventArgs uae = new UnmergeEventArgs();
                uae.Package = atom;

                if (_pkgmgr.IsProtected(atom))
                    throw new ProtectedPackageException(atom.ToString());

                if (this.OnUnmerge != null)
                    this.OnUnmerge.Invoke(this, uae);

                IInstallProject installer = null;
                string instblob = _pkgmgr.GetPackageInstaller(atom);

                if (!String.IsNullOrEmpty(instblob)) {
                    byte[] buf = Convert.FromBase64String(instblob);

                    MemoryStream ms = new MemoryStream();
                    ms.Write(buf, 0, buf.Length);
                    ms.Seek(0, SeekOrigin.Begin);
                    installer = (IInstallProject)(new BinaryFormatter()).Deserialize(ms);
                    ms.Close();

                    installer.PkgPreRm();
                } else
                    _log.WarnFormat("Missing installer project for {0}", atom.ToString());

                FileTuple[] files = _pkgmgr.QueryPackageFiles(atom)
                    .OrderByDescending(i => i.Item1)
                    .ToArray();

                foreach (FileTuple ft in files) {
                    try {
                        if (ft.Item2 == FileType.Directory) {
                            if (Directory.GetFiles(ft.Item1).Length > 0)
                                throw new IOException();

                            Directory.Delete(ft.Item1);
                        } else
                            File.Delete(ft.Item1);

                        _log.InfoFormat("Delete file {0}", ft.Item1);
                    } catch {
                        TrashWorker.AddFile(ft.Item1, _pkgmgr);
                        _log.WarnFormat("Trash file {0}", ft.Item1);
                    }
                }

                if (installer != null)
                    installer.PkgPostRm();

                _pkgmgr.DeletePackage(atom);

                Atom worlatom = Atom.Parse(atom.PackageName, AtomParseOptions.WithoutVersion);
                if (_pkgmgr.FindPackages(worlatom).Length == 0)
                    _pkgmgr.DeselectPackage(worlatom);
            }

            if (this.OnAutoClean != null)
                this.OnAutoClean.Invoke(this, new EventArgs());

            TrashWorker.Purge(_pkgmgr);
        }
    }
}
