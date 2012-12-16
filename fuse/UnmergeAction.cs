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
        private uint _delay;

        /// <summary>
        /// Initialises a new unmerge action.
        /// </summary>
        public UnmergeAction()
            : this(5) { }

        /// <summary>
        /// Initialises a new unmerge action.
        /// </summary>
        /// <param name="delay">delay in seconds</param>
        public UnmergeAction(uint delay)
        {
            _atomlst = new List<string>();
            _delay = delay;
        }

        /// <summary>
        /// Executes this action.
        /// </summary>
        /// <param name="pkgmgr">package manager instance</param>
        public void Execute(IPackageManager pkgmgr)
        {
            Security.DemandNTAdmin();

            List<Atom> atomlst = Atom.ParseAll(_atomlst.ToArray()).ToList();
            List<Atom> allselected = new List<Atom>();
            List<Atom> allprotected = new List<Atom>();
            List<Atom> allomitted = new List<Atom>();

            foreach (Atom atom in atomlst) {
                Atom[] instatoms = pkgmgr.FindPackages(atom);

                if (instatoms.Length == 0)
                    throw new PackageNotFoundException(atom.ToString());

                allselected.AddRange(instatoms);
            }

            allprotected = allselected
                .Where(i => pkgmgr.IsProtected(i))
                .ToList();
            foreach (Atom a in allprotected)
                allselected.Remove(a);

            allomitted = pkgmgr.GetInstalledPackages()
                .Where(i => allselected.Where(
                    s => s.PackageName == i.PackageName).Count() > 0)
                .Where(i => allselected.Where(
                    s => s.PackageName == i.PackageName &&
                         s.Version == i.Version).Count() == 0)
                .ToList();

            string[] allpackages = allselected
                .Select(i => i.PackageName)
                .Union(allprotected.Select(i => i.PackageName))
                .Union(allomitted.Select(i => i.PackageName))
                .OrderBy(i => i)
                .ToArray();

            foreach (string pkg in allpackages) {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\n {0}", pkg);
                Console.ResetColor();

                string[] pkgselected = allselected
                    .Where(i => i.PackageName == pkg)
                    .OrderBy(i => i.Version)
                    .Select(i => i.Version.ToString())
                    .ToArray();
                string[] pkgprotected = allprotected
                    .Where(i => i.PackageName == pkg)
                    .OrderBy(i => i.Version)
                    .Select(i => i.Version.ToString())
                    .ToArray();
                string[] pkgomitted = allomitted
                    .Where(i => i.PackageName == pkg)
                    .OrderBy(i => i.Version)
                    .Select(i => i.Version.ToString())
                    .ToArray();

                Console.Write("    selected: ");
                if (pkgselected.Length > 0) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(String.Join(" ", pkgselected));
                    Console.ResetColor();
                } else
                    Console.WriteLine("none");

                Console.Write("   protected: ");
                if (pkgprotected.Length > 0) {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(String.Join(" ", pkgprotected));
                    Console.ResetColor();
                } else
                    Console.WriteLine("none");

                Console.Write("     omitted: ");
                if (pkgomitted.Length > 0) {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(String.Join(" ", pkgomitted));
                    Console.ResetColor();
                } else
                    Console.WriteLine("none");
            }

            Console.WriteLine(
                "\nAll selected packages: {0}",
                String.Join(" ", allselected.Select(i => i.ToString())));

            Console.Write("\n>>> ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Selected");
            Console.ResetColor();
            Console.WriteLine(" packages are scheduled for removal.");

            Console.Write(">>> ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Protected");
            Console.ResetColor();
            Console.Write(" and ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("omitted");
            Console.ResetColor();
            Console.WriteLine(" packages will not be removed.");

            if (_options.pretend || allselected.Count == 0)
                return;

            Console.WriteLine(
                "\n>>> Waiting {0} {1} before starting...",
                _delay,
                _delay == 1 ? "second" : "seconds");
            Console.WriteLine(">>> Press Control-C to abort");
            Console.Write(">>> Unmerging in ");

            for (uint i = _delay; i > 0; i--) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("{0} ", i);
                Console.ResetColor();

                Console.Beep();
                System.Threading.Thread.Sleep(1000);
            }
            Console.Write("\n");

            MergeWorker mw = new MergeWorker(pkgmgr);
            mw.OnUnmerge += this.MergeWorker_OnUnmerge;

            mw.Unmerge(allselected.ToArray());
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
