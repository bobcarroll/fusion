using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Manages packages installed on a local system.
    /// </summary>
    public interface IPackageManager : IDisposable
    {
        /// <summary>
        /// Event raised before merging of any packages begins.
        /// </summary>
        event EventHandler<EventArgs> OnPreMerge;

        /// <summary>
        /// Event raised for each package during a pretend merge.
        /// </summary>
        event EventHandler<MergeEventArgs> OnPretendMerge;

        /// <summary>
        /// Event raised after merging of all packages finishes.
        /// </summary>
        event EventHandler<EventArgs> OnPostMerge;

        /// <summary>
        /// Find packages installed matching the given package atom.
        /// </summary>
        /// <param name="atom">the atom to search</param>
        /// <param name="zone">ID of the zone to search</param>
        /// <returns>an array of zone packages</returns>
        Atom[] FindPackages(Atom atom, long zone);

        /// <summary>
        /// Merges the given distributions into the given zone.
        /// </summary>
        /// <param name="distarr">the distributions to merge</param>
        /// <param name="zone">ID of zone to merge into</param>
        void Merge(IDistribution[] distarr, long zone);

        /// <summary>
        /// Merges the given distributions into the given zone.
        /// </summary>
        /// <param name="distarr">the distributions to merge</param>
        /// <param name="zone">ID of zone to merge into</param>
        /// <param name="mopts">merge option flags</param>
        void Merge(IDistribution[] distarr, long zone, MergeOptions mopts);

        /// <summary>
        /// Finds the installed version of the given package.
        /// </summary>
        /// <param name="atom">package atom without version</param>
        /// <param name="zone">selected zone ID</param>
        /// <returns>package version, or NULL if none is found</returns>
        Version QueryInstalledVersion(Atom atom, long zone);

        /// <summary>
        /// Resolves the ID for the given zone name.
        /// </summary>
        /// <param name="zone">zone name to lookup</param>
        /// <returns>zone ID</returns>
        long QueryZoneID(string zone);

        /// <summary>
        /// Items in the world favourites set.
        /// </summary>
        Atom[] WorldSet { get; }
    }
}
