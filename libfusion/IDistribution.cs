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
    /// A versioned distribution of a package.
    /// </summary>
    public interface IDistribution
    {
        /// <summary>
        /// Creates a distribution installer project instance.
        /// </summary>
        /// <returns>an install project instance</returns>
        IInstallProject GetInstallProject();

        /// <summary>
        /// Fusion API revision number.
        /// </summary>
        int ApiRevision { get; }

        /// <summary>
        /// The exact atom for this distribution.
        /// </summary>
        Atom Atom { get; }

        /// <summary>
        /// Packages this distribution depends on.
        /// </summary>
        Atom[] Dependencies { get; }

        /// <summary>
        /// Fetch restriction imposes manual distfile download.
        /// </summary>
        bool FetchRestriction { get; }

        /// <summary>
        /// Flag indicating the installation requires user interaction.
        /// </summary>
        bool Interactive { get; }

        /// <summary>
        /// The arch keywords for this distribution.
        /// </summary>
        string[] Keywords { get; }

        /// <summary>
        /// A reference to the parent package.
        /// </summary>
        IPackage Package { get; }

        /// <summary>
        /// A reference to the parent ports tree.
        /// </summary>
        AbstractTree PortsTree { get; }

        /// <summary>
        /// Package installation slot number.
        /// </summary>
        uint Slot { get; }

        /// <summary>
        /// Package source files.
        /// </summary>
        SourceFile[] Sources { get; }

        /// <summary>
        /// The total uncompressed size of package files.
        /// </summary>
        long TotalSize { get; }

        /// <summary>
        /// The package version specified in this atom.
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Gets a string representation of this distribution
        /// </summary>
        /// <returns>a package atom</returns>
        string ToString();
    }
}
