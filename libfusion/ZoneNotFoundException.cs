using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Exception for when a given zone is not found.
    /// </summary>
    public sealed class ZoneNotFoundException : Exception
    {
        public ZoneNotFoundException(string zone) : 
            base("Could not find zone '" + zone + "' in the database.")
        { }
    }
}
