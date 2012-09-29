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
    /// Copies a single file from one path to another.
    /// </summary>
    public sealed class CopyFile : Task
    {
        /// <summary>
        /// Task execution handler.
        /// </summary>
        /// <returns>true on success, false on error</returns>
        public override bool Execute()
        {
            if (File.Exists(this.TargetFile) && !this.Overwrite) {
                Log.LogWarning("Skipping file copy because '" + this.TargetFile + "' already exists");
                return true;
            }

            File.Copy(this.SourceFile, this.TargetFile, this.Overwrite);
            base.Log.LogMessage("Copying file '" + this.SourceFile + "' to '" + this.TargetFile + "'");
            return true;
        }

        /// <summary>
        /// Source file path..
        /// </summary>
        [Required]
        public string SourceFile { get; set; }

        /// <summary>
        /// Target file path.
        /// </summary>
        [Required]
        public string TargetFile { get; set; }

        /// <summary>
        /// Flag to overwrite the target file.
        /// </summary>
        public bool Overwrite { get; set; }
    }
}
