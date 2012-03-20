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

        /// <summary>
        /// Regular expression for matching the category name.
        /// </summary>
        public const string CATEGORY_NAME_FMT = "[a-z0-9]+(?:-[a-z0-9]+)?";

        /// <summary>
        /// Regular expression for matching the package name.
        /// </summary>
        public const string PACKAGE_NAME_FMT = "[a-z0-9]+(?:[_-][a-z0-9]+)*";

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
        public const string SLOT_FMT = ":[0-9]";

        /// <summary>
        /// Initialises an atom from a package name.
        /// </summary>
        /// <param name="pkg">the package name</param>
        private Atom(string pkg)
            : this(null, pkg, null, 0) { }

        /// <summary>
        /// Initialises an atom from a distribution.
        /// </summary>
        /// <param name="dist">an existing distribution</param>
        public Atom(IDistribution dist)
            : this("=", dist.Package.FullName, dist.Version.ToString(), dist.Slot) { }

        /// <summary>
        /// Initialises an atom.
        /// </summary>
        /// <param name="oper">the comparison operator</param>
        /// <param name="pkg">the package name</param>
        /// <param name="ver">the package version</param>
        /// <param name="slot">the slot number</param>
        private Atom(string oper, string pkg, string ver, uint slot)
        {
            Debug.Assert(
                (oper == null && ver == null) ||
                (oper != null && ver != null));

            _oper = oper;
            _pkg = pkg;
            _ver = ver != null ? new Version(ver) : null;
            _slot = slot;
        }

        /// <summary>
        /// Determines if the atom has a version.
        /// </summary>
        /// <returns>true if it has a version, false otherwise</returns>
        public bool HasVersion()
        {
            return _ver != null;
        }

        /// <summary>
        /// Makes an atom string from the given parameters.
        /// </summary>
        /// <param name="fullname">full package name (in the form of category/package)</param>
        /// <param name="version">version string</param>
        /// <param name="slot">slot number</param>
        /// <returns>the full package name</returns>
        public static string MakeAtomString(string fullname, string version, uint slot)
        {
            if (slot > 0)
                return String.Format("{0}-{1}:{3}", fullname, version, slot);
            else
                return String.Format("{0}-{1}", fullname, version);
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
        /// Determines if the given atom matches this atom.
        /// </summary>
        /// <param name="atom">the atom to compare to</param>
        /// <returns>true on match, false otherwise</returns>
        public bool Match(Atom atom)
        {
            return this.Match(atom, false);
        }

        /// <summary>
        /// Determines if the given atom matches this atom.
        /// </summary>
        /// <param name="atom">the atom to compare to</param>
        /// <param name="nover">flag to ignore versions when matching</param>
        /// <returns>true on match, false otherwise</returns>
        public bool Match(Atom atom, bool nover)
        {
            if (this.IsFullName && atom.IsFullName && atom.PackageName != _pkg)
                return false;
            else if (atom.PackagePart != this.PackagePart)
                return false;
            else if (!nover && atom.Slot != _slot)
                return false;

            if (nover || _oper == null || _ver == null)
                return true;
            else if (_oper == "=" && atom.Version.CompareTo(_ver) == 0)
                return true;
            else if (_oper == "<" && atom.Version.CompareTo(_ver) < 0)
                return true;
            else if (_oper == "<=" && atom.Version.CompareTo(_ver) <= 0)
                return true;
            else if (_oper == ">" && atom.Version.CompareTo(_ver) > 0)
                return true;
            else if (_oper == ">=" && atom.Version.CompareTo(_ver) >= 0)
                return true;
            else if (_oper == "!=" && atom.Version.CompareTo(_ver) != 0)
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
            uint slot = 0;

            /* full package atom with implicit equals: =(category/package)-(version):slot */
            m = Regex.Match(
                atom.ToLower(),
                "^(?:=?)(" + CATEGORY_NAME_FMT + "/" + PACKAGE_NAME_FMT + ")-(" + VERSION_FMT + ")(" + SLOT_FMT + ")?$");
            if (opts != AtomParseOptions.WithoutVersion && m.Success) {
                UInt32.TryParse(m.Groups[3].Value.TrimStart(':'), out slot);
                return new Atom("=", m.Groups[1].Value, m.Groups[2].Value, slot);
            }

            /* full package atom: (operator)(category/package)-(version) */
            m = Regex.Match(
                atom.ToLower(),
                "^(" + CMP_OPERATOR_FMT + ")(" + CATEGORY_NAME_FMT + "/" + PACKAGE_NAME_FMT + ")-(" + VERSION_FMT + ")(" + SLOT_FMT + ")?$");
            if ((opts == AtomParseOptions.VersionOptional || opts == AtomParseOptions.VersionRequired) && m.Success) {
                UInt32.TryParse(m.Groups[4].Value.TrimStart(':'), out slot);
                return new Atom(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, slot);
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
                oper + _pkg + "-" + _ver.ToString() + slot: 
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
