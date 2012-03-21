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
using System.IO;
using System.Linq;
using System.Text;

using log4net;

using Fusion.Framework.Model;

namespace Fusion.Framework
{
    /// <summary>
    /// Stores information about packages installed on a local system.
    /// </summary>
    public sealed class PackageDatabase : IPackageManager
    {
        private static ILog _log = LogManager.GetLogger(typeof(PackageDatabase));

        private Entities _ent;
        private XmlConfiguration _cfg;

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
        /// Initialises the package manager instance.
        /// </summary>
        /// <param name="ent">entity container</param>
        /// <param name="cfg">ports configuration</param>
        private PackageDatabase(Entities ent, XmlConfiguration cfg)
        {
            _ent = ent;
            _cfg = cfg;
            _log = LogManager.GetLogger(typeof(PackageDatabase));
        }

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        ~PackageDatabase()
        {
            this.Dispose();
        }

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        public void Dispose()
        {
            _ent.Dispose();
        }

        /// <summary>
        /// Find packages installed matching the given package atom.
        /// </summary>
        /// <param name="atom">the atom to search</param>
        /// <param name="zone">ID of the zone to search</param>
        /// <returns>an array of zone packages</returns>
        public Atom[] FindPackages(Atom atom, long zone)
        {
            return _ent.Packages
                .Where(i => i.ZoneID == zone)
                .AsEnumerable()
                .Select(i => Atom.MakeAtomString(i.FullName, i.Version, (uint)i.Slot))
                .Select(i => Atom.Parse(i, AtomParseOptions.VersionRequired))
                .Where(i => atom.Match(i))
                .ToArray();
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

            List<MergeEventArgs> scheduled = new List<MergeEventArgs>();
            Downloader downloader = new Downloader(_cfg.DistFilesDir);

            for (int i = 0; i < distdeparr.Length; i++) {
                IDistribution dist = distdeparr[i];
                Atom current = _ent.Packages
                    .AsEnumerable()
                    .Where(p => p.FullName == dist.Package.FullName && p.Slot == dist.Slot)
                    .Select(p => Atom.MakeAtomString(p.FullName, p.Version, (uint)p.Slot))
                    .Select(p => Atom.Parse(p, AtomParseOptions.VersionRequired))
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

            if (this.OnParallelFetch != null)
                this.OnParallelFetch.Invoke(this, new EventArgs());

            downloader.FetchAsync();

            for (int i = 0; i < scheduled.Count; i++) {
                MergeEventArgs mea = scheduled[i];
                IDistribution dist = mea.Distribution;

                mea.CurrentIter = i + 1;
                mea.TotalMerges = scheduled.Count;

                if (mopts.HasFlag(MergeOptions.Pretend)) {
                    if (this.OnPretendMerge != null)
                        this.OnPretendMerge.Invoke(this, mea);

                    continue;
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
                    continue;

                IInstallProject installer = dist.GetInstallProject();
                if (installer == null)
                    throw new InstallException("Encountered missing or invalid installer project.");
            }
        }

        /// <summary>
        /// Normalises the given zone name.
        /// </summary>
        /// <param name="zone">raw zone name</param>
        /// <returns>normalised zone name</returns>
        private string NormaliseZoneName(string zone)
        {
            return zone.ToLower();
        }

        /// <summary>
        /// Opens the local package database for read/write.
        /// </summary>
        /// <param name="connstr">database connection string</param>
        /// <param name="cfg">ports configuration</param>
        /// <returns>package manager instance</returns>
        public static IPackageManager Open(string connstr, XmlConfiguration cfg)
        {
            return new PackageDatabase(new Entities(connstr), cfg);
        }

        /// <summary>
        /// Finds the installed version of the given package.
        /// </summary>
        /// <param name="atom">package atom without version</param>
        /// <param name="zone">selected zone ID</param>
        /// <returns>package version, or NULL if none is found</returns>
        public Version QueryInstalledVersion(Atom atom, long zone)
        {
            string result = _ent.Packages
                .Where(i => i.Zone.ID == zone && 
                            i.FullName == atom.PackageName && 
                            i.Slot == atom.Slot)
                .Select(i => i.Version)
                .SingleOrDefault();

            return result != null ? new Version(result) : null;
        }

        /// <summary>
        /// Resolves the ID for the given zone name.
        /// </summary>
        /// <param name="zone">zone name to lookup</param>
        /// <returns>zone ID</returns>
        public long QueryZoneID(string zone)
        {
            string zonenorm = this.NormaliseZoneName(zone);
            long result = _ent.Zones
                .Where(i => i.Name == zonenorm)
                .Select(i => i.ID)
                .SingleOrDefault();

            if (result == 0)
                throw new ZoneNotFoundException(zonenorm);

            return result;
        }

        /// <summary>
        /// Items in the world favourites set.
        /// </summary>
        public Atom[] WorldSet
        {
            get
            {
                return _ent.WorldSet
                    .AsEnumerable()
                    .Select(i => Atom.Parse(i.Atom, AtomParseOptions.WithoutVersion))
                    .ToArray();
            }
        }
    }
}
