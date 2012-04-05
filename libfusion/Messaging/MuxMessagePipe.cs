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

namespace Fusion.Framework.Messaging
{
    /// <summary>
    /// Relays messages through a multiplexed named pipe.
    /// </summary>
    public sealed class MuxMessagePipe
    {
        private StreamWriter _sw;

        /// <summary>
        /// Initialises the client side of the event sink.
        /// </summary>
        /// <param name="npss">named pipe client stream</param>
        public MuxMessagePipe(NamedPipeClientStream npcs)
        {
            if (!npcs.CanWrite || npcs.CanRead)
                throw new InvalidOperationException("Named pipe must be write-only.");

            _sw = new StreamWriter(npcs);

            _sw.AutoFlush = true;
            _sw.NewLine = "\n";
        }

        /// <summary>
        /// Writes a message to the named pipe.
        /// </summary>
        /// <param name="chan">message channel</param>
        /// <param name="msg">message</param>
        public void WriteMessage(Channel chan, string msg)
        {
            this.WriteMessage(chan, SubType.None, msg);
        }

        /// <summary>
        /// Writes a message to the named pipe.
        /// </summary>
        /// <param name="chan">message channel</param>
        /// <param name="subtype">message sub-type</param>
        /// <param name="msg">message</param>
        public void WriteMessage(Channel chan, SubType subtype, string msg)
        {
            char[] msgbuf = msg.ToCharArray();

            char[] swbuf = new char[msgbuf.Length + 2];
            swbuf[0] = (char)chan;
            swbuf[1] = (char)subtype;
            Array.Copy(msgbuf, 0, swbuf, 2, msgbuf.Length);

            _sw.WriteLine(swbuf, 0, swbuf.Length);
        }
    }
}
