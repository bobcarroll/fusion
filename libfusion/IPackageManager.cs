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

using FileTuple = System.Tuple<string, Fusion.Framework.FileType, string>;
using MetadataPair = System.Collections.Generic.KeyValuePair<string, string>;
using VersionTuple = System.Tuple<System.Version, uint>;

namespace Fusion.Framework
{
    /// <summary>
    /// Manages packages installed on a local system.
    /// </summary>
    public interface IPackageManager : IDisposable
    {
        /// <summary>
        /// Determines if the given paths are in use by another package.
        /// </summary>
        /// <param name="patharr">absolute paths to check</param>
        /// <param name="owner">package atom that should own the files</param>
        /// <returns>paths owned by another package</returns>
        string[] CheckFilesOwner(string[] patharr, Atom owner);

        /// <summary>
        /// Determines if the given path is in use by another package.
        /// </summary>
        /// <param name="path">absolute path to check</param>
        /// <returns>true if the path is owned, false otherwise</returns>
        bool CheckPathOwned(string path);

        /// <summary>
        /// Removes the given package from the database.
        /// </summary>
        /// <param name="atom">package atom with version and slot</param>
        void DeletePackage(Atom atom);

        /// <summary>
        /// Removes the given items from the trash.
        /// </summary>
        /// <param name="patharr">absolute file paths in the trash</param>
        void DeleteTrashItems(string[] patharr);

        /// <summary>
        /// Removes the given package atom from the world favourites.
        /// </summary>
        /// <param name="atom">package atom without version</param>
        void DeselectPackage(Atom atom);

        /// <summary>
        /// Find packages installed matching the given package atom.
        /// </summary>
        /// <param name="atom">the atom to search</param>
        /// <returns>an array of installed packages</returns>
        Atom[] FindPackages(Atom atom);

        /// <summary>
        /// Gets the installer project for the given atom.
        /// </summary>
        /// <param name="atom">package atom with version and slot</param>
        /// <returns>the installer project</returns>
        IInstallProject GetPackageInstaller(Atom atom);

        /// <summary>
        /// Determines if the given package is protected by profile.
        /// </summary>
        /// <param name="atom">the package to check</param>
        /// <returns>true if protected, false otherwise</returns>
        bool IsProtected(Atom atom);

        /// <summary>
        /// Finds the installed version of the given package.
        /// </summary>
        /// <param name="atom">package atom without version</param>
        /// <returns>package version, or NULL if none is found</returns>
        /// <remarks>This will query the same slot of the given atom.</remarks>
        VersionTuple QueryInstalledVersion(Atom atom);

        /// <summary>
        /// Finds all files associated with the given installed package.
        /// </summary>
        /// <param name="atom">package atom with version and slot</param>
        /// <returns>files tuple for all installed files</returns>
        FileTuple[] QueryPackageFiles(Atom atom);

        /// <summary>
        /// Records a package installation in the package database.
        /// </summary>
        /// <param name="dist">newly installed package</param>
        /// <param name="installer">installer project</param>
        /// <param name="files">real files and directories created by the package</param>
        /// <param name="metadata">dictionary of package installation metadata</param>
        /// <param name="world">indicates if the package is a world favourite</param>
        /// <remarks>The files tuple should be (absolute file path, file type, digest).</remarks>
        void RecordPackage(IDistribution dist, IInstallProject installer, FileTuple[] files, 
            MetadataPair[] metadata, bool world);

        /// <summary>
        /// Adds an item to the trash for later clean-up.
        /// </summary>
        /// <param name="path">absolute path of the file</param>
        void TrashFile(string path);

        /// <summary>
        /// The contents of the file trash.
        /// </summary>
        string[] Trash { get; }

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
