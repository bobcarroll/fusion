using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Metadata for merge callbacks.
    /// </summary>
    public sealed class MergeEventArgs : EventArgs
    {
        /// <summary>
        /// The one-based index of the current merge.
        /// </summary>
        public int CurrentIter;

        /// <summary>
        /// The distribution being merged.
        /// </summary>
        public IDistribution Distribution;

        /// <summary>
        /// Unique identifier for retrieving fetch results.
        /// </summary>
        public Guid FetchHandle;

        /// <summary>
        /// Flag to indicate if the package is only being fetched.
        /// </summary>
        public bool FetchOnly;

        /// <summary>
        /// Merge status flags.
        /// </summary>
        public MergeFlags Flags;

        /// <summary>
        /// The previously installed version (if any).
        /// </summary>
        public Atom Previous;

        /// <summary>
        /// Package was explicitly selected for merging.
        /// </summary>
        public bool Selected;

        /// <summary>
        /// The total number of packages to merge.
        /// </summary>
        public int TotalMerges;
    }
}
