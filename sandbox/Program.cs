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

using libconsole2;

using Fusion.Framework;

namespace sandbox
{
    class Program
    {
        static ILog _log = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// Application entry-point function.
        /// </summary>
        /// <param name="args">command-line arguments</param>
        /// <returns>an error code</returns>
        static int Main(string[] args)
        {
            string consid = Environment.GetEnvironmentVariable("CONSOLE");
            if (!String.IsNullOrEmpty(consid)) {
                ConsoleEx.Detach();
                ConsoleEx.Attach(Convert.ToUInt32(consid));
            }

            if (args.Length != 1) {
                Console.WriteLine("USAGE: sandbox <project file>");
                return 1;
            }

            if (!File.Exists(args[0])) {
                Console.WriteLine("sandbox: project file doesn't exist!");
                return 1;
            }

            SandboxDirectory sbox = SandboxDirectory.Open(new DirectoryInfo(Path.GetDirectoryName(args[0])));
            GlobalContext.Properties["logfile"] = sbox.Root.FullName + @"\build.log";
            XmlConfigurator.Configure();

            Environment.SetEnvironmentVariable("TEMP", sbox.TempDir.FullName);
            Environment.SetEnvironmentVariable("TMP", sbox.TempDir.FullName);

            try {
                Stream stream = new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.None);
                IInstallProject installer = (IInstallProject)(new BinaryFormatter()).Deserialize(stream);
                stream.Close();

                if (installer.HasSrcUnpackTarget) {
                    Console.WriteLine("\n>>> Unpacking source...");
                    installer.SrcUnpack();
                }

                if (installer.HasSrcCompileTarget) {
                    Console.WriteLine("\n>>> Compiling source in {0}", sbox.WorkDir.FullName);
                    installer.SrcCompile();
                }

                if (installer.HasSrcTestTarget) {
                    Console.WriteLine("\n>>> Running build tests...");
                    installer.SrcTest();
                }

                if (installer.HasSrcInstallTarget) {
                    Console.WriteLine("\n>>> Install {0} into {1}", installer.PackageName, sbox.ImageDir.FullName);
                    installer.SrcInstall();
                }
            } catch (Exception ex) {
                Func<Exception, Delegate, Exception> GetRootException = delegate(Exception outer, Delegate fn) {
                    if (outer.InnerException != null)
                        return (Exception)fn.DynamicInvoke(outer.InnerException, fn);

                    return outer;
                };

                Exception rootex = GetRootException(ex, GetRootException);
                _log.Fatal(rootex.Message);
                _log.FatalFormat("See log file at {0}", GlobalContext.Properties["logfile"]);
                _log.Debug(rootex.StackTrace);

                return 1;
            }

            return 0;
        }
    }
}
