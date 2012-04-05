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
using System.Linq;
using System.Text;

using Fusion.Framework;

namespace fuse
{
    /// <summary>
    /// Removes packages from the specified zone.
    /// </summary>
    class UnmergeAction : IAction
    {
        private List<string> _atomlst;
        private Options _options;

        /// <summary>
        /// Initialises a new unmerge action.
        /// </summary>
        public UnmergeAction()
        {
            _atomlst = new List<string>();
        }

        /// <summary>
        /// Executes this action.
        /// </summary>
        /// <param name="pkgmgr">package manager instance</param>
        /// <param name="cfg">ports configuration</param>
        public void Execute(IPackageManager pkgmgr, XmlConfiguration cfg)
        {
            /* TODO */
            throw new NotImplementedException();
        }

        /// <summary>
        /// A list of package atoms to unmerge.
        /// </summary>
        public List<string> Atoms
        {
            get { return _atomlst; }
        }

        /// <summary>
        /// Command options structure.
        /// </summary>
        public Options Options
        {
            get { return _options; }
            set { _options = value; }
        }
    }
}
