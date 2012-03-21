using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Exception for when two packages are being merged into the same slot.
    /// </summary>
    public sealed class SlotConflictException : Exception
    {
        private IDistribution[] _conflicts;

        public SlotConflictException(IDistribution[] conflicts)
            : base("Multiple package instances within a single package slot " +
                    "have been pulled into the dependency graph, resulting " +
                    "in a slot conflict.")
        {
            _conflicts = conflicts;
        }

        /// <summary>
        /// Distributions with a slot conflict.
        /// </summary>
        public IDistribution[] Conflicts
        {
            get { return _conflicts; }
        }
    }
}
