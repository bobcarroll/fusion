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

namespace graphcheck
{
    class Program
    {
        /// <summary>
        /// Application entry-point function.
        /// </summary>z
        /// <param name="args">command-line arguments</param>
        /// <returns>an error code</returns>z
        static int Main(string[] args)
        {
            AbstractTree tree = LocalRepository.Read();
            List<IDistribution> dists = new List<IDistribution>();

            foreach (IPackage pkg in tree.Packages)
                dists.AddRange(pkg.Distributions);

            Console.WriteLine("Checking {0} package(s)...", dists.Count);

            List<string> known = new List<string>();
            foreach (IDistribution d in dists) {
                try {
                    DependencyGraph.Compute(new IDistribution[] { d });
                } catch (CircularReferenceException ex) {
                    if (!known.Contains(ex.Message)) {
                        Console.WriteLine(ex.Message);
                        known.Add(ex.Message);
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }

            return 0;
        }
    }
}
