using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using log4net;

using Fusion.Framework.Model;

namespace Fusion.Framework
{
    /// <summary>
    /// Stores information about packages installed on a local system.
    /// </summary>
    public sealed class PackageDatabase : IPackageManager
    {
        private Entities _ent;
        private XmlConfiguration _cfg;
        private ILog _log;

        /// <summary>
        /// Event raised before merging of any packages begins.
        /// </summary>
        public event EventHandler<EventArgs> OnPreMerge;

        /// <summary>
        /// Event raised for each package during a pretend merge.
        /// </summary>
        public event EventHandler<MergeEventArgs> OnPretendMerge;

        /// <summary>
        /// Event raised after merging of all packages finishes.
        /// </summary>
        public event EventHandler<EventArgs> OnPostMerge;

        /// <summary>
        /// Initialises the package manager instance.
        /// </summary>
        /// <param name="ent">entity container</param>
        /// <param name="cfg">ports configuration</param>
        /// <param name="log">system logger</param>
        private PackageDatabase(Entities ent, XmlConfiguration cfg, ILog log)
        {
            _ent = ent;
            _cfg = cfg;
            _log = log;
        }

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        ~PackageDatabase()
        {
            this.Dispose();
        }

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        public void Dispose()
        {
            _ent.Dispose();
        }

        /// <summary>
        /// Find packages installed matching the given package atom.
        /// </summary>
        /// <param name="atom">the atom to search</param>
        /// <param name="zone">ID of the zone to search</param>
        /// <returns>an array of zone packages</returns>
        public Atom[] FindPackages(Atom atom, long zone)
        {
            return _ent.Packages
                .Where(i => i.ZoneID == zone)
                .AsEnumerable()
                .Select(i => Atom.MakeAtomString(i.FullName, i.Version, (uint)i.Slot))
                .Select(i => Atom.Parse(i, AtomParseOptions.VersionRequired))
                .Where(i => atom.Match(i))
                .ToArray();
        }

        /// <summary>
        /// Merges the given distributions into the given zone.
        /// </summary>
        /// <param name="distarr">the distributions to merge</param>
        /// <param name="zone">ID of zone to merge into</param>
        public void Merge(IDistribution[] distarr, long zone)
        {
            this.Merge(distarr, zone, 0);
        }

        /// <summary>
        /// Merges the given distributions into the given zone.
        /// </summary>
        /// <param name="distarr">the distributions to merge</param>
        /// <param name="zone">ID of zone to merge into</param>
        /// <param name="mopts">merge option flags</param>
        public void Merge(IDistribution[] distarr, long zone, MergeOptions mopts)
        {
            
        }

        /// <summary>
        /// Normalises the given zone name.
        /// </summary>
        /// <param name="zone">raw zone name</param>
        /// <returns>normalised zone name</returns>
        private string NormaliseZoneName(string zone)
        {
            return zone.ToLower();
        }

        /// <summary>
        /// Opens the local package database for read/write.
        /// </summary>
        /// <param name="connstr">database connection string</param>
        /// <param name="cfg">ports configuration</param>
        /// <param name="log">system logger</param>
        /// <returns>package manager instance</returns>
        public static IPackageManager Open(string connstr, XmlConfiguration cfg, ILog log)
        {
            return new PackageDatabase(new Entities(connstr), cfg, log);
        }

        /// <summary>
        /// Finds the installed version of the given package.
        /// </summary>
        /// <param name="atom">package atom without version</param>
        /// <param name="zone">selected zone ID</param>
        /// <returns>package version, or NULL if none is found</returns>
        public Version QueryInstalledVersion(Atom atom, long zone)
        {
            string result = _ent.Packages
                .Where(i => i.Zone.ID == zone && 
                            i.FullName == atom.PackageName && 
                            i.Slot == atom.Slot)
                .Select(i => i.Version)
                .SingleOrDefault();

            return result != null ? new Version(result) : null;
        }

        /// <summary>
        /// Resolves the ID for the given zone name.
        /// </summary>
        /// <param name="zone">zone name to lookup</param>
        /// <returns>zone ID</returns>
        public long QueryZoneID(string zone)
        {
            string zonenorm = this.NormaliseZoneName(zone);
            long result = _ent.Zones
                .Where(i => i.Name == zonenorm)
                .Select(i => i.ID)
                .SingleOrDefault();

            if (result == 0)
                throw new ZoneNotFoundException(zonenorm);

            return result;
        }

        /// <summary>
        /// Items in the world favourites set.
        /// </summary>
        public Atom[] WorldSet
        {
            get
            {
                return _ent.WorldSet
                    .AsEnumerable()
                    .Select(i => Atom.Parse(i.Atom, AtomParseOptions.WithoutVersion))
                    .ToArray();
            }
        }
    }
}
