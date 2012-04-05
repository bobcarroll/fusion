﻿/**
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

namespace Fusion.Framework.Messaging
{
    /// <summary>
    /// Message channels.
    /// </summary>
    public enum Channel
    {
        /// <summary>
        /// Merge-level and phase messages. 
        /// </summary>
        MergeMessage = 0,

        /// <summary>
        /// Log messages for the general framework log.
        /// </summary>
        FrameworkLogger = 1,

        /// <summary>
        /// Log messages for the installer log.
        /// </summary>
        InstallerLogger = 2
    }
}
