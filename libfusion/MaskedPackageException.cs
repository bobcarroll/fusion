using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// General exception for masked packages.
    /// </summary>
    public sealed class MaskedPackageException : Exception
    {
        private string _package;

        public MaskedPackageException(string package)
            : base("Encountered a masked package while performing the requested operation.")
        {
            _package = package;
        }

        /// <summary>
        /// The name of the masked package.
        /// </summary>
        public string Package
        {
            get { return _package; }
        }
    }
}
