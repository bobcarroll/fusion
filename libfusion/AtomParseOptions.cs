using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Options for parsing atom strings.
    /// </summary>
    public enum AtomParseOptions : int
    {
        /// <summary>
        /// Parse only the package name, and reject atom strings that have a version.
        /// </summary>
        WithoutVersion = 0,

        /// <summary>
        /// Accept atom strings with or without a version.
        /// </summary>
        VersionOptional = 1,

        /// <summary>
        /// Accepts atom string only if a version is given.
        /// </summary>
        VersionRequired = 2
    }
}
