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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace bootstrap
{
    class Program
    {
        /// <summary>
        /// Application entry-point function.
        /// </summary>
        /// <param name="args">command-line arguments</param>
        /// <returns>an error code</returns>
        static void Main(string[] args)
        {
            Console.WriteLine("Fusion - http://github.com/rcarz/fusion\n");

            if (Environment.OSVersion.Version.Major < 6) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: your Windows version is not supported!");
                Console.ResetColor();
                Console.WriteLine("\nFusion is only compatible with Windows Vista/7/8/2008.");
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey(true);
                return;
            }

            string path = Environment.GetFolderPath(Environment.SpecialFolder.System);
            if (!Directory.Exists(path + @"\..\Microsoft.NET\Framework\v4.0.30319")) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: .NET Framework 4.0 is not installed!");
                Console.ResetColor();
                Console.WriteLine("\nYou will be redirected to the Microsoft download website. " +
                    "Re-run Fusion\nsetup after you've installed .NET Framework 4.0.");
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey(true);

                Process browser = Process.Start("http://www.microsoft.com/en-us/download/details.aspx?id=17851");
                return;
            }

            string asmpath =
                        Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
            string home = Path.GetDirectoryName(asmpath);

            Directory.CreateDirectory(home + @"\distfiles");

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = Path.GetDirectoryName(asmpath) + @"\bin\fuse.exe";
            psi.Arguments = "fusion";
            psi.EnvironmentVariables.Add("FUSION_HOME", home);
            psi.UseShellExecute = false;

            Process fuse = Process.Start(psi);
            fuse.WaitForExit();

            string newhome = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Fusion";
            if (!Directory.Exists(newhome)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Fusion installation failed!");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey(true);
                return;
            }

            File.Copy(home + @"\etc\fusion.s3db", newhome + @"\etc\fusion.s3db", true);

            Environment.SetEnvironmentVariable(
                "PATH",
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine));

            psi = new ProcessStartInfo();
            psi.FileName = "fuse.exe";
            psi.Arguments = "--sync";
            psi.UseShellExecute = false;

            fuse = Process.Start(psi);
            fuse.WaitForExit();

            psi = new ProcessStartInfo();
            psi.FileName = "fusion-config.exe";
            psi.Arguments = "-a";
            psi.UseShellExecute = false;

            fuse = Process.Start(psi);
            fuse.WaitForExit();

            Console.WriteLine("\nFusion installation completed successfully. Type 'fuse' at a command prompt.");
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey(true);
        }
    }
}
