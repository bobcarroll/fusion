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
using System.Security.Principal;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Provides security helpers.
    /// </summary>
    public static class Security
    {
        /// <summary>
        /// Determines if the Windows user that owns the thread has administrator
        /// rights on the local machine. If UAC is enabled, this function will fail
        /// for protected administrators and succeed for elevated administrators.
        /// </summary>
        /// <returns>true if admin, false otherwise</returns>
        public static bool IsNTAdmin()
        {
            WindowsPrincipal wp = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return wp.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// This function is a wrapper for Security.IsNTAdmin(). If the user is not
        /// an admin then an exception is thrown, otherwise no action is taken.
        /// </summary>
        public static void DemandNTAdmin()
        {
            if (!Security.IsNTAdmin())
                throw new UnauthorizedAccessException("You must be an administrator to execute this command.");
        }
    }
}
