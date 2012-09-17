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

using log4net.Appender;
using log4net.Core;

namespace Fusion.Framework
{
    /// <summary>
    /// Decorates the ConsoleAppender for custom output.
    /// </summary>
    public sealed class CustomConsoleAppender : ConsoleAppender
    {
        /// <summary>
        /// Writes a log event to the console.
        /// </summary>
        /// <param name="ev">log event</param>
        protected override void Append(LoggingEvent ev)
        {
            if (base.Target == ConsoleAppender.ConsoleOut) {
                if (ev.Level == Level.Fatal || ev.Level == Level.Error)
                    Console.ForegroundColor = ConsoleColor.Red;
                else if (ev.Level == Level.Warn)
                    Console.ForegroundColor = ConsoleColor.Yellow;
                else
                    Console.ForegroundColor = ConsoleColor.Green;

                Console.Write(" * ");
                Console.ResetColor();
            }

            base.Append(ev);
        }
    }
}
