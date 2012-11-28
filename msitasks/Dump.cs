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
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Fusion.Tasks.MSI
{
    /// <summary>
    /// Extracts the contents of an MSI.
    /// </summary>
    public sealed class Dump : Task
    {
        /// <summary>
        /// Task execution handler.
        /// </summary>
        /// <returns>true on success, false on error</returns>
        public override bool Execute()
        {
            string wixhome = Environment.GetEnvironmentVariable(
                "WIX_HOME", 
                EnvironmentVariableTarget.Machine);

            if (String.IsNullOrEmpty(wixhome)) {
                base.Log.LogError("WIX_HOME environment variable is not defined!");
                return false;
            }

            List<string> args = new List<string>();
            args.Add(this.MsiPath);
            args.Add(Path.Combine(this.DumpDir, "wix.xml"));

            if (this.CabFiles) {
                args.Add("-x");
                args.Add(this.DumpDir.TrimEnd('\\'));
            }

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = Path.Combine(wixhome, "dark.exe");
            psi.Arguments = String.Join(" ", args.ToArray());
            psi.EnvironmentVariables.Add("WIX_TEMP", this.TempDir);
            psi.UseShellExecute = false;

            base.Log.LogMessage(
                "Dumping MSI package '{0}' to '{1}'", 
                this.MsiPath, 
                this.DumpDir);
            Process dark = Process.Start(psi);
            dark.WaitForExit();

            return true;
        }

        /// <summary>
        /// Flag to dump cabinet files.
        /// </summary>
        public bool CabFiles { get; set; }

        /// <summary>
        /// Path where files were dumped from the MSI.
        /// </summary>
        [Required]
        public string DumpDir { get; set; }

        /// <summary>
        /// Path of the MSI file.
        /// </summary>
        [Required]
        public string MsiPath { get; set; }

        /// <summary>
        /// Path where temporary files are written to.
        /// </summary>
        [Required]
        public string TempDir { get; set; }
    }
}
