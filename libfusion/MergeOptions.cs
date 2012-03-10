using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Options for merging packages.
    /// </summary>
    [Flags]
    public enum MergeOptions : int
    {
        /// <summary>
        /// Instead of performing the install, show what packages would be installed.
        /// </summary>
        Pretend = 1,

        /// <summary>
        /// Merge the package normally, but do not include the package in the world set.
        /// </summary>
        OneShot = 2,

        /// <summary>
        /// Do not re-merge packages that are already installed.
        /// </summary>
        NoReplace = 4,

        /// <summary>
        /// Reinstall the target atoms and their entire deep dependency tree, as though 
        /// no packages are currently installed.
        /// </summary>
        EmptyTree = 8,

        /// <summary>
        /// Instead of performing the install, download the packages that would be installed.
        /// </summary>
        FetchOnly = 16
    }
}
