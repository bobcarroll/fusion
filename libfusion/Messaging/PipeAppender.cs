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
using System.IO.Pipes;
using System.Linq;
using System.Text;

using log4net.Appender;
using log4net.Core;

namespace Fusion.Framework.Messaging
{
    /// <summary>
    /// Relays logger messages through a pipe.
    /// </summary>
    public sealed class PipeAppender : AppenderSkeleton
    {
        private MuxMessagePipe _pipe;
        private Channel _chan;

        /// <summary>
        /// Initialises the appender.
        /// </summary>
        /// <param name="pipe">client end of the pipe</param>
        /// <param name="chan">message channel</param>
        public PipeAppender(MuxMessagePipe pipe, Channel chan)
        {
            _pipe = pipe;
            _chan = chan;
        }

        /// <summary>
        /// Writes a log message to the message pipe.
        /// </summary>
        /// <param name="ev">log event</param>
        protected override void Append(LoggingEvent ev)
        {
            SubType subtype = SubType.Other;

            if (ev.Level == Level.Fatal)
                subtype = SubType.Fatal;
            else if (ev.Level == Level.Error)
                subtype = SubType.Error;
            else if (ev.Level == Level.Warn)
                subtype = SubType.Warn;
            else if (ev.Level == Level.Info)
                subtype = SubType.Info;
            else if (ev.Level == Level.Debug)
                subtype = SubType.Debug;

            _pipe.WriteMessage(_chan, subtype, ev.RenderedMessage);
        }
    }
}
