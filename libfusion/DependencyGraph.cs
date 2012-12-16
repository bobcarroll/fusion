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
    /// Represents a package dependency graph.
    /// </summary>
    public sealed class DependencyGraph
    {
        /// <summary>
        /// Dependency item structure.
        /// </summary>
        public struct Dependency
        {
            public Atom Atom;
            public IDistribution[] Matches;
            public IDistribution Selected;
            public IDistribution PulledInBy;
        }

        /// <summary>
        /// Slot conflict item.
        /// </summary>
        public struct Conflict
        {
            public IPackage Package;
            public uint Slot;
            public IDistribution[] Distributions;
            public Dictionary<IDistribution, IDistribution[]> ReverseMap;
        }

        /// <summary>
        /// A dependency node structure.
        /// </summary>
        public class Node
        {
            public IDistribution dist;
            public Node firstchild;
            public Node next;
        }

        private IDistribution[] _sorted;
        private Node _root;
        private Dictionary<string, List<Dependency>> _depmap;

        /// <summary>
        /// Initialises the dependency graph.
        /// </summary>
        /// <param name="transrel">a table of distribution transitive relationships</param>
        /// <param name="depmap">a mapping of package and dependency metadata</param>
        private DependencyGraph(Dictionary<IDistribution, List<IDistribution>> transrel,
            Dictionary<string, List<Dependency>> depmap)
        {
            _sorted = DependencyGraph.TopoSort(transrel);
            _depmap = depmap;

            Dictionary<IDistribution, Node> nodemap = new Dictionary<IDistribution, Node>();
            List<Node> visited = new List<Node>();
            Node tail;

            /* build the dependency graph */
            foreach (IDistribution dist in _sorted) {
                Node node = new Node();
                node.dist = dist;

                nodemap.Add(dist, node);

                foreach (IDistribution dep in transrel[dist]) {
                    tail = node.firstchild;

                    if (tail == null) {
                        Node first = nodemap[dep];
                        if (!visited.Contains(first)) {
                            node.firstchild = first;
                            visited.Add(first);
                        }
                    } else {
                        while (tail.next != null)
                            tail = tail.next;

                        Node next = nodemap[dep];
                        if (!visited.Contains(next)) {
                            tail.next = nodemap[dep];
                            visited.Add(next);
                        }
                    }
                }
            }

            /* find root nodes */
            IDistribution[] rootdists = transrel.Keys
                .Where(k => transrel.Values.Where(v => v.Contains(k)).Count() == 0)
                .ToArray();
            _root = nodemap[rootdists[0]];
            tail = _root;

            for (int i = 1; i < rootdists.Length; i++) {
                while (tail.next != null)
                    tail = tail.next;

                tail.next = nodemap[rootdists[i]];
            }
        }

        /// <summary>
        /// Generates a dependency graph from the given distributions.
        /// </summary>
        /// <param name="distarr">an array of distributions</param>
        /// <returns>the new dependency graph</returns>
        public static DependencyGraph Compute(IDistribution[] distarr)
        {
            if (distarr.Length == 0)
                throw new InvalidOperationException("Distribution array is empty!");

            Dictionary<IDistribution, List<IDistribution>> transrel = 
                new Dictionary<IDistribution, List<IDistribution>>();
            Dictionary<string, List<Dependency>> depmap =
                new Dictionary<string, List<Dependency>>();
            
            DependencyGraph.DeepFind(distarr, transrel, depmap);
            DependencyGraph.CheckForCycles(transrel);

            return new DependencyGraph(transrel, depmap);
        }

        /// <summary>
        /// Determines if the dependency graph has circular references.
        /// </summary>
        /// <param name="transrel">a table of distribution transitive relationships</param>
        public static void CheckForCycles(Dictionary<IDistribution, List<IDistribution>> transrel)
        {
            Dictionary<IDistribution, List<IDistribution>> transrelcpy = 
                new Dictionary<IDistribution, List<IDistribution>>();
            foreach (KeyValuePair<IDistribution, List<IDistribution>> kvp in transrel)
                transrelcpy.Add(kvp.Key, new List<IDistribution>(kvp.Value));

            DependencyGraph.Expand(transrelcpy);

            foreach (KeyValuePair<IDistribution, List<IDistribution>> kvp in transrelcpy) {
                if (kvp.Value.Contains(kvp.Key))
                    throw new CircularReferenceException(kvp.Key.ToString());
            }
        }

        /// <summary>
        /// Determines if the given distribution satisfies dependency requirements for
        /// the current graph.
        /// </summary>
        /// <param name="dist">distribution atom to check</param>
        /// <returns>true if satisfies, false otherwise</returns>
        public bool CheckSatisfies(Atom atom)
        {
            if (!_depmap.ContainsKey(atom.PackageName))
                throw new KeyNotFoundException("Package '" + atom.PackageName + "' is not a dependency.");

            /* the given dist satisfies the requirement if it appears in all matches found */
            foreach (Dependency d in _depmap[atom.PackageName]) {
                if (d.Matches.Where(i => atom.Match(d.Atom)).Count() == 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Finds all dependencies of the given distributions.
        /// </summary>
        /// <param name="distarr">the distributions to visit</param>
        /// <param name="transrel">an empty table for transitive relationships</param>
        /// <param name="depmap">a mapping of package and dependency metadata</param>
        public static void DeepFind(IDistribution[] distarr, Dictionary<IDistribution, List<IDistribution>> transrel,
            Dictionary<string, List<Dependency>> depmap)
        {
            List<IDistribution> newdists = new List<IDistribution>();

            foreach (IDistribution dist in distarr) {
                List<IDistribution> mydeps = new List<IDistribution>();
                foreach (Atom a in dist.Dependencies) {
                    Dependency d = new Dependency() {
                        Atom = a,
                        Matches = dist.PortsTree.LookupAll(a),
                        PulledInBy = dist
                    };

                    /* select the highest version returned */
                    d.Selected = d.Matches
                        .OrderBy(i => i.Version)
                        .Last();
                    mydeps.Add(d.Selected);

                    /* cache all matching distributions */
                    string pkgkey = d.Selected.Package.FullName;
                    if (!depmap.ContainsKey(pkgkey))
                        depmap.Add(pkgkey, new List<Dependency>());
                    depmap[pkgkey].Add(d);
                }

                /* cache direct dependencies of the current dist */
                if (!transrel.Keys.Contains(dist))
                    transrel.Add(dist, mydeps);

                /* queue dependencies that haven't been visited */
                newdists.AddRange(mydeps.Except(transrel.Keys));
            }

            if (newdists.Count > 0)
                DependencyGraph.DeepFind(newdists.ToArray(), transrel, depmap);
        }

        /// <summary>
        /// Computes the transitive closure of each row in the relationships table.
        /// </summary>
        /// <param name="transrel">a table of distribution transitive relationships</param>
        public static void Expand(Dictionary<IDistribution, List<IDistribution>> transrel)
        {
            Dictionary<IDistribution, List<IDistribution>> depdictref = transrel;

            foreach (KeyValuePair<IDistribution, List<IDistribution>> kvp in transrel.ToList()) {
                List<IDistribution> alldeps = kvp.Value;
                
                while (true) {
                    /* resolve dependencies for all first-order dependencies */
                    List<IDistribution> resolvdeps = new List<IDistribution>();
                    alldeps.ForEach(i => resolvdeps.AddRange(depdictref[i]));

                    /* determine which dependencies were previously unseen */
                    List<IDistribution> newdeps = resolvdeps.Except(alldeps).ToList();
                    if (newdeps.Count == 0)
                        break;

                    alldeps = alldeps.Union(newdeps).ToList();
                }

                transrel[kvp.Key] = alldeps;
            }
        }

        /// <summary>
        /// Finds any slot conflicts that cause unsatisfied dependencies.
        /// </summary>
        /// <returns>an array of conflicted distributions</returns>
        public Conflict[] FindSlotConflicts()
        {
            /* first, find all slot conflicts */
            IDistribution[] conflicts = _sorted
                .Where(d => _sorted.Where(
                    dd => d.Package.FullName == dd.Package.FullName && d.Slot == dd.Slot).Count() > 1)
                .ToArray();

            Dictionary<string, List<IDistribution>> pkgdict = new Dictionary<string, List<IDistribution>>();
            foreach (string pkg in conflicts.Select(c => c.Package.FullName + ":" + c.Slot).Distinct())
                pkgdict.Add(pkg, new List<IDistribution>());

            foreach (IDistribution dist in conflicts)
                pkgdict[dist.Package.FullName + ":" + dist.Slot].Add(dist);

            List<Conflict> results = new List<Conflict>();

            /* scan the conflicts table for packages that satisfy dependency requirements,
             * and add dists that fail the check to the results set */
            foreach (KeyValuePair<string, List<IDistribution>> kvp in pkgdict) {
                bool pass = false;

                foreach (IDistribution d in kvp.Value) {
                    if (this.CheckSatisfies(d.Atom)) {
                        pass = true;
                        break;
                    }
                }

                if (!pass) {
                    IDistribution first = kvp.Value.First();
                    Conflict c = new Conflict() {
                        Package = first.Package,
                        Slot = first.Slot,
                        Distributions = kvp.Value.ToArray(),
                        ReverseMap = new Dictionary<IDistribution, IDistribution[]>()
                    };

                    foreach (IDistribution d in kvp.Value)
                        c.ReverseMap[d] = this.QueryPulledInBy(d);

                    results.Add(c);
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// Queries the dependency graph for the distributions that pulled in the
        /// given distribution as a dependency.
        /// </summary>
        /// <param name="dist">dependency dist to query</param>
        /// <returns>all distributions that pulled in dist</returns>
        public IDistribution[] QueryPulledInBy(IDistribution dist)
        {
            List<IDistribution> results = new List<IDistribution>();

            if (!_depmap.ContainsKey(dist.Package.FullName))
                throw new KeyNotFoundException("Package '" + dist.Package.FullName + "' is not a dependency.");

            foreach (Dependency d in _depmap[dist.Package.FullName].Where(i => i.Atom.Match(dist.Atom)))
                results.Add(d.PulledInBy);

            return results.Distinct().ToArray();
        }

        /// <summary>
        /// Performs a topological sort of the given transitive relationships.
        /// </summary>
        /// <param name="transrel">a table of distribution transitive relationships</param>
        /// <returns>a sorted list of distributions</returns>
        public static IDistribution[] TopoSort(Dictionary<IDistribution, List<IDistribution>> transrel)
        {
            Dictionary<IDistribution, List<IDistribution>> transrelcpy =
                new Dictionary<IDistribution, List<IDistribution>>();
            foreach (KeyValuePair<IDistribution, List<IDistribution>> kvp in transrel)
                transrelcpy.Add(kvp.Key, new List<IDistribution>(kvp.Value));

            /* find nodes with no outgoing edges */
            List<IDistribution> results = transrelcpy
                .Where(i => i.Value.Count == 0)
                .Select(i => i.Key)
                .ToList();

            while (transrelcpy.SelectMany(i => i.Value).Count() > 0) {
                List<IDistribution> lhsnodes = new List<IDistribution>();

                /* scan the relationships table and find LHS nodes with edges to nodes in the results 
                 * list. then remove all rhsnodes from the RHS of the table. */
                foreach (KeyValuePair<IDistribution, List<IDistribution>> kvp in transrelcpy.ToList()) {
                    if (kvp.Value.Intersect(results).Count() > 0)
                        lhsnodes.Add(kvp.Key);
                    kvp.Value.RemoveAll(i => results.Contains(i));
                }

                /* find nodes on the LHS that have no remaining outgoing edges */
                foreach (IDistribution lhs in lhsnodes) {
                    if (transrelcpy[lhs].Count == 0)
                        results.Add(lhs);
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// The graph root node.
        /// </summary>
        public Node RootNode
        {
            get { return _root; }
        }

        /// <summary>
        /// A sorted list of nodes.
        /// </summary>
        public ReadOnlyCollection<IDistribution> SortedNodes
        {
            get { return new ReadOnlyCollection<IDistribution>(_sorted); }
        }
    }
}
