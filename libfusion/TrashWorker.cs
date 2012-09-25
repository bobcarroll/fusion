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
using System.IO;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Cleans up trash files not deleted on unmerge or update.
    /// </summary>
    public static class TrashWorker
    {
        /// <summary>
        /// Adds the given file to the trash for later clean-up.
        /// </summary>
        /// <param name="path">absolute path of file</param>
        /// <param name="pkgmgr">package manager reference</param>
        public static void AddFile(string path, IPackageManager pkgmgr)
        {
            string newpath = null;

            if (File.Exists(path)) {
                newpath = Path.GetDirectoryName(path) + @"\" + Guid.NewGuid().ToString();
                File.Move(path, newpath);
                File.SetAttributes(newpath, FileAttributes.Hidden);
            } else if (Directory.Exists(path)) {
                newpath = path;
            } else
                return;

            pkgmgr.TrashFile(newpath);
        }

        /// <summary>
        /// Attempts to delete files and directories in the trash.
        /// </summary>
        /// <param name="pkgmgr">package manager reference</param>
        public static void Purge(IPackageManager pkgmgr)
        {
            List<string> deleted = new List<string>();

            foreach (string p in pkgmgr.Trash.OrderBy(i => i)) {
                try {
                    if (File.Exists(p))
                        File.Delete(p);
                    else if (Directory.Exists(p) && pkgmgr.CheckPathOwned(p)) {
                        /* this can be removed from the trash */
                    } else if (Directory.Exists(p) && Directory.GetFiles(p).Length > 0)
                        continue;
                    else if (Directory.Exists(p))
                        Directory.Delete(p);
                    else {
                        /* orphaned file? */
                    }

                    deleted.Add(p);
                } catch {
                    continue;
                }
            }

            pkgmgr.DeleteTrashItems(deleted.ToArray());
        }
    }
}
