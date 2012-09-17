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
using System.Runtime.Serialization;
using System.Text;

using log4net;

namespace Fusion.Framework
{
    /// <summary>
    /// A distribution installer project.
    /// </summary>
    public interface IInstallProject : ISerializable
    {
        /// <summary>
        /// Registers a variable with the installer project.
        /// </summary>
        /// <param name="key">variable name</param>
        /// <param name="value">the value</param>
        void RegisterVariable(string key, string value);

        /// <summary>
        /// Install the source files into the destination directory.
        /// </summary>
        void SrcInstall();

        /// <summary>
        /// Unpack the source files into the working directory.
        /// </summary>
        void SrcUnpack();

        /// <summary>
        /// Indicates whether or not the installer project has a source-install target.
        /// </summary>
        bool HasSrcInstallTarget { get; }

        /// <summary>
        /// Indicates whether or not the installer project has a source-unpack target.
        /// </summary>
        bool HasSrcUnpackTarget { get; }

        /// <summary>
        /// Name of the package being installed.
        /// </summary>
        string PackageName { get; }
    }
}
