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
using System.Net;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Represents a source file that is downloaded from the web.
    /// </summary>
    public sealed class WebSourceFile : SourceFile
    {
        private Uri _location;

        /// <summary>
        /// Initialises the web source.
        /// </summary>
        /// <param name="location">remote URL to download from</param>
        /// <param name="digest">expected digest of the file</param>
        /// <param name="localname">file name to use when saving to distfiles</param>
        /// <param name="size">size of the file in bytes</param>
        /// <param name="cpuarch">CPU architecture name</param>
        /// <param name="srctype">source type name</param>
        internal WebSourceFile(Uri location, string digest, string localname, long size, 
            CpuArchitecture cpuarch, SourceType srctype)
            : base(digest, localname, size, cpuarch, srctype)
        {
            _location = location;
        }

        /// <summary>
        /// Fetch the file and put it in the given directory.
        /// </summary>
        /// <param name="destdir">destination directory</param>
        public void Fetch(DirectoryInfo destdir)
        {
            try {
                WebClient wc = new WebClient();
                wc.Proxy = WebRequest.DefaultWebProxy;
                wc.DownloadFile(_location, destdir.FullName + @"\" + this.LocalName);
            } catch (WebException ex) {
                throw ex;
            }
        }

        /// <summary>
        /// Remote URI to download from.
        /// </summary>
        public Uri Location
        {
            get { return _location; }
        }
    }
}
