using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Fusion.Framework
{
    /// <summary>
    /// A grouping of packages in a file-based ports collection.
    /// </summary>
    public sealed class Category : ICategory
    {
        private DirectoryInfo _catdir;
        private AbstractTree _tree;
        private List<IPackage> _packages;

        /// <summary>
        /// Initialises the category.
        /// </summary>
        /// <param name="catdir">the category directory</param>
        /// <param name="tree">the ports tree this category belongs to</param>
        private Category(DirectoryInfo catdir, AbstractTree tree)
        {
            _catdir = catdir;
            _tree = tree;
            _packages = new List<IPackage>(Package.Enumerate(this));
        }

        /// <summary>
        /// Scans the ports directory for categories.
        /// </summary>
        /// <param name="tree">the ports tree to scan</param>
        /// <returns>a list of categories found in the ports directory</returns>
        internal static List<Category> Enumerate(LocalRepository tree)
        {
            return Category.Enumerate(tree, tree.PortsDirectory);
        }

        /// <summary>
        /// Scans the ports directory for categories.
        /// </summary>
        /// <param name="tree">the ports tree to associate categories with</param>
        /// <param name="portdir">the ports directory to scan</param>
        /// <returns>a list of categories found in the ports directory</returns>
        private static List<Category> Enumerate(AbstractTree tree, DirectoryInfo portdir)
        {
            DirectoryInfo[] diarr = portdir.EnumerateDirectories()
                .Where(d => d.Name.Length <= 20 && Category.ValidateName(d.Name)).ToArray();
            List<Category> results = new List<Category>();

            foreach (DirectoryInfo di in diarr)
                results.Add(new Category(di, tree));

            return results;
        }

        /// <summary>
        /// Determines if the given string is a properly-formatted category name.
        /// </summary>
        /// <param name="name">the string to test</param>
        /// <returns>true when valid, false otherwise</returns>
        public static bool ValidateName(string name)
        {
            if (name.ToLower() == XmlConfiguration.PROFILEDIR.ToLower())
                return false;

            return Regex.IsMatch(name, "^" + Atom.CATEGORY_NAME_FMT + "$");
        }

        /// <summary>
        /// The category directory local path.
        /// </summary>
        public DirectoryInfo CategoryDirectory
        {
            get { return _catdir; }
        }

        /// <summary>
        /// The name of this category.
        /// </summary>
        public string Name
        {
            get { return _catdir.Name; }
        }

        /// <summary>
        /// All of the packages contained in this category.
        /// </summary>
        public ReadOnlyCollection<IPackage> Packages
        {
            get { return new ReadOnlyCollection<IPackage>(_packages); }
        }

        /// <summary>
        /// A reference to the parent ports tree.
        /// </summary>
        public AbstractTree PortsTree
        {
            get { return _tree; }
        }
    }
}
