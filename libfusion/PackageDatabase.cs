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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using log4net;

using Fusion.Framework.Model;

using FileTuple = System.Tuple<string, Fusion.Framework.FileType, string>;
using MetadataPair = System.Collections.Generic.KeyValuePair<string, string>;
using VersionTuple = System.Tuple<System.Version, uint>;

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
        private Atom[] _protected;

        /// <summary>
        /// Initialises the package manager instance.
        /// </summary>
        /// <param name="ent">entity container</param>
        private PackageDatabase(Entities ent)
        {
            _ent = ent;
            _cfg = XmlConfiguration.LoadSeries();
            _log = LogManager.GetLogger(typeof(PackageDatabase));
            _protected = this.GetProtectedPackages();

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
        /// Determines if the given paths are in use by another package.
        /// </summary>
        /// <param name="patharr">absolute paths to check</param>
        /// <param name="owner">package atom that should own the files</param>
        /// <returns>paths owned by another package</returns>
        public string[] CheckFilesOwner(string[] patharr, Atom owner)
        {
            return _ent.Files
                .AsEnumerable()
                .Where(i => i.Package.FullName != owner.PackageName)
                .Select(i => i.Path.TrimEnd('\\'))
                .Intersect(patharr.Select(i => i.TrimEnd('\\')))
                .ToArray();
        }

        /// <summary>
        /// Determines if the given path is in use by another package.
        /// </summary>
        /// <param name="path">absolute path to check</param>
        /// <returns>true if the path is owned, false otherwise</returns>
        public bool CheckPathOwned(string path)
        {
            return _ent.Files
                .AsEnumerable()
                .Where(i => i.Path.TrimEnd('\\') == path.TrimEnd('\\'))
                .Count() > 0;
        }

        /// <summary>
        /// Removes the given package from the database.
        /// </summary>
        /// <param name="atom">package atom with version and slot</param>
        public void DeletePackage(Atom atom)
        {
            Model.Package mp = _ent.Packages
                .AsEnumerable()
                .Where(i => i.FullName == atom.PackageName &&
                             i.Version == atom.Version.ToString() &&
                             i.Revision == atom.Revision &&
                             i.Slot == atom.Slot)
                .SingleOrDefault();

            if (mp == null)
                return;

            _ent.Packages.DeleteObject(mp);
            _ent.SaveChanges();
        }

        /// <summary>
        /// Removes the given items from the trash.
        /// </summary>
        /// <param name="patharr">absolute file paths in the trash</param>
        public void DeleteTrashItems(string[] patharr)
        {
            foreach (TrashItem ti in _ent.Trash.OrderBy(i => i.Path)) {
                if (patharr.Contains(ti.Path))
                    _ent.Trash.DeleteObject(ti);
            }

            _ent.SaveChanges();
        }

        /// <summary>
        /// Removes the given package atom from the world favourites.
        /// </summary>
        /// <param name="atom">package atom without version</param>
        public void DeselectPackage(Atom atom)
        {
            WorldItem wi = _ent.WorldSet
                .AsEnumerable()
                .Where(i => i.Atom == atom.PackageName)
                .SingleOrDefault();

            if (wi == null)
                return;

            _ent.WorldSet.DeleteObject(wi);
            _ent.SaveChanges();
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
                .Select(i => Atom.MakeAtomString(i.FullName, i.Version, (uint)i.Revision, (uint)i.Slot))
                .Select(i => Atom.Parse(i, AtomParseOptions.VersionRequired))
                .Where(i => atom.Match(i))
                .ToArray();
        }

        /// <summary>
        /// Gets the installer project for the given atom.
        /// </summary>
        /// <param name="atom">package atom with version and slot</param>
        /// <returns>installer project</returns>
        public IInstallProject GetPackageInstaller(Atom atom)
        {
            if (!atom.IsFullName || !atom.HasVersion)
                throw new ArgumentException("Cannot get installer project without full package atom with version");

            string instblob = _ent.Packages
                .AsEnumerable()
                .Where(i => i.FullName == atom.PackageName &&
                             i.Version == atom.Version.ToString() &&
                             i.Revision == atom.Revision &&
                             i.Slot == atom.Slot)
                .Select(i => i.Project)
                .SingleOrDefault();

            if (String.IsNullOrEmpty(instblob))
                return null;

            byte[] buf = Convert.FromBase64String(instblob);
            IInstallProject installer;

            MemoryStream ms = new MemoryStream();
            ms.Write(buf, 0, buf.Length);
            ms.Seek(0, SeekOrigin.Begin);
            installer = (IInstallProject)(new BinaryFormatter()).Deserialize(ms);
            ms.Close();

            return installer;
        }

        /// <summary>
        /// Reads atoms from the profile packages.protect file.
        /// </summary>
        /// <returns>an array of package atoms</returns>
        public Atom[] GetProtectedPackages()
        {
            List<Atom> alst = new List<Atom>();

            FileInfo fi = new FileInfo(_cfg.ProfileDir + @"\package.protect");
            if (fi.Exists) {
                string[] inarr = System.IO.File.ReadAllLines(fi.FullName);

                foreach (string s in inarr) {
                    try {
                        if (s.StartsWith("#")) continue;
                        alst.Add(Atom.Parse(s, AtomParseOptions.WithoutVersion));
                    } catch (BadAtomException) {
                        throw new BadAtomException("Bad package atom '" + s + "' in package.protect file.");
                    }
                }
            }

            return alst.ToArray();
        }

        /// <summary>
        /// Determines if the given package is protected by profile.
        /// </summary>
        /// <param name="atom">the package to check</param>
        /// <returns>true if protected, false otherwise</returns>
        public bool IsProtected(Atom atom)
        {
            return _protected.Where(i => i.Match(atom)).Count() > 0;
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
        /// <returns>package manager instance</returns>
        public static IPackageManager Open(string connstr)
        {
            return new PackageDatabase(new Entities(connstr));
        }

        /// <summary>
        /// Finds the installed version of the given package.
        /// </summary>
        /// <param name="atom">package atom without version</param>
        /// <returns>package version, or NULL if none is found</returns>
        /// <remarks>This will query the same slot of the given atom.</remarks>
        public VersionTuple QueryInstalledVersion(Atom atom)
        {
            return _ent.Packages
                .AsEnumerable()
                .Where(i => i.FullName == atom.PackageName && 
                            i.Slot == atom.Slot)
                .Select(i => new VersionTuple(new Version(i.Version), (uint)i.Revision))
                .SingleOrDefault();
        }

        /// <summary>
        /// Finds all files associated with the given installed package.
        /// </summary>
        /// <param name="atom">package atom with version and slot</param>
        /// <returns>files tuple for all installed files</returns>
        public FileTuple[] QueryPackageFiles(Atom atom)
        {
            if (!atom.IsFullName || !atom.HasVersion)
                throw new ArgumentException("Cannot query files without full package atom with version");

            return _ent.Files
                .AsEnumerable()
                .Where(i => i.Package.FullName == atom.PackageName &&
                             i.Package.Version == atom.Version.ToString() &&
                             i.Package.Revision == atom.Revision &&
                             i.Package.Slot == atom.Slot)
                .OrderBy(i => i.Path)
                .Select(i => new FileTuple(i.Path, (FileType)i.Type, i.Digest))
                .ToArray();
        }

        /// <summary>
        /// Records a package installation in the package database.
        /// </summary>
        /// <param name="dist">newly installed package</param>
        /// <param name="installer">installer project</param>
        /// <param name="files">real files and directories created by the package</param>
        /// <param name="metadata">dictionary of package installation metadata</param>
        /// <remarks>The files tuple should be (absolute file path, file type, digest).</remarks>
        public void RecordPackage(IDistribution dist, IInstallProject installer, FileTuple[] files, 
            MetadataPair[] metadata)
        {
            string pf = dist.Package.FullName;
            string ver = dist.Version.ToString();
            uint rev = dist.Revision;
            uint slot = dist.Slot;

            MemoryStream ms = new MemoryStream();
            (new BinaryFormatter()).Serialize(ms, installer);
            string installerblob = Convert.ToBase64String(ms.ToArray());
            ms.Close();

            Model.Package oldpkg = _ent.Packages
                .Where(i => i.FullName == pf && i.Slot == slot)
                .SingleOrDefault();

            if (oldpkg != null)
                _ent.Packages.DeleteObject(oldpkg);

            Model.Package newpkg = new Model.Package() {
                FullName = pf,
                Version = ver,
                Revision = rev,
                Slot = slot,
                Project = installerblob
            };

            foreach (FileTuple ft in files) {
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
            _ent.SaveChanges();
        }

        /// <summary>
        /// Adds the given package atom to the world favourites.
        /// </summary>
        /// <param name="atom">package atom without version</param>
        public void SelectPackage(Atom atom)
        {
            if (_ent.WorldSet.Where(i => i.Atom == atom.PackageName).Count() == 0) {
                _ent.WorldSet.AddObject(new WorldItem() { Atom = atom.PackageName });
                _ent.SaveChanges();
            }
        }

        /// <summary>
        /// Adds an item to the trash for later clean-up.
        /// </summary>
        /// <param name="path">absolute path of the file</param>
        public void TrashFile(string path)
        {
            _ent.Trash.AddObject(new TrashItem() { Path = path });
            _ent.SaveChanges();
        }

        /// <summary>
        /// The contents of the file trash.
        /// </summary>
        public string[] Trash
        {
            get
            {
                return _ent.Trash
                    .Select(i => i.Path)
                    .ToArray();
            }
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
