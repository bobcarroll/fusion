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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using libconsole2;

using Fusion.Framework;

namespace fusion_config
{
    class Program
    {
        private const uint SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CreateSymbolicLink([In] string lpSymlinkFileName,
            [In] string lpTargetFileName, uint dwFlags);

        /* option group names */
        static string[] _groups = new string[] {
            "Options"
        };

        /* command-line options */
        static OptionExtra[] _options = new OptionExtra[] {
            new OptionExtra() { 
                Option = new Option() { Name = "list-profiles", Val = 'l' },
                Description = "Print a list of available profiles.",
                InGroup = _groups[0] }
        };

        /// <summary>
        /// Application entry-point function.
        /// </summary>
        /// <param name="args">command-line arguments</param>
        /// <returns>an error code</returns>
        static int Main(string[] args)
        {
            string[] argv = System.Environment.GetCommandLineArgs();
            OptionTransformer ot = new OptionTransformer(_options);
            OptionPrinter op = new OptionPrinter(
                Path.GetFileName(argv[0]) + " [ options ] [ profile ]",
                _options,
                _groups);
            GlibcOpt gopt = new GlibcOpt();
            int longindex = 0;
            int result;
            bool list = false;

            if (args.Length == 0) {
                op.PrintUsage();
                return 0;
            }

            while ((result = gopt.GetOptLong(ref argv, ot.ToOptString(), ot.ToLongOpts(), ref longindex)) != -1) {

                switch (result) {
                    case 'l':
                        list = true;
                        break;
                }
            }

            XmlConfiguration cfg = XmlConfiguration.LoadSeries();
            DirectoryInfo profroot = 
                new DirectoryInfo(cfg.PortDir + @"\" + XmlConfiguration.PROFILEDIR);
            DirectoryInfo[] profiles = profroot.GetDirectories("*", SearchOption.AllDirectories)
                .Where(i => File.ReadAllLines(i.FullName + @"\parent").Length > 1)
                .OrderBy(i => i.FullName)
                .ToArray();
            int striplev = profroot.FullName.TrimEnd('\\').Count(i => i == '\\') + 1;
            ReparsePoint current = cfg.ProfileDir.Exists ?
                new ReparsePoint(cfg.ProfileDir.FullName) :
                null;
            int padding = (int)Math.Log10(profiles.Length);

            if (list) {
                for (int i = 0; i < profiles.Length; i++) {
                    string name = String.Join("/", profiles[i].FullName.Split('\\').Skip(striplev));
                    string deprecated = profiles[i].GetFiles("deprecated").Length == 1 ?
                        " (deprecated)" :
                        "";
                    Console.Write(" [{0}] {1}{2}", (i + 1).ToString().PadLeft(padding), name, deprecated);

                    if (current != null && profiles[i].FullName == current.ToString()) {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(" *");
                        Console.ResetColor();
                    }

                    Console.Write("\n");
                }
            } else if (gopt.NextOption < argv.Length) {
                uint choice = 0;
                if (!uint.TryParse(argv[gopt.NextOption], out choice) || --choice >= profiles.Length) {
                    Console.WriteLine("Invalid profile selection");
                    return 1;
                }

                if (cfg.ProfileDir.Exists)
                    Directory.Delete(cfg.ProfileDir.FullName);

                bool success = CreateSymbolicLink(
                    cfg.ProfileDir.FullName, 
                    profiles[choice].FullName, 
                    SYMBOLIC_LINK_FLAG_DIRECTORY);
                if (!success) {
                    Console.WriteLine("Failed to create symbolic link");
                    return 1;
                }

                string name = String.Join("/", profiles[choice].FullName.Split('\\').Skip(striplev));
                Console.WriteLine("{0} is now the active profile", name);
            }

            return 0;
        }
    }
}
