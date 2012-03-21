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
using System.Reflection;
using System.Text;
using System.Xml;

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

using log4net;

namespace Fusion.Framework
{
    /// <summary>
    /// A distribution installer project in XML format.
    /// </summary>
    public sealed class MSBuildProject : IInstallProject
    {
        private Project _project;
        private ILog _log;
        
        /// <summary>
        /// Reads an installer project from an XML stream.
        /// </summary>
        /// <param name="root">an MSBuild project root element</param>
        public MSBuildProject(ProjectRootElement root)
        {
            Dictionary<string, string> globals = new Dictionary<string, string>();
            globals.Add("BINDIR", MSBuildProject.GetBinDir().FullName);

            _project = new Project(root, globals, null);
            _log = LogManager.GetLogger(typeof(MSBuildProject));
        }

        /// <summary>
        /// Gets the Fusion bin directory.
        /// </summary>
        /// <returns>the bin directory</returns>
        public static DirectoryInfo GetBinDir()
        {
            string asmpath =
                Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
            return new DirectoryInfo(Path.GetDirectoryName(asmpath));
        }

        /// <summary>
        /// Gets the installer project raw XML.
        /// </summary>
        /// <returns>installer project document root</returns>
        public XmlElement ToXml()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(_project.Xml.RawXml);

            return doc.DocumentElement;
        }
    }
}
