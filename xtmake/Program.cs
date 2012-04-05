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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;

using Fusion.Framework;
using Fusion.Framework.Messaging;

namespace xtmake
{
    class Program
    {
        static ILog _log = LogManager.GetLogger(typeof(Program));
        static MuxMessagePipe _mmp = null;

        /// <summary>
        /// Application entry-point function.
        /// </summary>
        /// <param name="args">command-line arguments</param>
        /// <returns>an error code</returns>
        static int Main(string[] args)
        {
            if (args.Length < 1) {
                Console.WriteLine("USAGE: xtmake <project file> [pipe name]");
                return 1;
            }

            if (!File.Exists(args[0])) {
                Console.WriteLine("xtmake: project file doesn't exist!");
                return 1;
            }

            XmlConfigurator.Configure();

            try {
                Stream stream = new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.None);
                IInstallProject installer = (IInstallProject)(new BinaryFormatter()).Deserialize(stream);
                stream.Close();

                if (args.Length >= 2) {
                    _log.DebugFormat("Message pipe name is {0}", args[1]);

                    try {
                        NamedPipeClientStream npcs = new NamedPipeClientStream(".", args[1], PipeDirection.Out);
                        npcs.Connect(3000);
                        _log.InfoFormat("Connected to message pipe");

                        _mmp = new MuxMessagePipe(npcs);
                        PipeAppender pa = new PipeAppender(_mmp, Channel.FrameworkLogger);
                        ((Hierarchy)LogManager.GetRepository()).Root.AddAppender(pa);
                    } catch (TimeoutException) {
                        _log.ErrorFormat("Failed to connect to message pipe");
                    };
                }
            } catch (Exception ex) {
                Func<Exception, Delegate, Exception> GetRootException = delegate(Exception outer, Delegate fn) {
                    if (outer.InnerException != null)
                        return (Exception)fn.DynamicInvoke(outer.InnerException, fn);

                    return outer;
                };

                Exception rootex = GetRootException(ex, GetRootException);
                _log.Fatal(rootex.Message);
                _log.Debug(rootex.StackTrace);

                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Writes a merge message to the message pipe, if one is set.
        /// </summary>
        /// <param name="msg">the message</param>
        static void RaiseMergeMessage(string msg)
        {
            if (_mmp != null)
                _mmp.WriteMessage(Channel.MergeMessage, msg);
        }
    }
}
