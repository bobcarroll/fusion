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
    /// Represents a source file that is on the local disk.
    /// </summary>
    public class SourceFile
    {
        private string _digest;
        private string _localname;
        private long _size;
        private CpuArchitecture _cpuarch;

        /// <summary>
        /// Initialises the local source.
        /// </summary>
        /// <param name="digest">expected digest of the file</param>
        /// <param name="localname">file name to use when saving to distfiles</param>
        /// <param name="size">size of the file in bytes</param>
        /// <param name="arch">CPU architecture name</param>
        internal SourceFile(string digest, string localname, long size, CpuArchitecture cpuarch)
        {
            _digest = digest;
            _localname = localname;
            _size = size;
            _cpuarch = cpuarch;
        }

        /// <summary>
        /// CPU architecture name.
        /// </summary>
        public CpuArchitecture CpuArch
        {
            get { return _cpuarch; }
        }

        /// <summary>
        /// Expected digest of the file.
        /// </summary>
        public string Digest
        {
            get { return _digest; }
        }

        /// <summary>
        /// File name to use when saving to distfiles.
        /// </summary>
        public string LocalName
        {
            get { return _localname; }
        }

        /// <summary>
        /// Size of the file in bytes.
        /// </summary>
        public long Size
        {
            get { return _size; }
        }
    }
}
