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

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool Wow64DisableWow64FsRedirection(out IntPtr OldValue);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool Wow64RevertWow64FsRedirection(IntPtr OldValue);

        private static ILog _log = LogManager.GetLogger(typeof(MergeWorker));

        private IPackageManager _pkgmgr;
        private Configuration _cfg;
        private string[] _protected;

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
            _cfg = Configuration.LoadSeries();
            _protected = this.GetSystemProtectedFiles();
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
            bool protect = false;

            foreach (string file in collisions)
                _log.ErrorFormat("File '{0}' is already owned by a package", file);

            foreach (string file in files) {
                if (this.IsProtected(file)) {
                    _log.ErrorFormat("File '{0}' is protected by the system profile", file);
                    protect = true;
                }
            }

            return (collisions.Length > 0 || protect);
        }

        /// <summary>
        /// Gets all protected system files and directories.
        /// </summary>
        /// <returns>system paths</returns>
        private string[] GetSystemProtectedFiles()
        {
            List<string> results = new List<string>();
            string root = 
                Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows));

            foreach (DirectoryInfo di in _cfg.ProfileTree) {
                FileInfo[] fiarr = di.GetFiles("system.protect").ToArray();
                if (fiarr.Length == 0)
                    continue;

                string[] lines = File.ReadAllLines(fiarr[0].FullName)
                    .Where(i => !String.IsNullOrWhiteSpace(i))
                    .Select(i => root + i.TrimStart('\\'))
                    .ToArray();
                results.AddRange(lines);
            }

            return results.OrderBy(i => i).ToArray();
        }

        /// <summary>
        /// Installs the package files into the system.
        /// </summary>
        /// <param name="imgdir">files directory</param>
        /// <param name="rootdir">root directory</param>
        /// <param name="created">output list of files created</param>
        private void InstallImage(DirectoryInfo imgdir, DirectoryInfo rootdir, out FileTuple[] created)
        {
            List<FileTuple> results = new List<FileTuple>();

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

                if (this.IsProtected(fsi.FullName) && type != FileType.Directory) {
                    _log.ErrorFormat("Refusing to overwrite system protected file!");
                    throw new UnauthorizedAccessException("File '" + fsi.FullName + "' is protected.");
                }

                if (type == FileType.Directory) {
                    if (!Directory.Exists(targetpath)) {
                        Directory.CreateDirectory(targetpath);
                        _log.InfoFormat("Created directory '{0}'", targetpath);
                    }
                } else {
                    File.Copy(fsi.FullName, targetpath, true);
                    digest = Md5Sum.Compute(targetpath, Md5Sum.MD5SUMMODE.BINARY);
                    _log.InfoFormat("Copied file to '{0}'", targetpath);
                }

                results.Add(new FileTuple(targetpath, type, digest));
            }

            created = results.ToArray();
        }

        /// <summary>
        /// Determines if the given path is protected.
        /// </summary>
        /// <param name="path">path to check</param>
        /// <returns>true if protected, false otherwise</returns>
        private bool IsProtected(string path)
        {
            return _protected
                .Select(i => i.TrimEnd('\\'))
                .Contains(path.TrimEnd('\\'));
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

            if (!mopts.HasFlag(MergeOptions.Pretend)) {
                if (this.OnParallelFetch != null)
                    this.OnParallelFetch.Invoke(this, new EventArgs());

                downloader.FetchAsync();
            }

            for (int i = 0; i < scheduled.Count; i++) {
                MergeEventArgs mea = scheduled[i];
                mea.CurrentIter = i + 1;
                mea.TotalMerges = scheduled.Count;

                this.MergeOne(mea, mopts, downloader, _pkgmgr.RootDir);
            }

            if (!mopts.HasFlag(MergeOptions.Pretend)) {
                if (this.OnAutoClean != null)
                    this.OnAutoClean.Invoke(this, new EventArgs());

                TrashWorker.Purge(_pkgmgr);
            }
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

            if (mea.HardMask || mea.KeywordMask)
                throw new MaskedPackageException(mea.Distribution.Package.FullName);

            if (dist.PlatformSources.Length > 0) {
                if (mea.FetchHandle != Guid.Empty && !downloader.Peek(mea.FetchHandle)) {
                    _log.Info("Fetching files in the background... please wait");
                    _log.InfoFormat("See {0} for fetch progress", downloader.LogFile);
                    downloader.WaitFor(mea.FetchHandle);
                }

                _log.InfoFormat("Checking package digests");

                foreach (SourceFile src in dist.PlatformSources) {
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

            bool runmake = installer.HasSrcUnpackTarget ||
                           installer.HasSrcCompileTarget ||
                           installer.HasSrcInstallTarget ||
                           installer.HasSrcTestTarget;
            if (runmake && (rc = this.SpawnSandboxHost(sbox, installer)) != 0) {
                _log.DebugFormat("sandbox host return code: {0}", rc);
                throw new InstallException("Installation failed. See previous errors.");
            }

            if (this.OnInstall != null)
                this.OnInstall.Invoke(this, mea);

            if (_cfg.CollisionDetect && this.DetectCollisions(sbox.ImageDir, dist.Atom))
                throw new InstallException("File collision(s) were detected.");

            if (mea.Previous != null) {
                _log.Debug("Trashing files from previous installation");
                FileTuple[] oldfiles = _pkgmgr.QueryPackageFiles(mea.Previous);

                foreach (FileTuple ft in oldfiles) {
                    if (ft.Item2 != FileType.Directory || !this.IsProtected(ft.Item1))
                        TrashWorker.AddFile(ft.Item1, _pkgmgr);
                }
            }

            FileTuple[] files = null;
            FileTuple[] shortcuts = null;

            if (installer.HasPkgPreInstTarget) {
                _log.Info("Executing pre-install tasks...");
                installer.PkgPreInst();
            }

            IntPtr wow64oldval = IntPtr.Zero;
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess) {
                if (!Wow64DisableWow64FsRedirection(out wow64oldval))
                    throw new InstallException("Failed to disable Wow64 file system redirection.");
            }

            try {
                this.InstallImage(sbox.ImageDir, rootdir, out files);
            } finally {
                if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
                    Wow64RevertWow64FsRedirection(wow64oldval);
            }

            this.InstallImage(
                sbox.LinkDir, 
                new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)), 
                out shortcuts);

            FileTuple[] allfiles = files.Union(shortcuts).ToArray();
            Dictionary<string, string> metadata = new Dictionary<string, string>();
            metadata.Add("repository", dist.PortsTree.Repository);
            _pkgmgr.RecordPackage(dist, installer, allfiles, metadata.ToArray());

            if (installer.HasPkgPostInstTarget) {
                _log.Info("Executing post-install tasks...");
                installer.PkgPostInst();
            }

            if (mea.Selected) {
                _log.InfoFormat("Recording {0} in world favourites", dist.Package.FullName);
                _pkgmgr.SelectPackage(dist.Atom);
            }

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

            DependencyGraph.Conflict[] conflicts = dg.FindSlotConflicts();
            if (conflicts.Length > 0)
                throw new SlotConflictException(conflicts);

            for (int i = 0; i < distdeparr.Length; i++) {
                IDistribution dist = distdeparr[i];
                Atom current = _pkgmgr
                    .FindPackages(Atom.Parse(dist.Package.FullName, AtomParseOptions.WithoutVersion))
                    .Where(n => n.Slot == dist.Slot)
                    .SingleOrDefault();
                bool selected = !mopts.HasFlag(MergeOptions.OneShot) && distarr.Contains(dist);

                if (!selected && current != null && !mopts.HasFlag(MergeOptions.Deep) && dg.CheckSatisfies(current))
                    dist = dist.PortsTree.Lookup(current);

                MergeEventArgs mea = new MergeEventArgs();
                mea.Previous = current;
                mea.Selected = selected;
                mea.Distribution = dist;
                mea.FetchOnly = mopts.HasFlag(MergeOptions.FetchOnly);

                mea.HardMask = dist.PortsTree.IsHardMasked(dist);
                mea.KeywordMask = dist.PortsTree.IsKeywordMasked(dist);

                mea.KeywordsNeeded = dist.Keywords
                    .Where(kw => _cfg.AcceptKeywords.Contains(kw) ||
                                 _cfg.AcceptKeywords.Contains(Distribution.GetKeywordName(kw)))
                    .ToArray();

                if (!mopts.HasFlag(MergeOptions.Pretend) && (mea.HardMask || mea.KeywordMask))
                    throw new MaskedPackageException(dist.Package.FullName);

                if (current == null)
                    mea.Flags |= MergeFlags.New;
                if (!mea.Flags.HasFlag(MergeFlags.New) && current.Version == dist.Version)
                    mea.Flags |= MergeFlags.Replacing;
                if (!mea.Flags.HasFlag(MergeFlags.New) && !mea.Flags.HasFlag(MergeFlags.Replacing))
                    mea.Flags |= MergeFlags.Updating;
                if (!mea.Flags.HasFlag(MergeFlags.New) && current.Version > dist.Version)
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

                if (!(mea.Flags.HasFlag(MergeFlags.FetchExists) || mea.Flags.HasFlag(MergeFlags.FetchNeeded)))
                    mea.FetchHandle = downloader.Enqueue(dist);

                scheduled.Add(mea);
            }
        }

        /// <summary>
        /// Launches the unprivileged sandbox host with the given installer.
        /// </summary>
        /// <param name="sboxdir">sandbox directory</param>
        /// <param name="installer">installer project</param>
        /// <returns>sandbox host exit code</returns>
        private uint SpawnSandboxHost(SandboxDirectory sbox, IInstallProject installer)
        {
            string installerbin = sbox.Root.FullName + @"\installer.bin";
            Stream stream = new FileStream(installerbin, FileMode.Create, FileAccess.Write, FileShare.Read);
            (new BinaryFormatter()).Serialize(stream, installer);
            stream.Close();

            Security.AccessToken token = Security.CreateRestrictedToken(true);
            uint pid = Security.CreateProcessWithToken(
                token,
                Configuration.BinDir.FullName + @"\sandbox.exe",
                "\"" + installerbin + "\"");
            Security.FreeToken(token);
            if (pid == 0)
                throw new InstallException("Failed to spawn installer process.");

            Process xtinstall = null;
            try {
                xtinstall = Process.GetProcessById((int)pid);
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

                IInstallProject installer = _pkgmgr.GetPackageInstaller(atom);

                if (installer != null && installer.HasPkgPreRmTarget) {
                    _log.Info("Executing pre-removal tasks...");
                    installer.PkgPreRm();
                }

                FileTuple[] files = _pkgmgr.QueryPackageFiles(atom)
                    .OrderByDescending(i => i.Item1)
                    .ToArray();

                IntPtr wow64oldval = IntPtr.Zero;
                if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess) {
                    if (!Wow64DisableWow64FsRedirection(out wow64oldval))
                        throw new InstallException("Failed to disable Wow64 file system redirection.");
                }

                foreach (FileTuple ft in files) {
                    try {
                        if (ft.Item2 == FileType.Directory && this.IsProtected(ft.Item1)) {
                            _log.DebugFormat("Skipping protected directory '{0}'", ft.Item1);
                            continue;
                        }

                        if (ft.Item2 == FileType.Directory) {
                            if (Directory.GetFiles(ft.Item1).Length > 0)
                                throw new IOException();

                            Directory.Delete(ft.Item1);
                            _log.InfoFormat("Deleted directory '{0}'", ft.Item1);
                        } else {
                            File.Delete(ft.Item1);
                            _log.InfoFormat("Deleted file '{0}'", ft.Item1);
                        }
                    } catch (DirectoryNotFoundException) {
                        _log.DebugFormat("Skipping non-existant directory '{0}'", ft.Item1);
                    } catch (FileNotFoundException) {
                        _log.DebugFormat("Skipping non-existant file '{0}'", ft.Item1);
                    } catch {
                        TrashWorker.AddFile(ft.Item1, _pkgmgr);
                        _log.WarnFormat("Marked '{0}' for future removal", ft.Item1);
                    }
                }

                if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
                    Wow64RevertWow64FsRedirection(wow64oldval);

                if (installer != null && installer.HasPkgPostRmTarget) {
                    _log.Info("Executing post-removal tasks...");
                    installer.PkgPostRm();
                }

                _pkgmgr.DeletePackage(atom);

                Atom worldatom = Atom.Parse(atom.PackageName, AtomParseOptions.WithoutVersion);
                if (_pkgmgr.FindPackages(worldatom).Length == 0)
                    _pkgmgr.DeselectPackage(worldatom);
            }

            if (this.OnAutoClean != null)
                this.OnAutoClean.Invoke(this, new EventArgs());

            TrashWorker.Purge(_pkgmgr);
        }
    }
}
