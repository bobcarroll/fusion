using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Exception for when a package atom matches more than one package.
    /// </summary>
    public sealed class AmbiguousMatchException : Exception
    {
        private string _atom;

        public AmbiguousMatchException(string atom)
            : base("Ambigious short name given. Please specifiy the full package name.")
        {
            _atom = atom;
        }

        /// <summary>
        /// The ambiguous package atom.
        /// </summary>
        public string Atom
        {
            get { return _atom; }
        }
    }
}
