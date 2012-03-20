using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net;

namespace Fusion.Framework
{
    /// <summary>
    /// A versioned distribution of a package.
    /// </summary>
    public interface IDistribution
    {
        /// <summary>
        /// Creates a distribution installer project instance.
        /// </summary>
        /// <param name="log">system logger</param>
        /// <returns>an install project instance</returns>
        IInstallProject GetInstallProject(ILog log);

        /// <summary>
        /// Fusion API revision number.
        /// </summary>
        int ApiRevision { get; }

        /// <summary>
        /// File size of the package archive.
        /// </summary>
        long ArchiveSize { get; }

        /// <summary>
        /// The exact atom for this distribution.
        /// </summary>
        Atom Atom { get; }

        /// <summary>
        /// Packages this distribution depends on.
        /// </summary>
        Atom[] Dependencies { get; }

        /// <summary>
        /// Fetch restriction imposes manual distfile download.
        /// </summary>
        bool FetchRestriction { get; }

        /// <summary>
        /// Flag indicating the installation requires user interaction.
        /// </summary>
        bool Interactive { get; }

        /// <summary>
        /// The arch keywords for this distribution.
        /// </summary>
        string[] Keywords { get; }

        /// <summary>
        /// A reference to the parent package.
        /// </summary>
        IPackage Package { get; }

        /// <summary>
        /// A reference to the parent ports tree.
        /// </summary>
        AbstractTree PortsTree { get; }

        /// <summary>
        /// Package installation slot number.
        /// </summary>
        uint Slot { get; }

        /// <summary>
        /// The md5 hash of the distribution's archive.
        /// </summary>
        string SourceDigest { get; }

        /// <summary>
        /// The URI where the package can be downloaded.
        /// </summary>
        Uri SourceUri { get; }

        /// <summary>
        /// The total uncompressed size of package files.
        /// </summary>
        long TotalSize { get; }

        /// <summary>
        /// The package version specified in this atom.
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Gets a string representation of this distribution
        /// </summary>
        /// <returns>a package atom</returns>
        string ToString();
    }
}
