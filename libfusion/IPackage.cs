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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// A single package in a ports collection.
    /// </summary>
    public interface IPackage
    {
        /// <summary>
        /// A reference to the parent category.
        /// </summary>
        ICategory Category { get; }

        /// <summary>
        /// A brief description of this package.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// All distributions of this package.
        /// </summary>
        ReadOnlyCollection<IDistribution> Distributions { get; }

        /// <summary>
        /// The full name of this package.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// The homepage or project website of this package.
        /// </summary>
        string Homepage { get; }

        /// <summary>
        /// The latest available distribution of this package.
        /// </summary>
        IDistribution LatestAvailable { get; }

        /// <summary>
        /// The latest unmasked distribution of this package.
        /// </summary>
        IDistribution LatestUnmasked { get; }

        /// <summary>
        /// The software license for this package.
        /// </summary>
        string License { get; }

        /// <summary>
        /// The name of this package.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// A reference to the parent ports tree.
        /// </summary>
        AbstractTree PortsTree { get; }
    }
}
