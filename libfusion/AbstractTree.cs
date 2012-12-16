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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// A collection of ports arranged by category.
    /// </summary>
    public abstract class AbstractTree
    {
        /// <summary>
        /// Determines if the given distribution is masked, either by the 
        /// profile or by keyword.
        /// </summary>
        /// <param name="dist">the distribution to check</param>
        /// <returns>true if masked, false otherwise</returns>
        public abstract bool IsMasked(IDistribution dist);

        /// <summary>
        /// Determines if the given distribution is masked by profile.
        /// </summary>
        /// <param name="dist">the distribution to check</param>
        /// <returns>true if masked, false otherwise</returns>
        public abstract bool IsHardMasked(IDistribution dist);

        /// <summary>
        /// Determines if the given distribution is masked by keywords.
        /// </summary>
        /// <param name="dist">the distribution to check</param>
        /// <returns>true if masked, false otherwise</returns>
        public abstract bool IsKeywordMasked(IDistribution dist);

        /// <summary>
        /// Gets the package distribution referenced by the given atom.
        /// If multiple distributions match, then the latest version is
        /// returned.
        /// </summary>
        /// <param name="atom">the package atom to lookup</param>
        /// <returns>the matching distribution</returns>
        public virtual IDistribution Lookup(Atom atom)
        {
            return this.LookupAll(atom)
                .OrderBy(i => i.Version)
                .Last();
        }

        /// <summary>
        /// Gets all package distributions referenced by the given atom.
        /// </summary>
        /// <param name="atom">the package atom to lookup</param>
        /// <returns>the matching distributions</returns>
        public virtual IDistribution[] LookupAll(Atom atom)
        {
            IPackage pkg = this.ResolvePackage(atom);
            List<IDistribution> results = new List<IDistribution>();

            if (pkg == null)
                throw new PackageNotFoundException(atom.PackageName);

            if (atom.HasVersion || atom.Slot > 0) {        /* find a specific version */
                IDistribution[] matcharr = pkg.Distributions
                    .Where(i => atom.Match(i.Atom))
                    .OrderBy(i => i.Version)
                    .ToArray();
                if (matcharr.Length == 0)
                    throw new DistributionNotFoundException(atom);

                if (atom.Comparison == "=")
                    results.Add(matcharr.SingleOrDefault());
                else {
                    IDistribution[] unmasked = matcharr
                        .Where(i => !i.PortsTree.IsMasked(i))
                        .ToArray();
                    results.AddRange(unmasked.Length > 0 ? unmasked : matcharr);
                }
            } else {                        /* find the best version */
                if (pkg.LatestUnmasked != null)
                    results.Add(pkg.LatestUnmasked);
                else if (pkg.LatestAvailable != null)
                    results.Add(pkg.LatestAvailable);
            }

            if (results.Count == 0)
                throw new DistributionNotFoundException(atom);

            return results.ToArray();
        }

        /// <summary>
        /// Finds the package matching the given atom.
        /// </summary>
        /// <param name="atom">an atom</param>
        /// <returns>the matching package, or NULL if no match is found</returns>
        protected virtual IPackage ResolvePackage(Atom atom)
        {
            ReadOnlyCollection<IPackage> pkglst = this.Packages;
            IPackage pkg = null;

            if (atom.IsFullName) {
                pkg = pkglst
                    .Where(i => i.FullName == atom.PackageName)
                    .SingleOrDefault();
            } else {
                IPackage[] pkgarr = pkglst
                    .Where(i => i.Name == atom.PackageName)
                    .ToArray();

                if (pkgarr.Length == 1)         /* one match found */
                    pkg = pkgarr[0];
                else if (pkgarr.Length > 1)     /* multiple matches found */
                    throw new AmbiguousMatchException(atom.PackageName);
            }

            return pkg;
        }

        /// <summary>
        /// Searches the ports tree for the given string.
        /// </summary>
        /// <param name="atom">string to search</param>
        /// <returns>a list of packages matching the given string</returns>
        public List<IPackage> Search(string atom)
        {
            return this.Search(atom, false);
        }

        /// <summary>
        /// Searches the ports tree for the given string.
        /// </summary>
        /// <param name="name">string to search</param>
        /// <param name="fexact">flag to force exact name matching</param>
        /// <returns>a list of packages matching the given string</returns>
        public virtual List<IPackage> Search(string name, bool fexact)
        {
            List<IPackage> results = new List<IPackage>();

            foreach (ICategory c in this.Categories) {
                List<IPackage> plst = fexact ?
                    c.Packages.Where(p => p.Name == name && p.Distributions.Count > 0).ToList() :
                    c.Packages.Where(p => p.Name.Contains(name) && p.Distributions.Count > 0).ToList();
                plst.ForEach(p => results.Add(p));
            }

            return results;
        }

        /// <summary>
        /// All package categories in the tree.
        /// </summary>
        public abstract ReadOnlyCollection<ICategory> Categories { get; }

        /// <summary>
        /// All packages in the tree.
        /// </summary>
        public ReadOnlyCollection<IPackage> Packages
        {
            get
            {
                List<IPackage> pkglst = new List<IPackage>();

                foreach (ICategory cat in this.Categories)
                    pkglst.AddRange(cat.Packages);

                return new ReadOnlyCollection<IPackage>(pkglst);
            }
        }

        /// <summary>
        /// Ports repository identifier.
        /// </summary>
        public abstract string Repository { get; }
    }
}
