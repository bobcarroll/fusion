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
using System.Linq;
using System.Text;

using Fusion.Framework;

namespace fuse
{
    /// <summary>
    /// Clean packages not in the world set.
    /// </summary>
    class DepCleanAction : IAction
    {
        private Options _options;

        /// <summary>
        /// Initialises the dependency cleaner action.
        /// </summary>
        public DepCleanAction() { }

        /// <summary>
        /// Executes this action.
        /// </summary>
        /// <param name="pkgmgr">package manager instance</param>
        public void Execute(IPackageManager pkgmgr)
        {
            Atom[] unselected = pkgmgr.GetInstalledPackages()
                .Where(i => !pkgmgr.IsSelected(i))
                .ToArray();

            AbstractTree tree = LocalRepository.Read();
            List<IDistribution> worlddists = new List<IDistribution>();
            foreach (Atom a in pkgmgr.WorldSet)
                worlddists.Add(tree.Lookup(a));

            DependencyGraph dg = DependencyGraph.Compute(worlddists.ToArray());
            List<Atom> orphaned = new List<Atom>();

            foreach (Atom a in unselected) {
                try {
                    dg.CheckSatisfies(a);
                } catch (KeyNotFoundException) {
                    orphaned.Add(a);
                }
            }

            if (orphaned.Count == 0) {
                Console.WriteLine("There are no packages to clean up. :)");
                return;
            }

            if (_options.pretend) {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("\n>>> These are the packages that would be removed:");
                Console.ResetColor();
            }

            UnmergeAction ua = new UnmergeAction(10);
            ua.Atoms.AddRange(orphaned.Select(i => i.ToString()));
            ua.Options = _options;
            ua.Execute(pkgmgr);
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
