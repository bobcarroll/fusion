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
using System.Security.AccessControl;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Fusion.Tasks.FileSystem
{
    /// <summary>
    /// Removes permission inheritance on the given path, and optionally
    /// preserves permissions.
    /// </summary>
    public sealed class BreakInheritance : Task
    {
        /// <summary>
        /// Task execution handler.
        /// </summary>
        /// <returns>true on success, false on error</returns>
        public override bool Execute()
        {
            if (File.Exists(this.Path)) {
                FileSecurity fs = File.GetAccessControl(this.Path);
                fs.SetAccessRuleProtection(true, this.Preserve);
                File.SetAccessControl(this.Path, fs);
            } else if (Directory.Exists(this.Path)) {
                DirectorySecurity ds = Directory.GetAccessControl(this.Path);
                ds.SetAccessRuleProtection(true, this.Preserve);
                Directory.SetAccessControl(this.Path, ds);
            } else
                throw new FileNotFoundException("File '" + this.Path + "' was not found.");

            base.Log.LogMessage("Breaking permission inheritance on path '" + this.Path + "'");
            return true;
        }

        /// <summary>
        /// Target file or directory.
        /// </summary>
        [Required]
        public string Path { get; set; }

        /// <summary>
        /// Flag to copy parent permissions.
        /// </summary>
        public bool Preserve { get; set; }
    }
}
