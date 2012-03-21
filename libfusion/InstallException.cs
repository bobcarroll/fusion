using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// General exception for errors during install.
    /// </summary>
    public sealed class InstallException : Exception
    {
        public InstallException(string message)
            : base(message) { }
    }
}
