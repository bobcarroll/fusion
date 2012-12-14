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
using System.IO;
using System.Xml;

using Microsoft.Build.Construction;

using log4net;

namespace Fusion.Framework
{
    /// <summary>
    /// A file-based versioned distribution of a package.
    /// </summary>
    public sealed class Distribution : IDistribution
    {
        private static ILog _log = LogManager.GetLogger(typeof(Distribution));

        /// <summary>
        /// Regular expression for matching inclusive keywords.
        /// </summary>
        public const string KEYWORD_INCL_FMT = "[~]?[A-Za-z0-9-_]+";

        /// <summary>
        /// Regular expression for matching exclusive keywords.
        /// </summary>
        public const string KEYWORD_EXCL_FMT = "[-]?[A-Za-z0-9-_]+";

        private FileInfo _pkgdist;
        private Package _package;
        private PackageVersion _version;
        private int _apirev = 1;  /* default to the lowest revision */
        private string[] _keywords = new string[] { };
        private bool _fetch = false;
        private XmlElement _project = null;
        private SourceFile[] _sources = new SourceFile[] { };
        private List<Atom> _depends = new List<Atom>();
        private long _totalsz = 0;
        private uint _slot = 0;
        private bool _interactive = false;
        private bool _srcdist = false;
        private Atom _myatom;

        /// <summary>
        /// Initialises the package distribution.
        /// </summary>
        /// <param name="dist">the distribution file</param>
        /// <param name="pkg">the package this distro belongs to</param>
        private Distribution(FileInfo dist, Package pkg)
        {
            _pkgdist = dist;
            _package = pkg;
            _version = Distribution.ParseVersion(dist.Name, pkg.Name);

            XmlDocument doc = new XmlDocument();
            doc.Load(dist.FullName);

            XmlElement root = (XmlElement)doc.SelectSingleNode("//Port");
            if (root == null)
                throw new InvalidDataException("Port definition file is malformed.");

            if (root.Attributes["api"] != null)
                _apirev = Convert.ToInt32(root.Attributes["api"].Value);

            XmlNodeList nl = root.SelectNodes("Keywords/Keyword");
            List<string> keywords = new List<string>();
            foreach (XmlElement kwelem in nl) {
                if (Regex.IsMatch(kwelem.InnerText, KEYWORD_INCL_FMT) ||
                    Regex.IsMatch(kwelem.InnerText, KEYWORD_EXCL_FMT))
                     keywords.Add(kwelem.InnerText);
            }
            _keywords = keywords.ToArray();

            XmlNodeList srcnl = root.SelectNodes("Sources/File");
            List<SourceFile> sources = new List<SourceFile>();
            foreach (XmlElement e in srcnl) {
                SourceFile srcfile;
                string srctmp;

                string srcdigest = e.GetAttribute("digest");

                srctmp = e.GetAttribute("size");
                long archsz = !String.IsNullOrEmpty(srctmp) ? Convert.ToInt64(srctmp) : 0;

                srctmp = e.GetAttribute("uri");
                Uri srcuri = !String.IsNullOrEmpty(srctmp) ? new Uri(srctmp) : null;

                string pkgname = e.GetAttribute("pkgname");
                if (String.IsNullOrEmpty(pkgname)) {
                    string ext = Path.GetExtension(srcuri.LocalPath);
                    pkgname = Atom.FormatPackageVersion(_package.Name, _version) + ext;
                }

                srctmp = e.GetAttribute("cpuarch");
                CpuArchitecture arch = 0;
                Enum.TryParse<CpuArchitecture>(srctmp, true, out arch);

                if (srcuri != null)
                    srcfile = new WebSourceFile(srcuri, srcdigest, pkgname, archsz, arch);
                else
                    srcfile = new SourceFile(srcdigest, pkgname, archsz, arch);

                sources.Add(srcfile);
            }
            _sources = sources.ToArray();

            XmlElement elem = (XmlElement)root.SelectSingleNode("Restrict/Fetch");
            _fetch = (elem != null) ? Convert.ToBoolean(elem.InnerText) : false;

            elem = (XmlElement)root.SelectSingleNode("Properties/Interactive");
            _interactive = (elem != null) ? Convert.ToBoolean(elem.InnerText) : false;

            elem = (XmlElement)root.SelectSingleNode("Properties/Source");
            _srcdist = (elem != null) ? Convert.ToBoolean(elem.InnerText) : false;

            elem = (XmlElement)root.SelectSingleNode("Slot");
            if (elem != null)
                UInt32.TryParse(elem.InnerText, out _slot);

            XmlNodeList depends = root.SelectNodes("Dependencies/Package[@atom != '']");
            foreach (XmlElement e in depends)
                _depends.Add(Atom.Parse(e.Attributes["atom"].Value));

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(root.OwnerDocument.NameTable);
            nsmgr.AddNamespace("msbuild", MSBuildProject.MSBUILD_PROJECT_NS);
            _project = (XmlElement)root.SelectSingleNode("msbuild:Project", nsmgr);

            _myatom = Atom.Parse(
                Atom.MakeAtomString(_package.FullName, _version.ToString(), _slot), 
                AtomParseOptions.VersionRequired);

            foreach (SourceFile s in this.PlatformSources)
                _totalsz += s.Size;
        }

        /// <summary>
        /// Determines if the source files exist locally.
        /// </summary>
        /// <param name="dist">distribution to check</param>
        /// <param name="distdir">distributions directory</param>
        /// <returns>true if sources exist, false otherwise</returns>
        public static bool CheckSourcesExist(IDistribution dist, DirectoryInfo distdir)
        {
            bool result = true;

            foreach (SourceFile src in dist.PlatformSources) {
                FileInfo fi = new FileInfo(distdir + @"\" + src.LocalName);

                if (!fi.Exists)
                    result = false;
            }

            return result;
        }

        /// <summary>
        /// Scans the package directory for distribution port files.
        /// </summary>
        /// <param name="pkg">the associated package</param>
        /// <returns>a list of distributions of the given package</returns>
        internal static List<Distribution> Enumerate(Package pkg)
        {
            FileInfo[] fiarr = pkg.PackageDirectory
                .EnumerateFiles()
                .Where(p => Distribution.ValidateName(p.Name, pkg.Name))
                .ToArray();
            _log.DebugFormat(
                "{0} distributions: {1}",
                pkg.Name, String.Join(", ", fiarr.Select(i => i.Name).ToArray()));
            List<Distribution> results = new List<Distribution>();

            foreach (FileInfo fi in fiarr) {
                try {
                    results.Add(new Distribution(fi, pkg));
                } catch {
                    _log.WarnFormat("Skipping malformed port file '{0}'", fi.FullName);
                }
            }

            return results;
        }

        /// <summary>
        /// Creates a distribution installer project instance.
        /// </summary>
        /// <param name="sbox">sandbox directory</param>
        /// <returns>an install project instance, or NULL if no project is found</returns>
        public IInstallProject GetInstallProject(SandboxDirectory sbox)
        {
            if (_project == null)
                return null;

            string pkgname = Atom.FormatPackageVersion(_package.Name, _version);
            Configuration cfg = Configuration.LoadSeries();

            Func<string, string> addslash = delegate(string input) {
                return input.TrimEnd('\\') + @"\";
            };

            Dictionary<string, string> vars = new Dictionary<string, string>();
            vars.Add("P", pkgname);
            vars.Add("PN", _package.Name);
            vars.Add("PV", _version.ToString());
            vars.Add("CATEGORY", _package.Category.Name);
            vars.Add("ROOT", addslash(cfg.RootDir.FullName));
            vars.Add("DISTDIR", addslash(cfg.DistFilesDir.FullName));
            vars.Add("WORKDIR", addslash(sbox.WorkDir.FullName));
            vars.Add("DATADIR", addslash(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)));
            vars.Add("FILESDIR", addslash(this.FilesDirectory.FullName));
            vars.Add("T", addslash(sbox.TempDir.FullName));
            vars.Add("D", addslash(sbox.ImageDir.FullName));
            vars.Add("L", addslash(sbox.LinkDir.FullName));
            vars.Add("S", _slot.ToString());

            XmlReader xr = new XmlNodeReader(_project);
            string[] imports = MSBuildProject.GetImportedProjects(_project);
            return new MSBuildProject(pkgname, ProjectRootElement.Create(xr), vars, imports);
        }

        /// <summary>
        /// Gets the keyword name without any modifier symbols.
        /// </summary>
        /// <param name="keyword">keyword</param>
        /// <returns>keyword name</returns>
        public static string GetKeywordName(string keyword)
        {
            return keyword.TrimStart('~');
        }

        /// <summary>
        /// Parses the port file name for the distribution version.
        /// </summary>
        /// <param name="distname">the port file name</param>
        /// <param name="pkgname">the package name</param>
        /// <returns>the package version</returns>
        public static PackageVersion ParseVersion(string distname, string pkgname)
        {
            PackageVersion result = null;

            if (!Distribution.ValidateName(distname, pkgname))
                throw new FormatException("Failed to validate distribution name '" + distname + "'.");

            Match m = Regex.Match(distname, "-(" + PackageVersion.VERSION_FMT + ")", RegexOptions.RightToLeft);
            if (!PackageVersion.TryParse(m.Groups[1].Value, out result))
                throw new FormatException("Failed to parse package version from '" + distname + "'.");

            return result;
        }

        /// <summary>
        /// Determines if the given string is a properly-formatted package dist name.
        /// </summary>
        /// <param name="name">the string to test</param>
        /// <param name="pkgname">the package name</param>
        /// <returns>true when valid, false otherwise</returns>
        public static bool ValidateName(string name, string pkgname)
        {
            return Regex.IsMatch(
                name,
                "^" + Regex.Escape(pkgname) + "-" + PackageVersion.VERSION_FMT + "[.]xml$");
        }

        /// <summary>
        /// Gets a string representation of this distribution
        /// </summary>
        /// <returns>a package atom</returns>
        public override string ToString()
        {
            return Atom.FormatPackageVersion(_package.FullName, _version);
        }

        /// <summary>
        /// All package source files.
        /// </summary>
        public SourceFile[] AllSources
        {
            get { return _sources; }
        }

        /// <summary>
        /// Fusion API revision number.
        /// </summary>
        public int ApiRevision
        {
            get { return _apirev; }
        }

        /// <summary>
        /// The exact atom for this distribution.
        /// </summary>
        public Atom Atom
        {
            get { return _myatom; }
        }

        /// <summary>
        /// Packages this distribution depends on.
        /// </summary>
        public Atom[] Dependencies
        {
            get { return _depends.ToArray(); }
        }

        /// <summary>
        /// Fetch restriction imposes manual distfile download.
        /// </summary>
        public bool FetchRestriction
        {
            get { return _fetch; }
        }

        /// <summary>
        /// Full path to the extra files directory in the ports tree.
        /// </summary>
        public DirectoryInfo FilesDirectory
        {
            get { return new DirectoryInfo(_package.PackageDirectory.FullName + @"\files"); }
        }

        /// <summary>
        /// Flag indicating the installation requires user interaction.
        /// </summary>
        public bool Interactive
        {
            get { return _interactive; }
        }

        /// <summary>
        /// The arch keywords for this distribution.
        /// </summary>
        public string[] Keywords
        {
            get { return _keywords; }
        }

        /// <summary>
        /// A reference to the parent package.
        /// </summary>
        public IPackage Package
        {
            get { return _package; }
        }

        /// <summary>
        /// Package source files for the current platform.
        /// </summary>
        public SourceFile[] PlatformSources
        {
            get
            {
                return _sources
                    .Where(i => i.CpuArch == 0 || i.CpuArch == Configuration.RealCpuArch)
                    .ToArray();
            }
        }

        /// <summary>
        /// A reference to the parent ports tree.
        /// </summary>
        public AbstractTree PortsTree
        {
            get { return _package.PortsTree; }
        }

        /// <summary>
        /// Package installation slot number.
        /// </summary>
        public uint Slot
        {
            get { return _slot; }
        }

        /// <summary>
        /// Flag indicating if this distribution is source code.
        /// </summary>
        public bool SourceDistribution
        {
            get { return _srcdist; }
        }

        /// <summary>
        /// The total compressed size of platform source files.
        /// </summary>
        public long TotalSize
        {
            get { return _totalsz; }
        }

        /// <summary>
        /// The version of this distribution.
        /// </summary>
        public PackageVersion Version
        {
            get { return _version; }
        }
    }
}
