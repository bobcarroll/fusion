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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fusion.Framework
{
    /// <summary>
    /// Represents the package version number part of an atom.
    /// </summary>
    public sealed class PackageVersion : IComparable<PackageVersion>
    {
        /// <summary>
        /// Regular expression for matching the version number part of the version string.
        /// </summary>
        public const string VERSION_NUMBER_FMT = "[0-9]+(?:[.][0-9]+)*[a-z]?";

        /// <summary>
        /// Regular expression for matching the suffix part of the version string.
        /// </summary>
        public const string VERSION_SUFFIX_FMT = "(?:_(?:alpha|beta|pre|rc|p)(?:[1-9]|[1-9][0-9]+)?)?";

        /// <summary>
        /// Regular expression for matching the revision part of the version string.
        /// </summary>
        public const string VERSION_REVISION_FMT = "(?:[-]r(?:[1-9]|[1-9][0-9]+))?";

        /// <summary>
        /// Regular expression for matching the distribution version string.
        /// </summary>
        public const string VERSION_FMT = VERSION_NUMBER_FMT + VERSION_SUFFIX_FMT + VERSION_REVISION_FMT;

        private string _version;
        private string _suffix;
        private int _revision;

        /// <summary>
        /// Initialises the package version.
        /// </summary>
        /// <param name="version">version number</param>
        /// <param name="suffix">optional suffix</param>
        /// <param name="revision">optional revision number</param>
        private PackageVersion(string version, string suffix, string revision)
        {
            _version = version;
            _suffix = suffix.TrimStart('_');
            _revision = !String.IsNullOrEmpty(revision) ?
                Int32.Parse(revision.TrimStart('-').TrimStart('r')) :
                0;
        }

        /// <summary>
        /// Compares the version numbers of two package versions.
        /// </summary>
        /// <param name="l">left side</param>
        /// <param name="r">right side</param>
        /// <returns>0 if equal, -1 if l less than r, or 1 is l greater than r</returns>
        private static int CompareVersion(PackageVersion l, PackageVersion r)
        {
            string[] lparts = l._version.Split('.');
            string[] rparts = r._version.Split('.');
            int len = lparts.Length > rparts.Length ?
                lparts.Length :
                rparts.Length;

            for (int i = 0; i < len; i++) {
                string lstr = (i < lparts.Length) ? lparts[i] : "0";
                string rstr = (i < rparts.Length) ? rparts[i] : "0";

                int lnum = Char.IsNumber(lstr, lstr.Length - 1) ?
                    Int32.Parse(lstr) :
                    Int32.Parse(lstr.Substring(0, lstr.Length - 1));
                char lchar = lstr.Length > 1 && !Char.IsNumber(lstr, lstr.Length - 1) ?
                    lstr[lstr.Length - 1] :
                    '\0';
                int rnum = Char.IsNumber(rstr, rstr.Length - 1) ?
                    Int32.Parse(rstr) :
                    Int32.Parse(rstr.Substring(0, rstr.Length - 1));
                char rchar = rstr.Length > 1 && !Char.IsNumber(rstr, rstr.Length - 1) ?
                    rstr[rstr.Length - 1] :
                    '\0';

                if (lnum < rnum)
                    return -1;
                else if (lnum > rnum)
                    return 1;
                else if (lchar < rchar)
                    return -1;
                else if (lchar > rchar)
                    return 1;
            }

            return 0;
        }

        /// <summary>
        /// Compares the suffix of two package versions.
        /// </summary>
        /// <param name="l">left side</param>
        /// <param name="r">right side</param>
        /// <returns>0 if equal, -1 if l less than r, or 1 is l greater than r</returns>
        private static int CompareSuffix(PackageVersion l, PackageVersion r)
        {
            Dictionary<string, uint> sufmap = new Dictionary<string, uint>() {
                { "alpha", 1 },
                { "beta", 2 },
                { "pre", 3 },
                { "rc", 4 },
                { "", 5 },
                { "p", 6 }
            };

            string pattern = "^([a-z]+)([0-9]*)$";
            Match ml = Regex.Match(l._suffix, pattern);
            Match mr = Regex.Match(r._suffix, pattern);

            if (!sufmap.ContainsKey(ml.Groups[1].Value) || !sufmap.ContainsKey(mr.Groups[1].Value))
                throw new FormatException("Package version suffix format is invalid.");

            uint lsuf = sufmap[ml.Groups[1].Value];
            uint rsuf = sufmap[mr.Groups[1].Value];

            if (lsuf < rsuf)
                return -1;
            else if (lsuf > rsuf)
                return 1;

            int lnum = 0;
            int rnum = 0;

            Int32.TryParse(ml.Groups[2].Value, out lnum);
            Int32.TryParse(mr.Groups[2].Value, out rnum);

            if (lnum < rnum)
                return -1;
            else if (lnum > rnum)
                return 1;

            return 0;
        }

        /// <summary>
        /// Compares another package version to this one.
        /// </summary>
        /// <param name="r">right side</param>
        /// <returns>0 if equal, -1 if this less than r, or 1 is this greater than r</returns>
        public int CompareTo(PackageVersion r)
        {
            if (this < r)
                return -1;
            else if (this > r)
                return 1;

            return 0;
        }

        /// <summary>
        /// Determines if the given package version equals this (by value).
        /// </summary>
        /// <param name="obj">the other package version</param>
        /// <returns>true if equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is PackageVersion))
                return false;

            PackageVersion l = this;
            PackageVersion r = (PackageVersion)obj;

            return PackageVersion.CompareVersion(l, r) == 0 &&
                PackageVersion.CompareSuffix(l, r) == 0 &&
                l._revision == r._revision;
        }

        /// <summary>
        /// Returns the hash code for this package version.
        /// </summary>
        /// <returns>a 32-bit signed integer hash code</returns>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// Parses the given string as a package version or throws an exception.
        /// </summary>
        /// <param name="input">input string to parse</param>
        /// <returns>output package version result</returns>
        public static PackageVersion Parse(string input)
        {
            PackageVersion pv = null;

            if (!PackageVersion.TryParse(input, out pv))
                throw new FormatException("Bad package version '" + input + "'.");

            return pv;
        }

        /// <summary>
        /// Returns a string that represents the package version.
        /// </summary>
        /// <returns>package version string</returns>
        public override string ToString()
        {
            string s = !String.IsNullOrEmpty(_suffix) ? "_" + _suffix : "";
            string r = _revision > 0 ? "-r" + _revision : "";

            return _version + s + r;
        }

        /// <summary>
        /// Attempts to parse the given string as a package version.
        /// </summary>
        /// <param name="input">input string to parse</param>
        /// <param name="result">output package version result</param>
        /// <returns>true on success, false otherwise</returns>
        public static bool TryParse(string input, out PackageVersion result)
        {
            result = null;

            if (input == null)
                return false;

            string pattern = String.Format(
                "({0})({1})({2})",
                VERSION_NUMBER_FMT,
                VERSION_SUFFIX_FMT,
                VERSION_REVISION_FMT);
            Match m = Regex.Match(input, "^" + pattern + "$");
            if (m.Success)
                result = new PackageVersion(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value);

            return (result != null);
        }

        /// <summary>
        /// Tests for binary equality by checking for null values and calling PackageVersion.Equals().
        /// </summary>
        /// <param name="l">left side</param>
        /// <param name="r">right side</param>
        /// <returns>true if equals, false otherwise</returns>
        public static bool operator ==(PackageVersion l, PackageVersion r)
        {
            if (Object.ReferenceEquals(l, r))
                return true;

            if (Object.ReferenceEquals(l, null) || Object.ReferenceEquals(r, null))
                return false;

            return l.Equals(r);
        }

        /// <summary>
        /// Tests for binary inequality by checking for null values and returning the inverse
        /// of PackageVersion.Equals().
        /// </summary>
        /// <param name="l">left side</param>
        /// <param name="r">right side</param>
        /// <returns></returns>
        public static bool operator !=(PackageVersion l, PackageVersion r)
        {
            return !(l == r);
        }

        /// <summary>
        /// Tests if l is less than r.
        /// </summary>
        /// <param name="l">left side</param>
        /// <param name="r">right side</param>
        /// <returns>true if l less than r, false otherwise</returns>
        public static bool operator <(PackageVersion l, PackageVersion r)
        {
            int result;

            if ((result = PackageVersion.CompareVersion(l, r)) < 0)
                return true;
            else if (result > 0)
                return false;

            if ((result = PackageVersion.CompareSuffix(l, r)) < 0)
                return true;
            else if (result > 0)
                return false;

            return l._revision < r._revision;
        }

        /// <summary>
        /// Tests if l is less than or equal to r.
        /// </summary>
        /// <param name="l">left side</param>
        /// <param name="r">right side</param>
        /// <returns>true if l less than or equal to r, false otherwise</returns>
        public static bool operator <=(PackageVersion l, PackageVersion r)
        {
            int result;

            if ((result = PackageVersion.CompareVersion(l, r)) < 0)
                return true;
            else if (result > 0)
                return false;

            if ((result = PackageVersion.CompareSuffix(l, r)) < 0)
                return true;
            else if (result > 0)
                return false;

            return l._revision <= r._revision;
        }

        /// <summary>
        /// Tests if l is greater than r.
        /// </summary>
        /// <param name="l">left side</param>
        /// <param name="r">right side</param>
        /// <returns>true if l greater than r, false otherwise</returns>
        public static bool operator >(PackageVersion l, PackageVersion r)
        {
            int result;

            if ((result = PackageVersion.CompareVersion(l, r)) > 0)
                return true;
            else if (result < 0)
                return false;

            if ((result = PackageVersion.CompareSuffix(l, r)) > 0)
                return true;
            else if (result < 0)
                return false;

            return l._revision > r._revision;
        }

        /// <summary>
        /// Tests if l is greater than or equal to r.
        /// </summary>
        /// <param name="l">left side</param>
        /// <param name="r">right side</param>
        /// <returns>true if l greater than or equal to r, false otherwise</returns>
        public static bool operator >=(PackageVersion l, PackageVersion r)
        {
            int result;

            if ((result = PackageVersion.CompareVersion(l, r)) > 0)
                return true;
            else if (result < 0)
                return false;

            if ((result = PackageVersion.CompareSuffix(l, r)) > 0)
                return true;
            else if (result < 0)
                return false;

            return l._revision >= r._revision;
        }
    }
}
