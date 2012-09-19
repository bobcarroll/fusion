﻿/**
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

        private XmlConfiguration() { }

        /// <summary>
        /// Loads the Fusion configuration series.
        /// </summary>
        /// <returns>a configuration instance with loaded values</returns>
        public static XmlConfiguration LoadSeries()
        {
            XmlConfiguration cfg = new XmlConfiguration();
            FileInfo[] fiarr;

            string progdata = 
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Fusion";

            cfg.ConfDir = new DirectoryInfo(progdata + @"\conf");
            if (!cfg.ConfDir.Exists)
                cfg.ConfDir.Create();

            cfg.PortDir = new DirectoryInfo(progdata + @"\global");
            if (!cfg.PortDir.Exists)
                cfg.PortDir.Create();

            cfg.LogDir = new DirectoryInfo(progdata + @"\logs");
            if (!cfg.LogDir.Exists)
                cfg.LogDir.Create();

            cfg.TmpDir = new DirectoryInfo(Path.GetTempPath() + @"\fusion");
            if (!cfg.TmpDir.Exists)
                cfg.TmpDir.Create();

            /* set defaults for optional settings */
            cfg.AcceptKeywords = new string[] { };
            cfg.CollisionDetect = false;
            cfg.PortDirOverlays = new DirectoryInfo[] { };
            cfg.PortMirrors = new Uri[] { };

            /* Determine the current profile */
            DirectoryInfo profileroot = new DirectoryInfo(cfg.PortDir + @"\" + PROFILEDIR);
            if (!profileroot.Exists)
                profileroot.Create();
            fiarr = profileroot.GetFiles(CURRENTFILE);
            if (fiarr.Length == 0)
                throw new FileNotFoundException("No profile is selected.");
            cfg.CurrentProfile = File.ReadAllText(profileroot + @"\current").TrimEnd('\r', '\n', ' ');
            cfg.ProfileDir = new DirectoryInfo(profileroot + @"\" + cfg.CurrentProfile);
            if (String.IsNullOrWhiteSpace(cfg.CurrentProfile) || !cfg.ProfileDir.Exists)
                throw new DirectoryNotFoundException("No profile found for '" + cfg.CurrentProfile + "'.");

            /* load profile config */
            fiarr = cfg.ProfileDir.GetFiles("global.xml");
            if (fiarr.Length > 0)
                cfg.LoadSingle(fiarr[0]);

            /* load machine config */
            fiarr = cfg.ConfDir.GetFiles("local.xml");
            if (fiarr.Length > 0)
                cfg.LoadSingle(fiarr[0]);

            if (cfg.RootDir == null || !cfg.RootDir.Exists)
                throw new DirectoryNotFoundException("Root directory is invalid.");

            return cfg;
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
        public DirectoryInfo DistFilesDir
        {
            get { return new DirectoryInfo(this.TmpDir + @"\distfiles"); }
        }
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

        /// <summary>
        /// The sandbox root directory.
        /// </summary>
        public DirectoryInfo TmpDir { get; set; }
    }
}
