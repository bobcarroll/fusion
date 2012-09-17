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
        /// Event raised for each package at the start of installation of the files.
        /// </summary>
        public event EventHandler<MergeEventArgs> OnInstall;

        /// <summary>
        /// Event raised when parallel fetching begins.
        /// </summary>
        public event EventHandler<EventArgs> OnParallelFetch;

        /// <summary>
        /// Event raised when there's a merge message.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnMergeMessage;

        /// <summary>
        /// Event raised for each package during a pretend merge.
        /// </summary>
        public event EventHandler<MergeEventArgs> OnPretendMerge;

        /// <summary>
        /// Event raised for each package at the start of a merge.
        /// </summary>
        public event EventHandler<MergeEventArgs> OnRealMerge;

        /// <summary>
        /// Initialises the merge worker.
        /// </summary>
        /// <param name="pkgmgr">package manager instance</param>
        /// <param name="cfg">ports configuration</param>
        public MergeWorker(IPackageManager pkgmgr, XmlConfiguration cfg)
        {
            _pkgmgr = pkgmgr;
            _cfg = cfg;
        }

        /// <summary>
        /// Merges the given distributions into the given zone.
        /// </summary>
        /// <param name="distarr">the distributions to merge</param>
        /// <param name="zone">ID of zone to merge into</param>
        public void Merge(IDistribution[] distarr, long zone)
        {
            this.Merge(distarr, zone, 0);
        }

        /// <summary>
        /// Merges the given distributions into the given zone.
        /// </summary>
        /// <param name="distarr">the distributions to merge</param>
        /// <param name="zone">ID of zone to merge into</param>
        /// <param name="mopts">merge option flags</param>
        public void Merge(IDistribution[] distarr, long zone, MergeOptions mopts)
        {
            if (distarr.Length == 0)
                return;

            string zonedir = _pkgmgr.QueryZonePrefix(zone);
            Downloader downloader = new Downloader(_cfg.DistFilesDir);
            List<MergeEventArgs> scheduled = null;

            this.ScheduleMerges(distarr, mopts, downloader, zone, out scheduled);

            if (this.OnParallelFetch != null)
                this.OnParallelFetch.Invoke(this, new EventArgs());

            downloader.FetchAsync();

            for (int i = 0; i < scheduled.Count; i++) {
                MergeEventArgs mea = scheduled[i];
                mea.CurrentIter = i + 1;
                mea.TotalMerges = scheduled.Count;

                this.MergeOne(mea, mopts, downloader, zonedir);
            }
        }

        /// <summary>
        /// Merge a single package into the given zone.
        /// </summary>
        /// <param name="mea">merge event arguments</param>
        /// <param name="mopts">merge options</param>
        /// <param name="downloader">the downloader</param>
        /// <param name="zonedir">zone prefix directory</param>
        private void MergeOne(MergeEventArgs mea, MergeOptions mopts, Downloader downloader, string zonedir)
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

            IInstallProject installer = dist.GetInstallProject();
            if (installer == null)
                throw new InstallException("Encountered missing or invalid installer project.");

            Guid sboxid = Guid.NewGuid();
            DirectoryInfo sboxdir = _cfg.TmpDir.CreateSubdirectory(sboxid.ToString());
            _log.DebugFormat("Created sandbox directory: {0}", sboxdir.FullName);

            if ((rc = this.SpawnXtMake(sboxdir, installer, zonedir)) != 0) {
                _log.DebugFormat("xtmake return code: {0}", rc);
                throw new InstallException("Installation failed. See previous errors.");
            }

            if (this.OnInstall != null)
                this.OnInstall.Invoke(this, mea);
        }

        /// <summary>
        /// Raises a merge message event if a handler is set.
        /// </summary>
        /// <param name="msg">the message</param>
        private void RaiseMergeMessage(string msg)
        {
            if (this.OnMergeMessage != null)
                this.OnMergeMessage.Invoke(this, new MessageEventArgs() { Message = msg });
        }

        /// <summary>
        /// Determines the packages needed for merging, including dependencies if necessary.
        /// </summary>
        /// <param name="distarr">packages selected for merging</param>
        /// <param name="mopts">merge options</param>
        /// <param name="downloader">the downloader</param>
        private void ScheduleMerges(IDistribution[] distarr, MergeOptions mopts, Downloader downloader, long zone, 
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
                    .FindPackages(new Atom(dist, true), zone)
                    .SingleOrDefault();

                MergeEventArgs mea = new MergeEventArgs();
                mea.Previous = current;
                mea.Selected = distarr.Contains(dist);
                mea.Distribution = dist;
                mea.FetchOnly = mopts.HasFlag(MergeOptions.FetchOnly);

                if (current == null || mopts.HasFlag(MergeOptions.EmptyTree))
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

                if (mea.Flags.HasFlag(MergeFlags.Replacing) && (!mea.Selected || mopts.HasFlag(MergeOptions.NoReplace)))
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
        /// <param name="sboxdir">sandbox root directory</param>
        /// <param name="installer">installer project</param>
        /// <param name="zonedir">zone prefix directory</param>
        /// <returns>xtmake exit code</returns>
        private uint SpawnXtMake(DirectoryInfo sboxdir, IInstallProject installer, string zonedir)
        {
            string installerbin = sboxdir + @"\installer.bin";
            Stream stream = new FileStream(installerbin, FileMode.Create, FileAccess.Write, FileShare.Read);
            (new BinaryFormatter()).Serialize(stream, installer);
            stream.Close();

            StringBuilder sb = new StringBuilder();
            sb.Append(XmlConfiguration.BinDir + @"\xtmake.exe"); // TODO
            sb.Append(" ");
            sb.Append(installerbin);
            sb.Append(" ");
            sb.Append(zonedir);

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
    }
}
