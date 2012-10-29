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
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Nini.Config;

namespace Fusion.Framework
{
    /// <summary>
    /// A simple configuration loader.
    /// </summary>
    public sealed class Configuration
    {
        private static DirectoryInfo _bindir;
        private static Configuration _instance;
        private static CpuArchitecture _cpuarch = 0;

        private Configuration() { }

        /// <summary>
        /// Loads the Fusion configuration series.
        /// </summary>
        /// <returns>a configuration instance with loaded values</returns>
        public static Configuration LoadSeries()
        {
            return Configuration.LoadSeries(false);
        }

        /// <summary>
        /// Loads the Fusion configuration series.
        /// </summary>
        /// <param name="reload">flag to reload configuration</param>
        /// <returns>a configuration instance with loaded values</returns>
        public static Configuration LoadSeries(bool reload)
        {
            if (_instance != null && !reload)
                return _instance;

            _instance = new Configuration();
            bool isadmin = Security.IsNTAdmin();

            string homedirenv = Environment.GetEnvironmentVariable("FUSION_HOME");
            string progdata = String.IsNullOrEmpty(homedirenv) ?
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Fusion" :
                homedirenv;

            _instance.ConfDir = new DirectoryInfo(progdata + @"\etc");
            _instance.DistFilesDir = new DirectoryInfo(progdata + @"\distfiles");
            _instance.PortDir = new DirectoryInfo(progdata + @"\global");
            _instance.LogDir = isadmin ?
                new DirectoryInfo(progdata + @"\logs") :
                new DirectoryInfo(Path.GetTempPath());
            _instance.ProfileDir = new DirectoryInfo(progdata + @"\profile");
            _instance.ProfilesRootDir = new DirectoryInfo(_instance.PortDir + @"\profiles");

            /* set defaults for optional settings */
            _instance.AcceptKeywords = new string[] { };
            _instance.CollisionDetect = false;
            _instance.PortDirOverlays = new DirectoryInfo[] { };
            _instance.RsyncMirrors = new Uri[] { };

            /* load the profile cascade tree */
            if (_instance.ProfileDir.Exists) {
                List<DirectoryInfo> profilelst = new List<DirectoryInfo>();
                Configuration.WalkProfileTree(_instance.ProfileDir, profilelst, true);
                _instance.ProfileTree = profilelst.ToArray();
            } else
                _instance.ProfileTree = new DirectoryInfo[] { };

            /* load profile config */
            foreach (DirectoryInfo di in _instance.ProfileTree) {
                FileInfo profcfg = new FileInfo(di.FullName + @"\config.ini");
                if (profcfg.Exists)
                    _instance.LoadSingle(profcfg);
            }

            /* load machine config */
            FileInfo localcfg = new FileInfo(_instance.ConfDir.FullName + @"\config.ini");
            if (localcfg.Exists)
                _instance.LoadSingle(localcfg);

            if (_instance.RootDir == null || !_instance.RootDir.Exists) {
                string winroot = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
                _instance.RootDir = new DirectoryInfo(winroot);
            }

            return _instance;
        }

        /// <summary>
        /// Read in configuration data from an INI file.
        /// </summary>
        /// <param name="file">path of the config file</param>
        public void LoadSingle(FileInfo cfgfile)
        {
            IConfigSource source = new IniConfigSource(cfgfile.FullName);
            IConfig cc = source.Configs["Fusion"];

            string acceptkw = cc.Get("AcceptKeywords", "");
            List<string> kwlist = new List<string>();
            foreach (string kw in acceptkw.Split(' ').Where(i => !String.IsNullOrWhiteSpace(i))) {
                if (Regex.IsMatch(kw, Distribution.KEYWORD_INCL_FMT))
                    kwlist.Add(kw);
            }
            this.AcceptKeywords = this.AcceptKeywords
                .Union(kwlist)
                .Distinct()
                .ToArray();

            /* yes, I know there's a GetBoolean(), but this is a tri-state...
               true to set true, false to set false, and empty to do nothing */
            string coldetstr = cc.Get("CollisionDetect", "");
            bool coldetbool = false;
            if (Boolean.TryParse(coldetstr, out coldetbool))
                this.CollisionDetect = coldetbool;

            string sysroot = cc.Get("SystemRoot", "");
            if (!String.IsNullOrEmpty(sysroot))
                this.RootDir = new DirectoryInfo(sysroot);

            string portdiroverlay = cc.Get("PortDir_Overlay", "");
            List<DirectoryInfo> overlaylist = new List<DirectoryInfo>();
            foreach (string overlay in portdiroverlay.Split(' ').Where(i => !String.IsNullOrWhiteSpace(i)))
                overlaylist.Add(new DirectoryInfo(overlay));
            this.PortDirOverlays = this.PortDirOverlays
                .Union(overlaylist)
                .Distinct()
                .ToArray();

            string mirrors = cc.Get("Mirrors", "");
            List<Uri> mirrorlist = new List<Uri>();
            foreach (string mirror in mirrors.Split(' ').Where(i => !String.IsNullOrWhiteSpace(i)))
                mirrorlist.Add(new Uri(mirror));
            this.RsyncMirrors = this.RsyncMirrors
                .Union(mirrorlist)
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Recursively finds the top of the profile cascade tree.
        /// </summary>
        /// <param name="curdir">starting directory</param>
        /// <param name="results">output list of results ordered top parent first</param>
        /// <param name="start">indicates the start of the recursive climb</param>
        private static void WalkProfileTree(DirectoryInfo curdir, List<DirectoryInfo> results, bool start)
        {
            FileInfo[] fiarr = curdir.GetFiles("parent").ToArray();
            string[] parentpath = (fiarr.Length != 0) ?
                File.ReadAllLines(fiarr[0].FullName) :
                null;

            if (parentpath == null) {
                results.Add(curdir);
                return;
            }

            Configuration.WalkProfileTree(
                new DirectoryInfo(curdir + @"\" + (start ? parentpath[1] : parentpath[0])), 
                results,
                false);
            results.Add(curdir);
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
        public Uri[] RsyncMirrors { get; set; }

        /// <summary>
        /// The selected profile directory.
        /// </summary>
        public DirectoryInfo ProfileDir { get; set; }

        /// <summary>
        /// Profile cascade tree ordered top-most parent first.
        /// </summary>
        public DirectoryInfo[] ProfileTree { get; set; }

        /// <summary>
        /// The profiles root directory.
        /// </summary>
        public DirectoryInfo ProfilesRootDir { get; set; }

        /// <summary>
        /// The native CPU architecture even when running under WOW64.
        /// </summary>
        public static CpuArchitecture RealCpuArch
        {
            get
            {
                if (_cpuarch == 0) {
                    string cpuarch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432");
                    if (cpuarch == null)
                        cpuarch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                    _cpuarch = (CpuArchitecture)Enum.Parse(typeof(CpuArchitecture), cpuarch.ToLower());
                }

                return _cpuarch;
            }
        }

        /// <summary>
        /// Root directory where packages are installed.
        /// </summary>
        public DirectoryInfo RootDir { get; set; }
    }
}
