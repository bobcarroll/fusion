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
using System.Xml;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Fusion.Tasks.MSI
{
    /// <summary>
    /// Creates an install image from an MSI dump.
    /// </summary>
    public sealed class MakeImage : Task
    {
        /// <summary>
        /// Task execution handler.
        /// </summary>
        /// <returns>true on success, false on error</returns>
        public override bool Execute()
        {
            string xmlfile = Path.Combine(this.DumpDir, "wix.xml");
            if (!File.Exists(xmlfile)) {
                base.Log.LogError("Could not find wix.xml file. Did you execute Dump?");
                return false;
            }

            XmlDocument wixdoc = new XmlDocument();
            wixdoc.Load(xmlfile);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(wixdoc.NameTable);
            nsmgr.AddNamespace("wi", "http://schemas.microsoft.com/wix/2006/wi");

            XmlElement pfdir = (XmlElement)wixdoc.SelectSingleNode(
                "//wi:Wix//wi:Directory[@Id = '" + this.Target + "']", 
                nsmgr);
            if (pfdir != null)
                this.RecursiveOutput(pfdir, this.ImageDir);

            return true;
        }

        /// <summary>
        /// Recursively descends into the XML structure to create the output.
        /// </summary>
        /// <param name="direlem">current directory element to operate on</param>
        /// <param name="outdir">output directory</param>
        private void RecursiveOutput(XmlElement direlem, string outdir)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(direlem.OwnerDocument.NameTable);
            nsmgr.AddNamespace("wi", "http://schemas.microsoft.com/wix/2006/wi");

            XmlNodeList nl = direlem.SelectNodes("wi:Component/wi:File", nsmgr);
            foreach (XmlElement elem in nl) {
                string oldname = elem.GetAttribute("Source");
                string newname =  Path.Combine(outdir, elem.GetAttribute("Name"));
                File.Move(oldname, newname);
                base.Log.LogMessage("Moved file '{0}' to '{1}'", oldname, newname);
            }

            nl = direlem.SelectNodes("wi:Directory", nsmgr);
            foreach (XmlElement elem in nl) {
                string dirname = Path.Combine(outdir, elem.GetAttribute("Name"));
                Directory.CreateDirectory(dirname);
                base.Log.LogMessage("Created directory '{0}'", dirname);
                this.RecursiveOutput(elem, dirname);
            }
        }

        /// <summary>
        /// Target directory ID.
        /// </summary>
        [Required]
        public string Target { get; set; }

        /// <summary>
        /// Path where output files are written to.
        /// </summary>
        [Required]
        public string ImageDir { get; set; }

        /// <summary>
        /// Path where files were dumped from the MSI.
        /// </summary>
        [Required]
        public string DumpDir { get; set; }
    }
}
