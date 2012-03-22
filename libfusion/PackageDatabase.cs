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
