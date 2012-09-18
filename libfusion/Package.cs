/**
 * Fusion - package management system for Windows
 * Copyright (c) 2010 Bob Carroll
 * 
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;

namespace Fusion.Framework
{
    /// <summary>
    /// A single package in a file-based ports collection.
    /// </summary>
    public sealed class Package : IPackage
    {
        private DirectoryInfo _pkgdir;
        private Category _category;
        private List<IDistribution> _dists;
        private string _description = String.Empty;
        private string _homepage = String.Empty;
        private string _license = String.Empty;

        /// <summary>
        /// Initialises the package.
        /// </summary>
        /// <param name="pkgdir">the package directory</param>
        /// <param name="cat">the category this package belongs to</param>
        private Package(DirectoryInfo pkgdir, Category cat)
        {
            _pkgdir = pkgdir;
            _category = cat;
            _dists = new List<IDistribution>(Distribution.Enumerate(this));

            /* load package info */
            FileInfo[] fiarr = pkgdir.GetFiles("metadata.xml");
            if (fiarr.Length == 1) {
                XmlDocument infoxml = new XmlDocument();
                infoxml.Load(fiarr[0].FullName);

                XmlElement elem = (XmlElement)infoxml.SelectSingleNode("//Package/Description");
                if (elem != null && !String.IsNullOrWhiteSpace(elem.InnerText))
                    _description = elem.InnerText;

                elem = (XmlElement)infoxml.SelectSingleNode("//Package/Homepage");
                if (elem != null && !String.IsNullOrWhiteSpace(elem.InnerText))
                    _homepage = elem.InnerText;

                elem = (XmlElement)infoxml.SelectSingleNode("//Package/License");
                if (elem != null && !String.IsNullOrWhiteSpace(elem.InnerText))
                    _license = elem.InnerText;
            }
        }

        /// <summary>
        /// Scans the category directory for packages.
        /// </summary>
        /// <param name="catdir">the associated category</param>
        /// <returns>a list of packages in the category directory</returns>
        internal static List<Package> Enumerate(Category cat)
        {
            DirectoryInfo[] diarr = cat.CategoryDirectory.EnumerateDirectories()
                .Where(d => d.Name.Length <= 30 && Package.ValidateName(d.Name)).ToArray();
            List<Package> results = new List<Package>();

            foreach (DirectoryInfo di in diarr)
                results.Add(new Package(di, cat));

            return results;
        }

        /// <summary>
        /// Determines if the given string is a properly-formatted package name.
        /// </summary>
        /// <param name="name">the string to test</param>
        /// <returns>true when valid, false otherwise</returns>
        public static bool ValidateName(string name)
        {
            return Regex.IsMatch(name, "^" + Atom.PACKAGE_NAME_FMT + "$");
        }

        /// <summary>
        /// A reference to the parent category.
        /// </summary>
        public ICategory Category
        {
            get { return _category; }
        }

        /// <summary>
        /// A brief description of this package.
        /// </summary>
        public string Description
        {
            get { return _description; }
        }

        /// <summary>
        /// All distributions of this package.
        /// </summary>
        public ReadOnlyCollection<IDistribution> Distributions
        {
            get { return new ReadOnlyCollection<IDistribution>(_dists); }
        }

        /// <summary>
        /// The full name of this package.
        /// </summary>
        public string FullName
        {
            get { return Atom.MakeFullName(_category.Name, _pkgdir.Name); }
        }

        /// <summary>
        /// The homepage or project website of this package.
        /// </summary>
        public string Homepage
        {
            get { return _homepage; }
        }

        /// <summary>
        /// The latest available distribution of this package.
        /// </summary>
        public IDistribution LatestAvailable
        {
            get { return _dists.OrderByDescending(i => i.Version).FirstOrDefault(); }
        }

        /// <summary>
        /// The latest unmasked distribution of this package.
        /// </summary>
        public IDistribution LatestUnmasked
        {
            get
            {
                return _dists.Where(i => !_category.PortsTree.IsMasked(i))
                    .OrderByDescending(i => i.Version).FirstOrDefault();
            }
        }

        /// <summary>
        /// The software license for this package.
        /// </summary>
        public string License
        {
            get { return _license; }
        }

        /// <summary>
        /// The name of this package.
        /// </summary>
        public string Name
        {
            get { return _pkgdir.Name; }
        }

        /// <summary>
        /// The package directory local path.
        /// </summary>
        public DirectoryInfo PackageDirectory
        {
            get { return _pkgdir; }
        }

        /// <summary>
        /// A reference to the parent ports tree.
        /// </summary>
        public AbstractTree PortsTree
        {
            get { return _category.PortsTree; }
        }
    }
}
