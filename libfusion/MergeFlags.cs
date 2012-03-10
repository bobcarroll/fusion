using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Merge status flags.
    /// </summary>
    [Flags]
    public enum MergeFlags : int
    {
        /// <summary>
        /// New package (not yet installed).
        /// </summary>
        New = 1,

        /// <summary>
        /// New side-by-side installation.
        /// </summary>
        Slot = 2,

        /// <summary>
        /// Updating package to another version.
        /// </summary>
        Updating = 4,

        /// <summary>
        /// Downgrading (best version seems lower).
        /// </summary>
        Downgrading = 8,

        /// <summary>
        /// Replacing the same version of an installed package.
        /// </summary>
        Replacing = 16,

        /// <summary>
        /// Fetch restriction (package must be manually downloaded).
        /// </summary>
        FetchNeeded = 32,

        /// <summary>
        /// Fetch restriction (package is already downloaded).
        /// </summary>
        FetchExists = 64,

        /// <summary>
        /// Requires user input.
        /// </summary>
        Interactive = 128,

        /// <summary>
        /// Blocked by another package (unresolved conflict).
        /// </summary>
        BlockUnresolved = 256,

        /// <summary>
        /// Blocked by another package (automatically resolved conflict).
        /// </summary>
        BlockResolved = 512
    }
}
