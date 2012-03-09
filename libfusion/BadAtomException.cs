using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Exception for when a package atom is malformed.
    /// </summary>
    public sealed class BadAtomException : Exception
    {
        private string _atom;
        private string _set;

        public BadAtomException(string atom)
            : base("Bad package atom '" + atom + "'.")
        {
            _atom = atom;
            _set = String.Empty;
        }

        public BadAtomException(string atom, string set)
            : base("Bad package atom '" + atom + "' in set '" + set + "'.")
        {
            _atom = atom;
            _set = set;
        }

        /// <summary>
        /// The badly formatted atom.
        /// </summary>
        public string Atom
        {
            get { return _atom; }
        }

        /// <summary>
        /// The set the atom was found in.
        /// </summary>
        public string Set
        {
            get { return _set; }
        }
    }
}
