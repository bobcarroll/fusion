using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// A grouping of packages in a ports collection.
    /// </summary>
    public interface ICategory
    {
        /// <summary>
        /// The name of this category.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// All of the packages contained in this category.
        /// </summary>
        ReadOnlyCollection<IPackage> Packages { get; }

        /// <summary>
        /// A reference to the parent ports tree.
        /// </summary>
        AbstractTree PortsTree { get; }
    }
}
