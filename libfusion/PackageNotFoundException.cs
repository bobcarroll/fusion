using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Exception for when a given package is not found.
    /// </summary>
    public sealed class PackageNotFoundException : Exception
    {
        public PackageNotFoundException(string package)
            : base("No package found matching the name '" + package + "'.")
        { }
    }
}
