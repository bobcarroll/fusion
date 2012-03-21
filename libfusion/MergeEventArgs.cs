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
    /// Metadata for merge callbacks.
    /// </summary>
    public sealed class MergeEventArgs : EventArgs
    {
        /// <summary>
        /// The one-based index of the current merge.
        /// </summary>
        public int CurrentIter;

        /// <summary>
        /// The distribution being merged.
        /// </summary>
        public IDistribution Distribution;

        /// <summary>
        /// Unique identifier for retrieving fetch results.
        /// </summary>
        public Guid FetchHandle;

        /// <summary>
        /// Flag to indicate if the package is only being fetched.
        /// </summary>
        public bool FetchOnly;

        /// <summary>
        /// Merge status flags.
        /// </summary>
        public MergeFlags Flags;

        /// <summary>
        /// The previously installed version (if any).
        /// </summary>
        public Atom Previous;

        /// <summary>
        /// Package was explicitly selected for merging.
        /// </summary>
        public bool Selected;

        /// <summary>
        /// The total number of packages to merge.
        /// </summary>
        public int TotalMerges;
    }
}
