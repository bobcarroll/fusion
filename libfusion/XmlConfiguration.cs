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
using System.Xml;
using System.IO;
using System.Reflection;

namespace Fusion.Framework
{
    /// <summary>
    /// A simple XML configuration loader.
    /// </summary>
    public sealed class XmlConfiguration
    {
        public const string CURRENTFILE = "current";
        public const string PROFILEDIR = "profiles";

        private static DirectoryInfo _bindir;
        private static XmlConfiguration _instance;

        private XmlConfiguration() { }

        /// <summary>
        /// Loads the Fusion configuration series.
        /// </summary>
        /// <returns>a configuration instance with loaded values</returns>
        public static XmlConfiguration LoadSeries()
        {
            return XmlConfiguration.LoadSeries(false);
        }

        /// <summary>
        /// Loads the Fusion configuration series.
        /// </summary>
        /// <param name="reload">flag to reload configuration</param>
        /// <returns>a configuration instance with loaded values</returns>
        public static XmlConfiguration LoadSeries(bool reload)
        {
            if (_instance != null && !reload)
                return _instance;

            _instance = new XmlConfiguration();
            bool isadmin = Security.IsNTAdmin();
            FileInfo[] fiarr;

            string progdata = 
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Fusion";

            _instance.ConfDir = new DirectoryInfo(progdata + @"\conf");
            _instance.DistFilesDir = new DirectoryInfo(progdata + @"\distfiles");
            _instance.PortDir = new DirectoryInfo(progdata + @"\global");
            _instance.LogDir = isadmin ?
                new DirectoryInfo(progdata + @"\logs") :
                new DirectoryInfo(Path.GetTempPath());

            /* set defaults for optional settings */
            _instance.AcceptKeywords = new string[] { };
            _instance.CollisionDetect = false;
            _instance.PortDirOverlays = new DirectoryInfo[] { };
            _instance.PortMirrors = new Uri[] { };

            /* Determine the current profile */
            DirectoryInfo profileroot = new DirectoryInfo(_instance.PortDir + @"\" + PROFILEDIR);
            if (!profileroot.Exists)
                profileroot.Create();
            fiarr = profileroot.GetFiles(CURRENTFILE);
            if (fiarr.Length == 0)
                throw new FileNotFoundException("No profile is selected.");
            _instance.CurrentProfile = File.ReadAllText(profileroot + @"\current").TrimEnd('\r', '\n', ' ');
            _instance.ProfileDir = new DirectoryInfo(profileroot + @"\" + _instance.CurrentProfile);
            if (String.IsNullOrWhiteSpace(_instance.CurrentProfile) || !_instance.ProfileDir.Exists)
                throw new DirectoryNotFoundException("No profile found for '" + _instance.CurrentProfile + "'.");

            /* load profile config */
            fiarr = _instance.ProfileDir.GetFiles("global.xml");
            if (fiarr.Length > 0)
                _instance.LoadSingle(fiarr[0]);

            /* load machine config */
            fiarr = _instance.ConfDir.GetFiles("local.xml");
            if (fiarr.Length > 0)
                _instance.LoadSingle(fiarr[0]);

            if (_instance.RootDir == null || !_instance.RootDir.Exists)
                throw new DirectoryNotFoundException("Root directory is invalid.");

            return _instance;
        }

        /// <summary>
        /// Read in configuration data from an XML file.
        /// </summary>
        /// <param name="file">path of the config file</param>
        public void LoadSingle(FileInfo cfgfile)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(cfgfile.FullName);
            XmlElement elem;

            XmlNodeList nl = doc.SelectNodes("//Configuration/AcceptKeywords/Keyword");
            List<string> blst = new List<string>(this.AcceptKeywords);
            foreach (XmlNode n in nl)
                blst.Add(n.InnerText);
            this.AcceptKeywords = blst.Distinct().ToArray();

            elem = (XmlElement)doc.SelectSingleNode("//Configuration/CollisionDetect");
            if (elem != null && !String.IsNullOrWhiteSpace(elem.InnerText))
                this.CollisionDetect = Convert.ToBoolean(elem.InnerText);

            elem = (XmlElement)doc.SelectSingleNode("//Configuration/SystemRoot");
            if (elem != null && !String.IsNullOrWhiteSpace(elem.InnerText))
                this.RootDir = new DirectoryInfo(elem.InnerText);

            nl = doc.SelectNodes("//Configuration/PortDirOverlay");
            List<DirectoryInfo> overlaylst = new List<DirectoryInfo>();
            foreach (XmlNode n in nl)
                overlaylst.Add(new DirectoryInfo(((XmlElement)n).InnerText));
            this.PortDirOverlays = overlaylst.ToArray();

            nl = doc.SelectNodes("//Configuration/PortMirror");
            List<Uri> mlst = new List<Uri>();
            foreach (XmlNode n in nl)
                mlst.Add(new Uri(((XmlElement)n).InnerText));
            this.PortMirrors = mlst.ToArray();
        }

        /// <summary>
        /// Arch keywords accepted for merge.
        /// </summary>
        public string[] AcceptKeywords { get; set; }

        /// <summary>
        /// Gets the Fusion binary directory.
        /// </summary>
        public static DirectoryInfo BinDir
        {
            get
            {
                if (_bindir == null) {
                    string asmpath =
                        Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
                    _bindir = new DirectoryInfo(Path.GetDirectoryName(asmpath));
                }

                return _bindir;
            }
        }

        /// <summary>
        /// Flag for collision detection during merge.
        /// </summary>
        public bool CollisionDetect { get; set; }

        /// <summary>
        /// The local configuration directory.
        /// </summary>
        public DirectoryInfo ConfDir { get; set; }

        /// <summary>
        /// The name of the selected profile.
        /// </summary>
        public string CurrentProfile { get; set; }

        /// <summary>
        /// The distfile cache directory.
        /// </summary>
        public DirectoryInfo DistFilesDir { get; set; }

        /// <summary>
        /// The log directory.
        /// </summary>
        public DirectoryInfo LogDir { get; set; }

        /// <summary>
        /// The local ports directory.
        /// </summary>
        public DirectoryInfo PortDir { get; set; }

        /// <summary>
        /// User-defined port overlay directories.
        /// </summary>
        public DirectoryInfo[] PortDirOverlays { get; set; }

        /// <summary>
        /// URIs of port mirrors.
        /// </summary>
        public Uri[] PortMirrors { get; set; }

        /// <summary>
        /// The selected profile directory.
        /// </summary>
        public DirectoryInfo ProfileDir { get; set; }

        /// <summary>
        /// Root directory where packages are installed.
        /// </summary>
        public DirectoryInfo RootDir { get; set; }
    }
}
