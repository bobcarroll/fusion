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
using System.Net;
using System.Text;
using System.Threading;

using Fusion.Framework;

namespace fuse
{
    /// <summary>
    /// Merges packages into the system.
    /// </summary>
    class MergeAction : IAction
    {
        private List<string> _atomlst;
        private Options _options;
        private List<string> _repolst;
        private int _numpkgs;
        private long _dloadsz;

        /// <summary>
        /// Initialises the new merge action.
        /// </summary>
        public MergeAction()
        {
            _atomlst = new List<string>();
            _repolst = new List<string>();
        }

        /// <summary>
        /// Executes this action.
        /// </summary>
        /// <param name="pkgmgr">package manager instance</param>
        public void Execute(IPackageManager pkgmgr)
        {
            MergeWorker mw = new MergeWorker(pkgmgr);

            AbstractTree tree = LocalRepository.Read();
            List<IDistribution> mergeset = new List<IDistribution>();
            List<Atom> atomset = new List<Atom>();

            try {
                /* expand world */
                if (_atomlst.Contains("world")) {
                    atomset.AddRange(pkgmgr.WorldSet);
                    _atomlst.Remove("world");
                }

                atomset.AddRange(Atom.ParseAll(_atomlst.ToArray()));

                /* slot number not allowed here */
                Atom slotatom = atomset.Where(i => i.Slot > 0).FirstOrDefault();
                if (slotatom != null)
                    throw new BadAtomException(slotatom.ToString());

                foreach (Atom atom in atomset) {
                    /* first find all installed packages matching the given atom */
                    Atom[] instarr = pkgmgr.FindPackages(atom).ToArray();

                    /* then find unique installed packages, and slotted packages with the
                       highest version number */
                    Atom[] latestinst = instarr
                        .Where(i => instarr.Where(n => n.PackageName == i.PackageName).Count() == 1 ||
                                    instarr.Where(n => n.PackageName == i.PackageName)
                                           .Max(n => n.Version.ToString()) == i.Version.ToString())
                        .ToArray();

                    /* if we're not updating, no version was specified, and there's a version
                     * already installed then select the installed version */
                    IDistribution[] distarr =
                        (!_options.update && !atom.HasVersion && latestinst.Length > 0) ?
                            tree.LookupAll(latestinst) :
                            tree.LookupAll(new Atom[] { atom });

                    mergeset.AddRange(distarr);
                }
            } catch (AmbiguousMatchException ex) {
                SearchAction sa = new SearchAction(ex.Atom);
                sa.Options = new Options() { exact = true };
                sa.Execute(pkgmgr);

                throw ex;
            }

            MergeOptions mopts = 0;

            if (_options.pretend)
                mopts |= MergeOptions.Pretend;
            if (_options.oneshot)
                mopts |= MergeOptions.OneShot;
            if (_options.noreplace || _options.update)
                mopts |= MergeOptions.NoReplace;
            if (_options.emptytree)
                mopts |= MergeOptions.EmptyTree;
            if (_options.fetchonly)
                mopts |= MergeOptions.FetchOnly;

            try {
                if (mopts.HasFlag(MergeOptions.Pretend)) {
                    mw.OnPretendMerge += this.MergeWorker_OnPretendMerge;
                    Console.WriteLine("\nThese are the packages that would be merged, in order:\n");
                } else {
                    mw.OnRealMerge += this.MergeWorker_OnRealMerge;
                    mw.OnParallelFetch += this.MergeWorker_OnParallelFetch;
                    mw.OnInstall += this.MergeWorker_OnInstall;
                    mw.OnAutoClean += this.MergeWorker_OnAutoClean;

                    Security.DemandNTAdmin();
                }

                mw.Merge(mergeset.ToArray(), mopts);

                if (mopts.HasFlag(MergeOptions.Pretend)) {
                    if (_repolst.Count > 0) {
                        Console.WriteLine("\nRepositories:");
                        for (int i = 0; i < _repolst.Count; i++) {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write(" [{0}] ", i);
                            Console.ResetColor();
                            Console.Write("{0}\n", _repolst[i]);
                        }

                        Console.Write("\n");
                    }

                    StringBuilder sb = new StringBuilder(11);
                    Win32.StrFormatByteSize(_dloadsz, sb, sb.Capacity);
                    Console.WriteLine(
                        "Total: {0} package(s), Size of download(s): {1}",
                        _numpkgs,
                        sb.ToString());
                } else
                    Console.Write("\n");
            } catch (MaskedPackageException ex) {
                Program.error_msg("\n!!! All ports that could satisfy '{0}' have been masked.", ex.Package);
            } catch (SlotConflictException ex) {
                Dictionary<string, List<IDistribution>> distdict = new Dictionary<string, List<IDistribution>>();

                foreach (string pkg in ex.Conflicts.Select(c => c.Package.FullName + ":" + c.Slot).Distinct())
                    distdict.Add(pkg, new List<IDistribution>());

                foreach (IDistribution dist in ex.Conflicts)
                    distdict[dist.Package.FullName + ":" + dist.Slot].Add(dist);

                Console.Write("\n");

                foreach (KeyValuePair<string, List<IDistribution>> kvp in distdict) {
                    Console.WriteLine(kvp.Key);

                    foreach (IDistribution dist in kvp.Value)
                        Console.WriteLine("  {0}", dist.ToString());

                    Console.Write("\n");
                }

                throw ex;
            }
        }
        
        /// <summary>
        /// Handler for the MergeWorker.OnParallelFetch event.
        /// </summary>
        /// <param name="sender">the merge worker</param>
        /// <param name="e">event args</param>
        private void MergeWorker_OnParallelFetch(object sender, EventArgs e)
        {
            Console.WriteLine("\n>>> Starting parallel fetch");
        }

        /// <summary>
        /// Handler for the MergeWorker.OnRealMerge event.
        /// </summary>
        /// <param name="sender">the merge worker</param>
        /// <param name="e">merge event args</param>
        public void MergeWorker_OnRealMerge(object sender, MergeEventArgs e)
        {
            if (e.FetchOnly)
                Console.Write("\n>>> Fetching (");
            else
                Console.Write("\n>>> Merging (");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(e.CurrentIter);

            Console.ResetColor();
            Console.Write(" of ");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(e.TotalMerges);

            Console.ResetColor();
            Console.Write(") ");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(e.Distribution.Atom.ToString());

            Console.ResetColor();
            Console.Write("\n");
        }

        /// <summary>
        /// Handler for the MergeWorker.OnPretendMerge event.
        /// </summary>
        /// <param name="sender">the merge worker</param>
        /// <param name="e">merge event args</param>
        public void MergeWorker_OnPretendMerge(object sender, MergeEventArgs e)
        {
            _numpkgs++;

            foreach (SourceFile s in e.Distribution.Sources)
                _dloadsz += s.Size;

            string distrepo = e.Distribution.PortsTree.Repository;
            if (!_repolst.Contains(distrepo))
                _repolst.Add(distrepo);

            Console.Write("[");
            if (e.Selected)
                Console.ForegroundColor = ConsoleColor.Green;
            else
                Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write("port   ");
            Console.ResetColor();

            if (e.Flags.HasFlag(MergeFlags.BlockUnresolved) || e.Flags.HasFlag(MergeFlags.BlockResolved)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("{0}", e.Flags.HasFlag(MergeFlags.BlockUnresolved) ? "B" : "b");
                Console.ResetColor();
            } else if (e.Flags.HasFlag(MergeFlags.Interactive)) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("I");
                Console.ResetColor();
            } else
                Console.Write(" ");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("{0}", e.Flags.HasFlag(MergeFlags.New) ? "N" : " ");
            Console.ResetColor();

            if (e.Flags.HasFlag(MergeFlags.Replacing)) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("R");
                Console.ResetColor();
            } else if (e.Flags.HasFlag(MergeFlags.Slot)) {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("S");
                Console.ResetColor();
            } else
                Console.Write(" ");

            if (e.Flags.HasFlag(MergeFlags.FetchNeeded) || e.Flags.HasFlag(MergeFlags.FetchExists)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("{0}", e.Flags.HasFlag(MergeFlags.FetchNeeded) ? "F" : "f");
                Console.ResetColor();
            } else
                Console.Write(" ");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("{0}", e.Flags.HasFlag(MergeFlags.Updating) ? "U" : " ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("{0}", e.Flags.HasFlag(MergeFlags.Downgrading) ? "D" : " ");
            Console.ResetColor();

            Console.Write("]");
            if (e.Selected)
                Console.ForegroundColor = ConsoleColor.Green;
            else
                Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(" {0}", e.Distribution.ToString());
            Console.ResetColor();

            if (!e.Flags.HasFlag(MergeFlags.New) && !e.Flags.HasFlag(MergeFlags.Replacing)) {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(" [{0}]", e.Previous.Version.ToString());
                Console.ResetColor();
            }

            StringBuilder sb = new StringBuilder(11);
            Win32.StrFormatByteSize(e.Distribution.TotalSize, sb, sb.Capacity);
            Console.Write(" {0}", sb.ToString());

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" [{0}]", _repolst.IndexOf(distrepo));
            Console.ResetColor();

            Console.Write("\n");
        }

        /// <summary>
        /// Handler for the MergeWorker.OnInstall event.
        /// </summary>
        /// <param name="sender">the merge worker</param>
        /// <param name="e">merge event args</param>
        public void MergeWorker_OnInstall(object sender, MergeEventArgs e)
        {
            Console.Write("\n>>> Installing ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(e.Distribution.Atom.ToString());

            Console.ResetColor();
            Console.Write(" into live file system\n");
        }

        /// <summary>
        /// Handler for the MergeWorker.OnAutoClean event.
        /// </summary>
        /// <param name="sender">the merge worker</param>
        /// <param name="e">event args</param>
        public void MergeWorker_OnAutoClean(object sender, EventArgs e)
        {
            Console.Write("\n>>> Auto-cleaning packages...");
        }

        /// <summary>
        /// A list of package atoms to merge.
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
