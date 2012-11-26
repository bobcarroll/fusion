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

using Fusion.Framework;

namespace fuse
{
    /// <summary>
    /// Performs searches against the ports tree.
    /// </summary>
    class SearchAction : IAction
    {
        private string _atom;
        private Options _options;

        /// <summary>
        /// Initialises the search action with an atom.
        /// </summary>
        /// <param name="atom">the search string</param>
        public SearchAction(string atom)
        {
            _atom = atom;
        }

        /// <summary>
        /// Executes this action.
        /// </summary>
        /// <param name="pkgmgr">package manager instance</param>
        public void Execute(IPackageManager pkgmgr)
        {
            AbstractTree tree = LocalRepository.Read();
            List<IPackage> results = tree.Search(_atom, _options.exact);

            Console.WriteLine("\n[ Packages found: {0} ]", results.Count);

            if (results.Count > 0) {
                foreach (IPackage p in results) {
                    bool fmasked = false;

                    IDistribution latest = p.LatestUnmasked;
                    if (latest == null) {
                        latest = p.LatestAvailable;
                        fmasked = true;
                    }

                    Version iv = pkgmgr.QueryInstalledVersion(latest.Atom);
                    string ivstr = iv != null ? iv.ToString() : "[ Not Installed ]";

                    StringBuilder sizesb = new StringBuilder(11);
                    Win32.StrFormatByteSize(latest.TotalSize, sizesb, sizesb.Capacity);

                    Console.Write("\n");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("*");
                    Console.ResetColor();
                    Console.Write("  {0}", p.FullName);

                    if (fmasked) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("  [ Masked ]\n");
                        Console.ResetColor();
                    } else
                        Console.Write("\n");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("      Latest version available:");
                    Console.ResetColor();
                    Console.Write(" {0}\n", latest.Version.ToString());

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("      Latest version installed:");
                    Console.ResetColor();
                    Console.Write(" {0}\n", ivstr);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("      Size of files:");
                    Console.ResetColor();
                    Console.Write(" {0}\n", sizesb);

                    if (!String.IsNullOrEmpty(p.Homepage)) {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("      Homepage:");
                        Console.ResetColor();
                        Console.Write(" {0}\n", p.Homepage);
                    }

                    if (!String.IsNullOrEmpty(p.Description)) {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("      Description:");
                        Console.ResetColor();
                        Console.Write(" {0}\n", p.Description);
                    }

                    if (!String.IsNullOrEmpty(p.License)) {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("      License:");
                        Console.ResetColor();
                        Console.Write(" {0}\n", p.License);
                    }
                }
            }

            Console.Write("\n");
        }

        /// <summary>
        /// Command options structure.
        /// </summary>
        public Options Options
        {
            get { return _options; }
            set { _options = value; }
        }
    }
}
