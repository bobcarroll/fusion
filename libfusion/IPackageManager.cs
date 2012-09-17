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

namespace Fusion.Framework
{
    /// <summary>
    /// Manages packages installed on a local system.
    /// </summary>
    public interface IPackageManager : IDisposable
    {
        /// <summary>
        /// Find packages installed matching the given package atom.
        /// </summary>
        /// <param name="atom">the atom to search</param>
        /// <param name="zone">ID of the zone to search</param>
        /// <returns>an array of zone packages</returns>
        Atom[] FindPackages(Atom atom, long zone);

        /// <summary>
        /// Finds the installed version of the given package.
        /// </summary>
        /// <param name="atom">package atom without version</param>
        /// <param name="zone">selected zone ID</param>
        /// <returns>package version, or NULL if none is found</returns>
        Version QueryInstalledVersion(Atom atom, long zone);

        /// <summary>
        /// Retrieves the zone directory prefix for the given ID.
        /// </summary>
        /// <param name="id">zone id to lookup</param>
        /// <returns>the zone directory prefix</returns>
        string QueryZonePrefix(long id);

        /// <summary>
        /// Resolves the ID for the given zone name.
        /// </summary>
        /// <param name="zone">zone name to lookup</param>
        /// <returns>zone ID</returns>
        long QueryZoneID(string zone);

        /// <summary>
        /// Items in the world favourites set.
        /// </summary>
        Atom[] WorldSet { get; }
    }
}
