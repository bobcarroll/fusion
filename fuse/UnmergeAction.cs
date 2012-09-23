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
    /// Removes packages from the system.
    /// </summary>
    class UnmergeAction : IAction
    {
        private List<string> _atomlst;
        private Options _options;

        /// <summary>
        /// Initialises a new unmerge action.
        /// </summary>
        public UnmergeAction()
        {
            _atomlst = new List<string>();
        }

        /// <summary>
        /// Executes this action.
        /// </summary>
        /// <param name="pkgmgr">package manager instance</param>
        public void Execute(IPackageManager pkgmgr)
        {
            Security.DemandNTAdmin();

            List<Atom> atomlst = Atom.ParseAll(_atomlst.ToArray()).ToList();
            List<Atom> uninstlst = new List<Atom>();

            foreach (Atom atom in atomlst) {
                Atom[] instatoms = pkgmgr.FindPackages(atom);

                if (instatoms.Length == 0) {
                    throw new PackageNotFoundException(atom.ToString());
                } else if (instatoms.Length == 1) {
                    uninstlst.Add(instatoms[0]);
                } else if (instatoms.Length > 1) {
                    Console.WriteLine("\nInstalled packages matching the given atom:");
                    foreach (Atom ia in instatoms) {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("\n * ");
                        Console.ResetColor();
                        Console.Write(ia.ToString());
                    }
                    Console.Write("\n");

                    throw new AmbiguousMatchException(atom.ToString());
                }
            }

            Atom[] protectpkg = uninstlst.Where(i => pkgmgr.IsProtected(i)).ToArray();
            if (protectpkg.Length > 0) {
                Console.Write("\nThe following package(s) are ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("protected");
                Console.ResetColor();
                Console.Write(" and will NOT be removed:\n");

                foreach (Atom atom in protectpkg) {
                    uninstlst.Remove(atom);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("\n * ");
                    Console.ResetColor();
                    Console.Write(atom.ToString());
                }
                Console.Write("\n");

                if (uninstlst.Count == 0) {
                    Console.WriteLine("\nNothing to do, exitting...");
                    return;
                }
            }

            Console.WriteLine("\nThe following package(s) will be removed:");
            foreach (Atom atom in uninstlst) {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\n * ");
                Console.ResetColor();
                Console.Write(atom.ToString());
            }

            Console.WriteLine("\n\n>>> Waiting 5 seconds before starting...");
            Console.WriteLine(">>> Press Control-C to abort");
            Console.Write(">>> Unmerging in ");

            for (int i = 5; i > 0; i--) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("{0} ", i);
                Console.ResetColor();

                Console.Beep();
                System.Threading.Thread.Sleep(1000);
            }
            Console.Write("\n");

            MergeWorker mw = new MergeWorker(pkgmgr);
            mw.OnUnmerge += this.MergeWorker_OnUnmerge;

            mw.Unmerge(uninstlst.ToArray());
        }

        /// <summary>
        /// Handler for the MergeWorker.OnUnmerge event.
        /// </summary>
        /// <param name="sender">the merge worker</param>
        /// <param name="e">event args</param>
        public void MergeWorker_OnUnmerge(object sender, UnmergeEventArgs e)
        {
            Console.WriteLine("\n>>> Unmerging {0}", e.Package.ToString());
        }

        /// <summary>
        /// A list of package atoms to unmerge.
        /// </summary>
        public List<string> Atoms
        {
            get { return _atomlst; }
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
