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

namespace Fusion.Framework.Messaging
{
    /// <summary>
    /// De-multiplexes messages recevied through a the message sink.
    /// </summary>
    public sealed class MessageDemuxer
    {
        /// <summary>
        /// Delegate for demux callback functions.
        /// </summary>
        /// <param name="subtype">message sub-type</param>
        /// <param name="msg">message</param>
        /// <param name="param">handler parameter</param>
        public delegate void DemuxerDelegate(SubType subtype, string msg, object param);

        private Dictionary<Channel, Tuple<DemuxerDelegate, object>> _dict;
        
        /// <summary>
        /// Initialises the demuxer.
        /// </summary>
        public MessageDemuxer()
        {
            _dict = new Dictionary<Channel, Tuple<DemuxerDelegate, object>>(); 
        }

        /// <summary>
        /// Sets the handler for the given channel.
        /// </summary>
        /// <param name="chan">channel</param>
        /// <param name="handler">handler</param>
        public void SetChannelHandler(Channel chan, DemuxerDelegate handler)
        {
            this.SetChannelHandler(chan, handler, null);
        }

        /// <summary>
        /// Sets the handler for the given channel.
        /// </summary>
        /// <param name="chan">channel</param>
        /// <param name="handler">handler</param>
        /// <param name="param">handler parameter</param>
        public void SetChannelHandler(Channel chan, DemuxerDelegate handler, object param)
        {
            _dict[chan] = new Tuple<DemuxerDelegate, object>(handler, param);
        }

        /// <summary>
        /// De-multiplexes the given message data.
        /// </summary>
        /// <param name="data">message data</param>
        public void Demux(string data)
        {
            char[] buf = data.ToCharArray();

            if (buf.Length < 3 || !Enum.IsDefined(typeof(Channel), (int)buf[0]))
                return;

            Channel chan = (Channel)buf[0];
            SubType subtype = SubType.None;
            string msg = new String(buf, 2, buf.Length - 2);

            if (Enum.IsDefined(typeof(SubType), (int)buf[1]))
                subtype = (SubType)buf[1];

            Tuple<DemuxerDelegate, object> cbinfo = _dict[chan];

            if (cbinfo != null)
                cbinfo.Item1(subtype, msg, cbinfo.Item2);
        }
    }
}
