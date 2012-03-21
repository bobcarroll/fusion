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

        /// <summary>
        /// Initialises the dependency graph.
        /// </summary>
        /// <param name="transrel">a table of distribution transitive relationships</param>
        private DependencyGraph(Dictionary<IDistribution, List<IDistribution>> transrel)
        {
            _sorted = DependencyGraph.TopoSort(transrel);

            Dictionary<IDistribution, Node> nodemap = new Dictionary<IDistribution, Node>();
            Node tail;

            /* build the dependency graph */
            foreach (IDistribution dist in _sorted) {
                Node node = new Node();
                node.dist = dist;

                nodemap.Add(dist, node);

                foreach (IDistribution dep in transrel[dist]) {
                    tail = node.firstchild;

                    if (tail == null)
                        node.firstchild = nodemap[dep];
                    else {
                        while (tail.next != null)
                            tail = tail.next;

                        tail.next = nodemap[dep];
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
            
            DependencyGraph.DeepFind(distarr, transrel);
            DependencyGraph.CheckForCycles(transrel);

            return new DependencyGraph(transrel);
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
        /// Finds all dependencies of the given distributions.
        /// </summary>
        /// <param name="distarr">the distributions to visit</param>
        /// <param name="transrel">an empty table for transitive relationships</param>
        public static void DeepFind(IDistribution[] distarr, Dictionary<IDistribution, List<IDistribution>> transrel)
        {
            List<IDistribution> newdists = new List<IDistribution>();

            foreach (IDistribution dist in distarr) {
                IDistribution[] mydeps = dist
                    .PortsTree.LookupAll(dist.Dependencies)
                    .ToArray();

                /* cache direct dependencies of the current dist */
                if (!transrel.Keys.Contains(dist))
                    transrel.Add(dist, mydeps.ToList());

                /* queue dependencies that haven't been visited */
                newdists.AddRange(mydeps.Except(transrel.Keys));
            }

            if (newdists.Count > 0)
                DependencyGraph.DeepFind(newdists.ToArray(), transrel);
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
