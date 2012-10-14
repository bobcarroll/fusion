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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Fusion.Tasks.Compression
{
    /// <summary>
    /// Extracts files from an archive.
    /// </summary>
    public sealed class Extract : Task
    {
        /// <summary>
        /// Task execution handler.
        /// </summary>
        /// <returns>true on success, false on error</returns>
        public override bool Execute()
        {
            if (!File.Exists(this.Archive)) {
                base.Log.LogError("Archive '{0}' does not exist", this.Archive);
                return false;
            }

            string asmpath = 
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = asmpath + @"\7za.exe";
            psi.Arguments = String.Format("x -o{0} {1}", this.Output, this.Archive);
            psi.UseShellExecute = false;

            Process szproc = Process.Start(psi);
            szproc.WaitForExit();

            return (szproc.ExitCode == 0);
        }

        /// <summary>
        /// Target archive path.
        /// </summary>
        [Required]
        public string Archive { get; set; }

        /// <summary>
        /// Path where output files are written to.
        /// </summary>
        [Required]
        public string Output { get; set; }
    }
}
