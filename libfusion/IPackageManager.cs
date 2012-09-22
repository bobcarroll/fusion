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
        /// <returns>an array of installed packages</returns>
        Atom[] FindPackages(Atom atom);

        /// <summary>
        /// Finds the installed version of the given package.
        /// </summary>
        /// <param name="atom">package atom without version</param>
        /// <returns>package version, or NULL if none is found</returns>
        Version QueryInstalledVersion(Atom atom);

        /// <summary>
        /// Records a package installation in the package database.
        /// </summary>
        /// <param name="dist">newly installed package</param>
        /// <param name="files">real files and directories created by the package</param>
        /// <param name="metadata">dictionary of package installation metadata</param>
        /// <param name="selected">indicates if the package is a world favourite</param>
        /// <remarks>The files tuple should be (absolute file path, file type, digest).</remarks>
        void RecordPackage(IDistribution dist, Tuple<string, FileType, string>[] files, 
            IDictionary<string, string> metadata, bool world);

        /// <summary>
        /// Items in the world favourites set.
        /// </summary>
        Atom[] WorldSet { get; }

        /// <summary>
        /// Root directory where packages are installed.
        /// </summary>
        DirectoryInfo RootDir { get; }
    }
}
