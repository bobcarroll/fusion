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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fusion.Framework
{
    /// <summary>
    /// A file-based collection of ports arranged by category.
    /// </summary>
    public sealed class LocalRepository : AbstractTree
    {
        private XmlConfiguration _xmlconf;
        private List<ICategory> _categories;
        private Atom[] _hdmasked;
        private Atom[] _unmasked;

        /// <summary>
        /// Initialises the ports tree from a PortDir structure.
        /// </summary>
        private LocalRepository()
        {
            _xmlconf = XmlConfiguration.LoadSeries();
            _categories = new List<ICategory>(Category.Enumerate(this));
            _hdmasked = this.GetMaskedPackages();
            _unmasked = this.GetUnmaskedPackages();
        }

        /// <summary>
        /// Reads atoms from the both the profile and local package.mask file.
        /// </summary>
        /// <returns>an array of package atoms</returns>
        public Atom[] GetMaskedPackages()
        {
            List<Atom> alst = new List<Atom>();
            string[] files = new string[] { 
                _xmlconf.ProfilesRootDir + @"\package.mask",
                _xmlconf.ConfDir + @"\package.mask" };

            foreach (string file in files) {
                FileInfo fi = new FileInfo(file);
                if (!fi.Exists) continue;

                string[] inarr = File.ReadAllLines(fi.FullName);

                foreach (string s in inarr) {
                    try {
                        if (s.StartsWith("#")) continue;
                        alst.Add(Atom.Parse(s, AtomParseOptions.VersionRequired));
                    } catch (BadAtomException) {
                        throw new BadAtomException("Bad package atom '" + s + "' in package.mask file.");
                    }
                }
            }

            return alst.ToArray();
        }

        /// <summary>
        /// Reads atoms and keywords from both the profile and local package.keywords file.
        /// </summary>
        /// <returns>a dictionary of package keywords</returns>
        public Dictionary<Atom, string[]> GetPackageKeywords()
        {
            Dictionary<Atom, string[]> dict = new Dictionary<Atom, string[]>();
            string[] files = new string[] { 
                _xmlconf.ProfilesRootDir + @"\package.keywords",
                _xmlconf.ConfDir + @"\package.keywords" };

            foreach (string file in files) {
                FileInfo fi = new FileInfo(file);
                if (!fi.Exists) continue;

                string[] inarr = File.ReadAllLines(fi.FullName);

                foreach (string s in inarr) {
                    try {
                        string[] parts = s.Split(
                            new char[] { ' ', '\t' },
                            StringSplitOptions.RemoveEmptyEntries);
                        if (s.StartsWith("#") || parts.Length < 2)
                            continue;

                        Atom a = Atom.Parse(parts[0], AtomParseOptions.WithoutVersion);
                        string[] kwarr = new string[parts.Length - 1];
                        Array.Copy(parts, 1, kwarr, 0, parts.Length - 1);

                        dict.Add(a, kwarr.Where(i => Regex.IsMatch(i, Distribution.KEYWORD_INCL_FMT)).ToArray());
                    } catch (BadAtomException) {
                        throw new BadAtomException("Bad package atom '" + s + "' in package.keywords file.");
                    }
                }
            }

            return dict;
        }

        /// <summary>
        /// Reads atoms from the package.unmask file.
        /// </summary>
        /// <returns>an array of package atoms</returns>
        public Atom[] GetUnmaskedPackages()
        {
            List<Atom> alst = new List<Atom>();

            FileInfo fi = new FileInfo(_xmlconf.ConfDir + @"\package.unmask");
            if (fi.Exists) {
                string[] inarr = File.ReadAllLines(fi.FullName);

                foreach (string s in inarr) {
                    try {
                        if (s.StartsWith("#")) continue;
                        alst.Add(Atom.Parse(s, AtomParseOptions.VersionRequired));
                    } catch (BadAtomException) {
                        throw new BadAtomException("Bad package atom '" + s + "' in package.unmask file.");
                    }
                }
            }

            return alst.ToArray();
        }

        /// <summary>
        /// Determines if the given distribution is masked, either by the profile 
        /// package.mask file or by keyword.
        /// </summary>
        /// <param name="dist">the distribution to check</param>
        /// <returns>true if masked, false otherwise</returns>
        public override bool IsMasked(IDistribution dist)
        {
            return this.IsHardMasked(dist) || this.IsKeywordMasked(dist);
        }

        /// <summary>
        /// Determines if the given distribution is masked by the profile package.mask file.
        /// </summary>
        /// <param name="dist">the distribution to check</param>
        /// <returns>true if masked, false otherwise</returns>
        public override bool IsHardMasked(IDistribution dist)
        {
            bool fmasked = false;

            /* look in hard-masked packages */
            foreach (Atom a in _hdmasked) {
                if (a.Match(dist.Atom)) {
                    fmasked = true;
                    break;
                }
            }

            /* look in unmasked packages */
            foreach (Atom a in _unmasked) {
                if (a.Match(dist.Atom)) {
                    fmasked = false;
                    break;
                }
            }

            return fmasked;
        }

        /// <summary>
        /// Determines if the given distribution is masked by keywords.
        /// </summary>
        /// <param name="dist">the distribution to check</param>
        /// <returns>true if masked, false otherwise</returns>
        public override bool IsKeywordMasked(IDistribution dist)
        {
            bool result = _xmlconf.AcceptKeywords.Intersect(dist.Keywords).Count() == 0;
            Dictionary<Atom, string[]> kwdict = this.GetPackageKeywords();

            foreach (KeyValuePair<Atom, string[]> kvp in kwdict) {
                if (kvp.Key.Match(dist.Atom) && kvp.Value.Intersect(dist.Keywords).Count() > 0) {
                    result = false;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Loads the given ports tree from disk.
        /// </summary>
        /// <returns>the ports tree root node</returns>
        public static LocalRepository Read()
        {
            return new LocalRepository();
        }

        /// <summary>
        /// All package categories in the tree.
        /// </summary>
        public override ReadOnlyCollection<ICategory> Categories
        {
            get { return new ReadOnlyCollection<ICategory>(_categories); }
        }

        /// <summary>
        /// This ports tree root directory.
        /// </summary>
        public DirectoryInfo PortsDirectory
        {
            get { return _xmlconf.PortDir; }
        }

        /// <summary>
        /// Ports repository identifier.
        /// </summary>
        public override string Repository
        {
            get { return this.PortsDirectory.FullName; }
        }
    }
}
