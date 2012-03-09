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
        private FileInfo _pkgdist;
        private Package _package;
        private Version _version;
        private int _apirev = 1;  /* default to the lowest revision */
        private string[] _keywords = new string[] { };
        private bool _fetch = false;
        private XmlElement _project = null;
        private string _srcdigest = null;
        private Uri _srcuri = null;
        private List<Atom> _depends = new List<Atom>();
        private long _archsz = 0;
        private long _totalsz = 0;
        private uint _slot = 0;
        private bool _interactive = false;

        /// <summary>
        /// MSBuild XML schema URI.
        /// </summary>
        public const string MSBUILD_PROJECT_NS = "http://schemas.microsoft.com/developer/msbuild/2003";

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
            foreach (XmlElement kwelem in nl)
                keywords.Add(kwelem.InnerText);
            _keywords = keywords.ToArray();

            XmlElement elem = (XmlElement)root.SelectSingleNode("Source/Uri");
            if (elem != null && !String.IsNullOrEmpty(elem.InnerText)) {
                _srcuri = new Uri(elem.InnerText);
                _srcdigest = elem.GetAttribute("digest");

                string archsz = elem.GetAttribute("size");
                if (!String.IsNullOrEmpty(archsz))
                    _archsz = Convert.ToInt64(archsz);

                string totalsz = ((XmlElement)elem.ParentNode).GetAttribute("size");
                if (!String.IsNullOrEmpty(totalsz))
                    _totalsz = Convert.ToInt64(totalsz);
            }

            elem = (XmlElement)root.SelectSingleNode("Restrict/Fetch");
            _fetch = (elem != null) ? Convert.ToBoolean(elem.InnerText) : false;

            elem = (XmlElement)root.SelectSingleNode("Interactive");
            _interactive = (elem != null) ? Convert.ToBoolean(elem.InnerText) : false;

            elem = (XmlElement)root.SelectSingleNode("Slot");
            if (elem != null)
                UInt32.TryParse(elem.InnerText, out _slot);

            XmlNodeList depends = root.SelectNodes("Dependencies/Package[@atom != '']");
            foreach (XmlElement e in depends)
                _depends.Add(Atom.Parse(e.Attributes["atom"].Value));

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(root.OwnerDocument.NameTable);
            nsmgr.AddNamespace("msbuild", MSBUILD_PROJECT_NS);
            _project = (XmlElement)root.SelectSingleNode("msbuild:Project", nsmgr);
        }

        /// <summary>
        /// Scans the package directory for distribution port files.
        /// </summary>
        /// <param name="pkg">the associated package</param>
        /// <returns>a list of distributions of the given package</returns>
        internal static List<Distribution> Enumerate(Package pkg)
        {
            FileInfo[] fiarr = pkg.PackageDirectory.EnumerateFiles()
                .Where(p => Distribution.ValidateName(p.Name, pkg.Name)).ToArray();
            List<Distribution> results = new List<Distribution>();

            foreach (FileInfo fi in fiarr) {
                try {
                    results.Add(new Distribution(fi, pkg));
                } catch { }
            }

            return results;
        }

        /// <summary>
        /// Creates a distribution installer project instance.
        /// </summary>
        /// <param name="log">system logger</param>
        /// <returns>an install project instance, or NULL if no project is found</returns>
        public IInstallProject GetInstallProject(ILog log)
        {
            if (_project == null)
                return null;

            XmlReader xr = new XmlNodeReader(_project);
            return new MSBuildProject(ProjectRootElement.Create(xr), log);
        }

        /// <summary>
        /// Gets the local path of the distribution archive.
        /// </summary>
        /// <param name="dist">the distribution to find</param>
        /// <param name="tmpdir">a temporary directory</param>
        /// <returns>the local path to the distpkg</returns>
        public static FileInfo GetLocalDist(IDistribution dist, DirectoryInfo tmpdir)
        {
            if (dist.SourceUri == null)
                throw new FileNotFoundException("Source URI is missing from the port.");

            string filename = Path.GetFileName(dist.SourceUri.AbsolutePath);
            FileInfo fi = new FileInfo(tmpdir.FullName + @"\" + filename);

            return fi;
        }

        /// <summary>
        /// Parses the port file name for the distribution version.
        /// </summary>
        /// <param name="distname">the port file name</param>
        /// <param name="pkgname">the package name</param>
        /// <returns>a version or NULL</returns>
        public static Version ParseVersion(string distname, string pkgname)
        {
            if (!Distribution.ValidateName(distname, pkgname))
                return null;

            Match m = Regex.Match(distname, "-(" + Atom.VERSION_FMT + ")");
            return new Version(m.Groups[1].Value);
        }

        /// <summary>
        /// Determines if the given string is a properly-formatted package dist name.
        /// </summary>
        /// <param name="name">the string to test</param>
        /// <param name="pkgname">the package name</param>
        /// <returns>true when valid, false otherwise</returns>
        public static bool ValidateName(string name, string pkgname)
        {
            return Regex.IsMatch(name, "^" + pkgname + "-" + Atom.VERSION_FMT + "[.]xml$");
        }

        /// <summary>
        /// Gets a string representation of this distribution
        /// </summary>
        /// <returns>a package atom</returns>
        public override string ToString()
        {
            return _package.FullName + "-" + _version.ToString();
        }

        /// <summary>
        /// Fusion API revision number.
        /// </summary>
        public int ApiRevision
        {
            get { return _apirev; }
        }

        /// <summary>
        /// File size of the package archive.
        /// </summary>
        public long ArchiveSize
        {
            get { return _archsz; }
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
        /// The md5 hash of the distribution's archive.
        /// </summary>
        public string SourceDigest
        {
            get { return _srcdigest; }
        }

        /// <summary>
        /// The URI where the package can be downloaded.
        /// </summary>
        public Uri SourceUri
        {
            get { return _srcuri; }
        }

        /// <summary>
        /// The total uncompressed size of package files.
        /// </summary>
        public long TotalSize
        {
            get { return _totalsz; }
        }

        /// <summary>
        /// The version of this distribution.
        /// </summary>
        public Version Version
        {
            get { return _version; }
        }
    }
}
