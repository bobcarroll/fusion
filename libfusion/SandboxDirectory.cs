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
    /// Provides sandbox directory paths.
    /// </summary>
    public sealed class SandboxDirectory
    {
        private DirectoryInfo _sboxdir;
        private DirectoryInfo _imgdir;
        private DirectoryInfo _linkdir;
        private DirectoryInfo _tempdir;
        private DirectoryInfo _workdir;

        /// <summary>
        /// Initialises the sandbox info.
        /// </summary>
        /// <param name="sboxdir">sandbox directory</param>
        private SandboxDirectory(DirectoryInfo sboxdir)
        {
            _sboxdir = sboxdir;
            _imgdir = new DirectoryInfo(_sboxdir.FullName + @"\image");
            _linkdir = new DirectoryInfo(_sboxdir.FullName + @"\link");
            _tempdir = new DirectoryInfo(_sboxdir.FullName + @"\temp");
            _workdir = new DirectoryInfo(_sboxdir.FullName + @"\work");
        }

        /// <summary>
        /// Creates a new sandbox.
        /// </summary>
        /// <param name="tmpdir">temp directory to create in</param>
        /// <returns>the new sandbox directory</returns>
        public static SandboxDirectory Create(DirectoryInfo tmpdir)
        {
            DirectoryInfo sboxdir = 
                new DirectoryInfo(tmpdir.FullName + @"\" + Guid.NewGuid().ToString());
            if (!sboxdir.Exists)
                sboxdir.Create();

            return SandboxDirectory.Open(new DirectoryInfo(sboxdir.FullName));
        }

        /// <summary>
        /// Deletes the sandbox directory.
        /// </summary>
        public void Delete()
        {
            try {
                _sboxdir.Delete(true);
            } catch {
                /* we don't care */
            }
        }

        /// <summary>
        /// Opens an existing sandbox.
        /// </summary>
        /// <param name="sboxdir">sandbox directory</param>
        /// <returns>the sandbox</returns>
        public static SandboxDirectory Open(DirectoryInfo sboxdir)
        {
            if (!sboxdir.Exists)
                throw new DirectoryNotFoundException("Directory does not exist: " + sboxdir.FullName);

            SandboxDirectory result = new SandboxDirectory(sboxdir);

            if (!result.ImageDir.Exists)
                result.ImageDir.Create();

            if (!result.LinkDir.Exists)
                result.LinkDir.Create();

            if (!result.TempDir.Exists)
                result.TempDir.Create();

            if (!result.WorkDir.Exists)
                result.WorkDir.Create();

            return result;
        }

        /// <summary>
        /// Directory where the package is copied to for installation.
        /// </summary>
        public DirectoryInfo ImageDir
        {
            get { return _imgdir; }
        }

        /// <summary>
        /// Directory where Start Menu shortcuts are installed.
        /// </summary>
        public DirectoryInfo LinkDir
        {
            get { return _linkdir; }
        }

        /// <summary>
        /// Sandbox directory root.
        /// </summary>
        public DirectoryInfo Root
        {
            get { return _sboxdir; }
        }

        /// <summary>
        /// Directory for temporary files.
        /// </summary>
        public DirectoryInfo TempDir
        {
            get { return _tempdir; }
        }

        /// <summary>
        /// Directory where package preparation and build are done.
        /// </summary>
        public DirectoryInfo WorkDir
        {
            get { return _workdir; }
        }
    }
}
