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
using System.Linq;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Microsoft.Win32;

namespace Fusion.Tasks.Registry
{
    /// <summary>
    /// Removes the given registry value.
    /// </summary>
    public sealed class DeleteValue : Task
    {
        private RegistryHive _hive;
        private RegistryView _view = Microsoft.Win32.RegistryView.Default;

        /// <summary>
        /// Task execution handler.
        /// </summary>
        /// <returns>true on success, false on error</returns>
        public override bool Execute()
        {
            RegistryKey hkey = RegistryKey.OpenBaseKey(_hive, _view);
            RegistryKey subkey = hkey.OpenSubKey(this.Key, true);

            if (subkey != null) {
                base.Log.LogMessage(
                    @"Deleting Registry Value: {0} on SubKey: {1} in Hive {2}, View: {3}",
                    this.Value,
                    this.Key,
                    this.RegistryHive,
                    this.RegistryView);
                subkey.DeleteValue(this.Value);
                subkey.Close();
            }

            hkey.Close();
            return true;
        }

        /// <summary>
        /// Registry subkey path.
        /// </summary>
        [Required]
        public string Key { get; set; }

        /// <summary>
        /// Registry hive name.
        /// </summary>
        [Required]
        public string RegistryHive
        {
            get { return Enum.GetName(typeof(RegistryHive), _hive); }

            set
            {
                _hive = (RegistryHive)Enum.Parse(typeof(RegistryHive), value);
            }
        }

        /// <summary>
        /// Registry view name.
        /// </summary>
        public string RegistryView
        {
            get { return Enum.GetName(typeof(RegistryView), _view); }

            set
            {
                _view = (RegistryView)Enum.Parse(typeof(RegistryView), value);
            }
        }

        /// <summary>
        /// Name of the value. If not set, then the default value is used.
        /// </summary>
        public string Value { get; set; }
    }
}
