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
using System.Runtime.Serialization;
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
    [Serializable]
    public sealed class MSBuildProject : IInstallProject
    {
        private const string PKG_POSTINST_TARGET = "pkg_postinst";
        private const string PKG_PREINST_TARGET = "pkg_preinst";
        private const string PKG_POSTRM_TARGET = "pkg_postrm";
        private const string PKG_PRERM_TARGET = "pkg_prerm";
        private const string SRC_COMPILE_TARGET = "src_compile";
        private const string SRC_INSTALL_TARGET = "src_install";
        private const string SRC_TEST_TARGET = "src_test";
        private const string SRC_UNPACK_TARGET = "src_unpack";

        private string _name;
        private Project _project;
        private ILog _log;
        private Dictionary<string, string> _globals;
        
        /// <summary>
        /// Reads an installer project from an MSBuild project root.
        /// </summary>
        /// <param name="pkg">package name</param>
        /// <param name="root">an MSBuild project root element</param>
        /// <param name="vars">dictionary of global installer variables</param>
        public MSBuildProject(string pkg, ProjectRootElement root, Dictionary<string, string> vars)
        {
            Dictionary<string, string> globals = new Dictionary<string, string>();
            globals.Add("BINDIR", Configuration.BinDir.FullName);
            globals.Add("BITNESS", Environment.Is64BitOperatingSystem ? "64" : "32");

            foreach (KeyValuePair<string, string> kvp in vars)
                globals.Add(kvp.Key, kvp.Value);

            _name = pkg;
            _project = new Project(root, globals, null);
            _log = LogManager.GetLogger(typeof(MSBuildProject));
            _globals = vars;
        }

        /// <summary>
        /// Constructs an instance from serialised data.
        /// </summary>
        /// <param name="info">the SerializationInfo to read data from</param>
        /// <param name="context">the source for this deserialization</param>
        private MSBuildProject(SerializationInfo info, StreamingContext context)
            : this((string)info.GetValue("name", typeof(string)), 
                   MSBuildProject.DeserialiseProject(info), 
                   MSBuildProject.DeserialiseGlobals(info)) { }

        /// <summary>
        /// Extract the MSBuild project XML from the serialised data.
        /// </summary>
        /// <param name="info">the SerializationInfo to read data from</param>
        /// <returns>MSBuild proejct root element</returns>
        private static ProjectRootElement DeserialiseProject(SerializationInfo info)
        {
            string projectxml = (string)info.GetValue("project", typeof(string));

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(projectxml);

            XmlReader xr = new XmlNodeReader(doc.DocumentElement);
            return ProjectRootElement.Create(xr);
        }

        /// <summary>
        /// Extract the installer variables from the serialised data.
        /// </summary>
        /// <param name="info">the SerializationInfo to read data from</param>
        /// <returns>dictionary of installer variables</returns>
        private static Dictionary<string, string> DeserialiseGlobals(SerializationInfo info)
        {
            Dictionary<string, string> vars =
                (Dictionary<string, string>)info.GetValue("globals", typeof(Dictionary<string, string>));

            /* wtf? we have to tell the dictionary to finish deserialising or it'll be empty */
            vars.OnDeserialization(new Object());

            return vars;
        }

        /// <summary>
        /// Executes the given target with the given installer instance.
        /// </summary>
        /// <param name="pi">installer project instance</param>
        /// <param name="target">target name</param>
        private void Execute(ProjectInstance pi, string target)
        {
            ILogger msb2l4n = new BuildLogRedirector(_log);
            if (!pi.Build(target, new ILogger[] { msb2l4n }))
                throw new InstallException("Target '" + target + "' execution failed.");
        }

        /// <summary>
        /// Populates a System.Runtime.Serialization.SerializationInfo with the data needed 
        /// to serialize the target object.
        /// </summary>
        /// <param name="info">the SerializationInfo to populate with data</param>
        /// <param name="context">the destination for this serialization</param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            XmlElement elem = this.ToXml();

            info.AddValue("name", _name);
            info.AddValue("project", elem.OwnerDocument.OuterXml);
            info.AddValue("globals", _globals, typeof(Dictionary<string, string>));
        }

        /// <summary>
        /// Called after image is installed to $(ROOT)
        /// </summary>
        public void PkgPostInst()
        {
            if (!this.HasPkgPostInstTarget)
                return;

            this.Execute(_project.CreateProjectInstance(), PKG_POSTINST_TARGET);
        }

        /// <summary>
        /// Called before image is installed to $(ROOT)
        /// </summary>
        public void PkgPreInst()
        {
            if (!this.HasPkgPreInstTarget)
                return;

            this.Execute(_project.CreateProjectInstance(), PKG_PREINST_TARGET);
        }

        /// <summary>
        /// Called after package is removed from $(ROOT)
        /// </summary>
        public void PkgPostRm()
        {
            if (!this.HasPkgPostRmTarget)
                return;

            this.Execute(_project.CreateProjectInstance(), PKG_POSTRM_TARGET);
        }

        /// <summary>
        /// Called before package is removed from $(ROOT)
        /// </summary>
        public void PkgPreRm()
        {
            if (!this.HasPkgPreRmTarget)
                return;

            this.Execute(_project.CreateProjectInstance(), PKG_PRERM_TARGET);
        }

        /// <summary>
        /// Configure and build the package.
        /// </summary>
        public void SrcCompile()
        {
            if (!this.HasSrcCompileTarget)
                return;

            this.Execute(_project.CreateProjectInstance(), SRC_COMPILE_TARGET);
        }

        /// <summary>
        /// Install a package to $(D)
        /// </summary>
        public void SrcInstall()
        {
            if (!this.HasSrcInstallTarget)
                return;

            this.Execute(_project.CreateProjectInstance(), SRC_INSTALL_TARGET);
        }

        /// <summary>
        /// Run pre-install test scripts
        /// </summary>
        public void SrcTest()
        {
            if (!this.HasSrcTestTarget)
                return;

            this.Execute(_project.CreateProjectInstance(), SRC_TEST_TARGET);
        }

        /// <summary>
        /// Extract source packages and do any necessary patching or fixes.
        /// </summary>
        public void SrcUnpack()
        {
            if (!this.HasSrcUnpackTarget)
                return;

            this.Execute(_project.CreateProjectInstance(), SRC_UNPACK_TARGET);
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

        /// <summary>
        /// Indicates whether or not the installer project has a package-post-install target.
        /// </summary>
        public bool HasPkgPostInstTarget
        {
            get { return _project.Targets.ContainsKey(PKG_POSTINST_TARGET); }
        }

        /// <summary>
        /// Indicates whether or not the installer project has a package-pre-install target.
        /// </summary>
        public bool HasPkgPreInstTarget
        {
            get { return _project.Targets.ContainsKey(PKG_PREINST_TARGET); }
        }

        /// <summary>
        /// Indicates whether or not the installer project has a package-post-remove target.
        /// </summary>
        public bool HasPkgPostRmTarget
        {
            get { return _project.Targets.ContainsKey(PKG_POSTRM_TARGET); }
        }

        /// <summary>
        /// Indicates whether or not the installer project has a package-pre-remove target.
        /// </summary>
        public bool HasPkgPreRmTarget
        {
            get { return _project.Targets.ContainsKey(PKG_PRERM_TARGET); }
        }

        /// <summary>
        /// Indicates whether or not the installer project has a source-compile target.
        /// </summary>
        public bool HasSrcCompileTarget
        {
            get { return _project.Targets.ContainsKey(SRC_COMPILE_TARGET); }
        }

        /// <summary>
        /// Indicates whether or not the installer project has a source-install target.
        /// </summary>
        public bool HasSrcInstallTarget
        {
            get { return _project.Targets.ContainsKey(SRC_INSTALL_TARGET); }
        }

        /// <summary>
        /// Indicates whether or not the installer project has a source-test target.
        /// </summary>
        public bool HasSrcTestTarget
        {
            get { return _project.Targets.ContainsKey(SRC_TEST_TARGET); }
        }

        /// <summary>
        /// Indicates whether or not the installer project has a source-unpack target.
        /// </summary>
        public bool HasSrcUnpackTarget
        {
            get { return _project.Targets.ContainsKey(SRC_UNPACK_TARGET); }
        }

        /// <summary>
        /// Name of the package being installed.
        /// </summary>
        public string PackageName
        {
            get { return _name; }
        }
    }
}
