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
using System.Threading;

using log4net;
using log4net.Core;

namespace Fusion.Framework.Messaging
{
    /// <summary>
    /// Receives messages from a named pipe.
    /// </summary>
    public sealed class MessageSink
    {
        private StreamReader _sr;
        private MessageDemuxer _demux;
        private Thread _thread;

        /// <summary>
        /// Initialises the server side of the event sink.
        /// </summary>
        /// <param name="npss">named pipe server stream</param>
        /// <param name="demux">demuxer instance</param>
        private MessageSink(NamedPipeServerStream npss, MessageDemuxer demux)
        {
            if (!npss.CanRead || npss.CanWrite)
                throw new InvalidOperationException("Named pipe must be read-only.");

            _sr = new StreamReader(npss);
            _demux = demux;
            _thread = new Thread(delegate(object param) {
                while (!_sr.EndOfStream) {
                    string msg = _sr.ReadLine();
                    _demux.Demux(msg);
                }
            });
        }

        /// <summary>
        /// Creates a new message sink from the given pipe and starts the pump thread.
        /// </summary>
        /// <param name="npss">named pipe server stream</param>
        /// <param name="demux">demuxer instance</param>
        /// <returns>message sink instance</returns>
        public static MessageSink Start(NamedPipeServerStream npss, MessageDemuxer demux)
        {
            MessageSink msgsink = new MessageSink(npss, demux);
            msgsink._thread.Start();

            return msgsink;
        }
    }
}
