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
    /// Options for merging packages.
    /// </summary>
    [Flags]
    public enum MergeOptions : int
    {
        /// <summary>
        /// Instead of performing the install, show what packages would be installed.
        /// </summary>
        Pretend = 1,

        /// <summary>
        /// Merge the package normally, but do not include the package in the world set.
        /// </summary>
        OneShot = 2,

        /// <summary>
        /// Do not re-merge packages that are already installed.
        /// </summary>
        NoReplace = 4,

        /// <summary>
        /// Reinstall the target atoms and their entire deep dependency tree, as though 
        /// no packages are currently installed.
        /// </summary>
        EmptyTree = 8,

        /// <summary>
        /// Instead of performing the install, download the packages that would be installed.
        /// </summary>
        FetchOnly = 16
    }
}
