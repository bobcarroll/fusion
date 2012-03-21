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
