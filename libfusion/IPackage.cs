using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// A single package in a ports collection.
    /// </summary>
    public interface IPackage
    {
        /// <summary>
        /// A reference to the parent category.
        /// </summary>
        ICategory Category { get; }

        /// <summary>
        /// A brief description of this package.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// All distributions of this package.
        /// </summary>
        ReadOnlyCollection<IDistribution> Distributions { get; }

        /// <summary>
        /// The full name of this package.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// The homepage or project website of this package.
        /// </summary>
        string Homepage { get; }

        /// <summary>
        /// The latest available distribution of this package.
        /// </summary>
        IDistribution LatestAvailable { get; }

        /// <summary>
        /// The latest unmasked distribution of this package.
        /// </summary>
        IDistribution LatestUnmasked { get; }

        /// <summary>
        /// The software license for this package.
        /// </summary>
        string License { get; }

        /// <summary>
        /// The name of this package.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// A reference to the parent ports tree.
        /// </summary>
        AbstractTree PortsTree { get; }
    }
}
