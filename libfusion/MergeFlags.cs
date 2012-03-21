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
    /// Merge status flags.
    /// </summary>
    [Flags]
    public enum MergeFlags : int
    {
        /// <summary>
        /// New package (not yet installed).
        /// </summary>
        New = 1,

        /// <summary>
        /// New side-by-side installation.
        /// </summary>
        Slot = 2,

        /// <summary>
        /// Updating package to another version.
        /// </summary>
        Updating = 4,

        /// <summary>
        /// Downgrading (best version seems lower).
        /// </summary>
        Downgrading = 8,

        /// <summary>
        /// Replacing the same version of an installed package.
        /// </summary>
        Replacing = 16,

        /// <summary>
        /// Fetch restriction (package must be manually downloaded).
        /// </summary>
        FetchNeeded = 32,

        /// <summary>
        /// Fetch restriction (package is already downloaded).
        /// </summary>
        FetchExists = 64,

        /// <summary>
        /// Requires user input.
        /// </summary>
        Interactive = 128,

        /// <summary>
        /// Blocked by another package (unresolved conflict).
        /// </summary>
        BlockUnresolved = 256,

        /// <summary>
        /// Blocked by another package (automatically resolved conflict).
        /// </summary>
        BlockResolved = 512
    }
}
