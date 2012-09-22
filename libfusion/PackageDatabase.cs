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
using System.Data;
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
        /// Initialises the package manager instance.
        /// </summary>
        /// <param name="ent">entity container</param>
        /// <param name="cfg">ports configuration</param>
        private PackageDatabase(Entities ent, XmlConfiguration cfg)
        {
            _ent = ent;
            _cfg = cfg;
            _log = LogManager.GetLogger(typeof(PackageDatabase));

            _ent.Connection.StateChange += 
                new StateChangeEventHandler(this.OnConnectionStateChange);
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
        /// <returns>an array of installed packages</returns>
        public Atom[] FindPackages(Atom atom)
        {
            return _ent.Packages
                .AsEnumerable()
                .Select(i => Atom.MakeAtomString(i.FullName, i.Version, (uint)i.Slot))
                .Select(i => Atom.Parse(i, AtomParseOptions.VersionRequired))
                .Where(i => atom.Match(i))
                .ToArray();
        }

        /// <summary>
        /// Handles database connection state change events.
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">state change event arguments</param>
        private void OnConnectionStateChange(object sender, StateChangeEventArgs e)
        {
            /* sqlite foreign keys must be enabled for each connection. Since EF will open and close
               connections whenever it feels like it, we must ensure foreign keys are re-enabled */
            if (e.CurrentState == ConnectionState.Open)
                _ent.ExecuteStoreCommand("PRAGMA foreign_keys = true;");
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
        /// <returns>package version, or NULL if none is found</returns>
        public Version QueryInstalledVersion(Atom atom)
        {
            string result = _ent.Packages
                .Where(i => i.FullName == atom.PackageName && 
                            i.Slot == atom.Slot)
                .Select(i => i.Version)
                .SingleOrDefault();

            return result != null ? new Version(result) : null;
        }

        /// <summary>
        /// Records a package installation in the package database.
        /// </summary>
        /// <param name="dist">newly installed package</param>
        /// <param name="files">real files and directories created by the package</param>
        /// <param name="metadata">dictionary of package installation metadata</param>
        /// <param name="selected">indicates if the package is a world favourite</param>
        public void RecordPackage(IDistribution dist, Tuple<string, FileType, string>[] files, 
            IDictionary<string, string> metadata, bool world)
        {
            string pf = dist.Package.FullName;
            string ver = dist.Version.ToString();
            uint slot = dist.Slot;

            Model.Package oldpkg = _ent.Packages
                .Where(i => i.FullName == pf && i.Version == ver && i.Slot == slot)
                .SingleOrDefault();

            if (oldpkg != null)
                _ent.Packages.DeleteObject(oldpkg);

            Model.Package newpkg = new Model.Package() {
                FullName = pf,
                Version = ver,
                Slot = slot
            };

            foreach (Tuple<string, FileType, string> ft in files) {
                newpkg.Files.Add(new Model.File() {
                    Path = ft.Item1,
                    Type = (int)ft.Item2,
                    Digest = ft.Item3
                });
            }

            foreach (KeyValuePair<string, string> kvp in metadata) {
                newpkg.Metadata.Add(new MetadataItem() {
                    Key = kvp.Key,
                    Value = kvp.Value
                });
            }

            _ent.Packages.AddObject(newpkg);

            if (world && _ent.WorldSet.Where(i => i.Atom == dist.Package.FullName).Count() == 0)
                _ent.WorldSet.AddObject(new WorldItem() { Atom = pf });

            _ent.SaveChanges();
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

        /// <summary>
        /// Root directory where packages are installed.
        /// </summary>
        public DirectoryInfo RootDir
        {
            get { return _cfg.RootDir; }
        }
    }
}
