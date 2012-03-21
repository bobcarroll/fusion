using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Exception for when a circular reference is detected in a dependency graph.
    /// </summary>
    public sealed class CircularReferenceException : Exception
    {
        public CircularReferenceException(string node)
            : base("Circular reference detected at " + node)
        { }
    }
}
