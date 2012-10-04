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
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Fusion.Framework
{
    /// <summary>
    /// A descriptor for matching package versions.
    /// </summary>
    public sealed class Atom
    {
        private string _oper;
        private string _pkg;
        private Version _ver;
        private uint _slot;
        private uint _revision;

        /// <summary>
        /// Regular expression for matching the category name.
        /// </summary>
        public const string CATEGORY_NAME_FMT = "[a-z0-9]+(?:-[a-z0-9]+)?";

        /// <summary>
        /// Regular expression for matching the package name.
        /// </summary>
        public const string PACKAGE_NAME_FMT = "[a-z0-9\\+]+(?:[_-][a-z0-9\\+]+)*";

        /// <summary>
        /// Regular expression for matching the distribution version string.
        /// </summary>
        public const string VERSION_FMT = "[0-9]+(?:[.][0-9]+){1,3}";

        /// <summary>
        /// Regular expression for matching the comparison operator.
        /// </summary>
        public const string CMP_OPERATOR_FMT = "=|(?:<|>|!)=?";

        /// <summary>
        /// Regular expression for matching the slot number.
        /// </summary>
        public const string SLOT_FMT = "(?:[1-9]|[1-9][0-9]+)";

        /// <summary>
        /// Regular expression for matching the revision number.
        /// </summary>
        public const string REVISION_FMT = "r(?:[1-9]|[1-9][0-9]+)";

        /// <summary>
        /// Initialises an atom from a package name.
        /// </summary>
        /// <param name="pkg">the package name</param>
        private Atom(string pkg)
            : this(null, pkg, null, 0, 0) { }

        /// <summary>
        /// Initialises an atom.
        /// </summary>
        /// <param name="oper">the comparison operator</param>
        /// <param name="pkg">the package name</param>
        /// <param name="ver">the package version</param>
        /// <param name="rev">the revision number</param>
        /// <param name="slot">the slot number</param>
        private Atom(string oper, string pkg, string ver, uint rev, uint slot)
        {
            Debug.Assert(
                (oper == null && ver == null) ||
                (oper != null && ver != null));

            _oper = oper;
            _pkg = pkg;
            _ver = null;
            _revision = rev;
            _slot = slot;

            Version.TryParse(ver, out _ver);
        }

        /// <summary>
        /// Compares two versions with revision numbers.
        /// </summary>
        /// <param name="leftver">version on the left side</param>
        /// <param name="leftrev">revision number on the left side</param>
        /// <param name="rightver">version on the right side</param>
        /// <param name="rightrev">revision number on the right side</param>
        /// <returns>zero if equal, less than zero if right is greater, greater than zero if left is greater</returns>
        public static int CompareVersions(Version leftver, uint leftrev, Version rightver, uint rightrev)
        {
            int vercmp = leftver.CompareTo(rightver);

            if (vercmp != 0)
                return vercmp;

            leftrev = Atom.GetRevisionSort(leftrev);
            rightrev = Atom.GetRevisionSort(rightrev);

            return leftrev.CompareTo(rightrev);
        }

        /// <summary>
        /// Makes a string representation of the given revision.
        /// </summary>
        /// <param name="rev">revision number</param>
        /// <returns>formatted revision string</returns>
        public static string FormatRevision(uint rev)
        {
            return (rev > 0) ? String.Format("r{0}", rev) : "";
        }

        /// <summary>
        /// Makes a string representation of the given revision and version.
        /// </summary>
        /// <param name="rev">revision number</param>
        /// <param name="ver">version</param>
        /// <returns>formatted revision string with version</returns>
        public static string FormatRevision(uint rev, Version ver)
        {
            return (rev > 0) ?
                String.Format("{0}-{1}", ver.ToString(), Atom.FormatRevision(rev)) :
                ver.ToString();
        }

        /// <summary>
        /// Gets the revision order value from the given revision number.
        /// </summary>
        /// <param name="rev">revision number</param>
        /// <returns>a revision number used for sorting</returns>
        public static uint GetRevisionSort(uint rev)
        {
            return (uint)(rev - 1);
        }

        /// <summary>
        /// Makes an atom string from the given parameters.
        /// </summary>
        /// <param name="fullname">full package name (in the form of category/package)</param>
        /// <param name="version">version string</param>
        /// <param name="rev">revision number</param>
        /// <param name="slot">slot number</param>
        /// <returns>the full package name</returns>
        public static string MakeAtomString(string fullname, string version, uint rev, uint slot)
        {
            string result;

            if (rev > 0)
                result = String.Format("{0}-{1}-{2}", fullname, version, Atom.FormatRevision(rev));
            else
                result = String.Format("{0}-{1}", fullname, version);

            if (slot > 0)
                result = String.Format("{0}:{1}", result, slot);

            return result;
        }

        /// <summary>
        /// Makes a full package name from the given category and package strings.
        /// </summary>
        /// <param name="category">category name</param>
        /// <param name="package">package name</param>
        /// <returns>the full package name</returns>
        public static string MakeFullName(string category, string package)
        {
            return String.Format("{0}/{1}", category, package);
        }

        /// <summary>
        /// Determines if the given atom matches the constraints of this atom.
        /// </summary>
        /// <param name="left">the atom to compare to</param>
        /// <returns>true on match, false otherwise</returns>
        /// <remarks>
        /// Evaluation uses the input as the left side of the equation and this
        /// instance as the right side.
        /// 
        /// Example:
        ///     "this" is >=foo/bar-123
        ///     "left" is foo/bar-124
        /// 
        ///     foo/bar-124 >= foo/bar-123
        ///    
        /// In this case the match will succeed. If "this" doesn't have a
        /// comparison operator set, then the package names are compared
        /// for equality and version is ignored. The comparison operator for
        /// "left" is always ignored.
        /// 
        /// If either atom doesn't have a version then version matching is
        /// skipped. Slots are never compared.
        /// </remarks>
        public bool Match(Atom left)
        {
            if (this.IsFullName && left.IsFullName && left.PackageName != _pkg)
                return false;
            else if (left.PackagePart != this.PackagePart)
                return false;

            if (!left.HasVersion || _oper == null || _ver == null)
                return true;
            else if (_oper == "=" && Atom.CompareVersions(left.Version, left.Revision, _ver, _revision) == 0)
                return true;
            else if (_oper == "<" && Atom.CompareVersions(left.Version, left.Revision, _ver, _revision) < 0)
                return true;
            else if (_oper == "<=" && Atom.CompareVersions(left.Version, left.Revision, _ver, _revision) <= 0)
                return true;
            else if (_oper == ">" && Atom.CompareVersions(left.Version, left.Revision, _ver, _revision) > 0)
                return true;
            else if (_oper == ">=" && Atom.CompareVersions(left.Version, left.Revision, _ver, _revision) >= 0)
                return true;
            else if (_oper == "!=" && Atom.CompareVersions(left.Version, left.Revision, _ver, _revision) != 0)
                return true;

            return false;
        }

        /// <summary>
        /// Converts a string into a package atom.
        /// </summary>
        /// <param name="atom">the string to parse</param>
        /// <returns>an atom</returns>
        public static Atom Parse(string atom)
        {
            return Atom.Parse(atom, AtomParseOptions.VersionOptional);
        }

        /// <summary>
        /// Converts a string into a package atom.
        /// </summary>
        /// <param name="atom">the string to parse</param>
        /// <param name="opts">parsing options</param>
        /// <returns>an atom</returns>
        public static Atom Parse(string atom, AtomParseOptions opts)
        {
            Match m;
            uint slot = 0, rev = 0;
            string atomstr;

            /* full package atom with implicit equals: =(category/package)-(version)-(revision):(slot) */
            atomstr = String.Format(
                "^(?:=?)({0}/{1})-({2})(?:-({3}))?(?:[:]({4}))?$",
                CATEGORY_NAME_FMT,
                PACKAGE_NAME_FMT,
                VERSION_FMT,
                REVISION_FMT,
                SLOT_FMT);
            m = Regex.Match(atom.ToLower(), atomstr);
            if (opts != AtomParseOptions.WithoutVersion && m.Success) {
                UInt32.TryParse(m.Groups[3].Value.TrimStart('r'), out rev);
                UInt32.TryParse(m.Groups[4].Value, out slot);
                return new Atom("=", m.Groups[1].Value, m.Groups[2].Value, rev, slot);
            }

            /* package short name with version and implicit equals: =(package)-(version)-(revision):(slot) */
            atomstr = String.Format(
                "^(?:=?)({0})-({1})(?:-({2}))?(?:[:]({3}))?$",
                PACKAGE_NAME_FMT,
                VERSION_FMT,
                REVISION_FMT,
                SLOT_FMT);
            m = Regex.Match(atom.ToLower(), atomstr);
            if (opts != AtomParseOptions.WithoutVersion && m.Success) {
                UInt32.TryParse(m.Groups[3].Value.TrimStart('r'), out rev);
                UInt32.TryParse(m.Groups[4].Value, out slot);
                return new Atom("=", m.Groups[1].Value, m.Groups[2].Value, rev, slot);
            }

            /* full package atom: (operator)(category/package)-(version)-(revision):(slot) */
            atomstr = String.Format(
                "^({0})({1}/{2})-({3})(?:-({4}))?(?:[:]({5}))?$",
                CMP_OPERATOR_FMT,
                CATEGORY_NAME_FMT,
                PACKAGE_NAME_FMT,
                VERSION_FMT,
                REVISION_FMT,
                SLOT_FMT);
            m = Regex.Match(atom.ToLower(), atomstr);
            if ((opts == AtomParseOptions.VersionOptional || opts == AtomParseOptions.VersionRequired) && m.Success) {
                UInt32.TryParse(m.Groups[4].Value.TrimStart('r'), out rev);
                UInt32.TryParse(m.Groups[5].Value, out slot);
                return new Atom(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, rev, slot);
            }

            /* fully-qualified package name: (category/package) */
            m = Regex.Match(atom.ToLower(), "^(" + CATEGORY_NAME_FMT + "/" + PACKAGE_NAME_FMT + ")$");
            if ((opts == AtomParseOptions.WithoutVersion || opts == AtomParseOptions.VersionOptional) && m.Success)
                return new Atom(m.Groups[1].Value);

            /* package short name: (package) */
            m = Regex.Match(atom.ToLower(), "^(" + PACKAGE_NAME_FMT + ")$");
            if ((opts == AtomParseOptions.WithoutVersion || opts == AtomParseOptions.VersionOptional) && m.Success)
                return new Atom(m.Groups[1].Value);

            throw new BadAtomException(atom);
        }

        /// <summary>
        /// Converts the given strings into package atoms.
        /// </summary>
        /// <param name="atoms">an array of strings to parse</param>
        /// <returns>an array of atoms</returns>
        public static Atom[] ParseAll(string[] atoms)
        {
            return Atom.ParseAll(atoms, AtomParseOptions.VersionOptional);
        }

        /// <summary>
        /// Converts the given strings into package atoms.
        /// </summary>
        /// <param name="atoms">an array of strings to parse</param>
        /// <param name="opts">parsing options</param>
        /// <returns>an array of atoms</returns>
        public static Atom[] ParseAll(string[] atoms, AtomParseOptions opts)
        {
            List<Atom> results = new List<Atom>();

            foreach (string a in atoms)
                results.Add(Atom.Parse(a, opts));

            return results.ToArray();
        }

        /// <summary>
        /// Gets the string representation of this atom.
        /// </summary>
        /// <returns>an atom string</returns>
        public override string ToString()
        {
            string oper = _oper != "=" ? _oper : "";
            string slot = _slot > 0 ? ":" + _slot.ToString() : "";

            return _ver != null ? 
                oper + _pkg + "-" + Atom.FormatRevision(_revision, _ver) + slot: 
                _pkg;
        }

        /// <summary>
        /// The category name part of this atom.
        /// </summary>
        public string CategoryPart
        {
            get
            {
                return this.IsFullName ?
                    _pkg.Substring(0, _pkg.IndexOf('/')) :
                    null;
            }
        }

        /// <summary>
        /// The comparison operator specified in this atom.
        /// </summary>
        public string Comparison
        {
            get { return _oper; }
        }

        /// <summary>
        /// Determines if the atom has a version.
        /// </summary>
        /// <returns>true if it has a version, false otherwise</returns>
        public bool HasVersion
        {
            get { return _ver != null; }
        }

        /// <summary>
        /// Flag indicates whether the package name is a fully-qualified 
        /// name or a short name.
        /// </summary>
        public bool IsFullName
        {
            get { return _pkg.Contains('/'); }
        }

        /// <summary>
        /// The package name specified in this atom.
        /// </summary>
        public string PackageName
        {
            get { return _pkg; }
        }

        /// <summary>
        /// The package name part of this atom.
        /// </summary>
        public string PackagePart
        {
            get
            {
                return this.IsFullName ?
                    _pkg.Substring(_pkg.IndexOf('/') + 1) :
                    _pkg;
            }
        }

        /// <summary>
        /// The revision number specified in this atom.
        /// </summary>
        public uint Revision
        {
            get { return _revision; }
        }

        /// <summary>
        /// The slot number specified in this atom.
        /// </summary>
        public uint Slot
        {
            get { return _slot; }
        }

        /// <summary>
        /// The package version specified in this atom.
        /// </summary>
        public Version Version
        {
            get { return _ver; }
        }
    }
}
