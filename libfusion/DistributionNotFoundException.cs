using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Exception for when a given distribution is not found.
    /// </summary>
    public sealed class DistributionNotFoundException : Exception
    {
        public DistributionNotFoundException(string package, Version version)
            : base("No package found matching the name '" + package + "-" + version.ToString() + "'.")
        { }
    }
}
