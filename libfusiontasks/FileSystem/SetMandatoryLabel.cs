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

using Fusion.Framework;

namespace Fusion.Tasks.FileSystem
{
    /// <summary>
    /// Sets the integrity label for the given file or directory.
    /// </summary>
    public sealed class SetMandatoryLabel : Task
    {
        private Security.AceFlags _inheritance = 0;
        private Security.MandatoryPolicy _policy = 0;
        private Security.MandatoryLabel _label = 0;

        /// <summary>
        /// Task execution handler.
        /// </summary>
        /// <returns>true on success, false on error</returns>
        public override bool Execute()
        {
            Security.SetFileMandatoryLabel(this.Path, _inheritance, _policy, _label);
            base.Log.LogMessageFromText(
                "Set mandatory label on path '" + this.Path + "'",
                MessageImportance.Normal);
            return true;
        }

        /// <summary>
        /// Target file or directory.
        /// </summary>
        [Required]
        public string Path { get; set; }

        /// <summary>
        /// List of flags to control inheritence.
        /// </summary>
        /// <remarks>See Fusion.Framework.Security.AceFlags for options.</remarks>
        public string[] Inheritance
        {
            get
            {
                return _inheritance
                    .ToString()
                    .Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            }
            
            set
            {
                foreach (string s in value)
                    _inheritance |= (Security.AceFlags)Enum.Parse(typeof(Security.AceFlags), s, true);
            }
        }

        /// <summary>
        /// List of mandatory policies.
        /// </summary>
        /// <remarks>See Fusion.Framework.Security.MandatoryPolicy for options.</remarks>
        public string[] Policy
        {
            get
            {
                return _policy
                    .ToString()
                    .Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            }

            set
            {
                foreach (string s in value)
                    _policy |= (Security.MandatoryPolicy)Enum.Parse(typeof(Security.MandatoryPolicy), s, true);
            }
        }

        /// <summary>
        /// The mandatory integrity level.
        /// </summary>
        /// <remarks>See Fusion.Framework.Security.MandatoryLabel for options.</remarks>
        [Required]
        public string Label
        {
            get { return _label.ToString(); }
            set { _label = (Security.MandatoryLabel)Enum.Parse(typeof(Security.MandatoryLabel), value, true); }
        }
    }
}
