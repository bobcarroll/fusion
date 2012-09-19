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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using libconsole2;

using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;

using Fusion.Framework;

namespace fuse
{
    class Program
    {
        /* option group names */
        static string[] _groups = new string[] {
            "Help",
            "Actions",
            "Options"
        };

        /* command-line options */
        static OptionExtra[] _options = new OptionExtra[] {
            new OptionExtra() { 
                Option = new Option() { Name = "unmerge", Val = 'C' },
                Description = "Remove the selected package(s).",
                InGroup = _groups[1] },
            new OptionExtra() { 
                Option = new Option() { Name = "depclean", Val = 'c' },
                Description = "Remove packages that are not associated with any package in the world set.",
                InGroup = _groups[1] },
            new OptionExtra() { 
                Option = new Option() { Name = "emptytree", Val = 'E' },
                Description = "Reinstall the target atoms and their entire deep dependency tree.",
                InGroup = _groups[2] },
            new OptionExtra() { 
                Option = new Option() { Name = "exact", Val = 'e' },
                Description = "When searching, only show packages that match the search string exactly.",
                InGroup = _groups[2] },
            new OptionExtra() { 
                Option = new Option() { Name = "fetchonly", Val = 'f' },
                Description = "Instead of performing the install, download the packages that would be installed.",
                InGroup = _groups[2] },
            new OptionExtra() {
                Option = new Option() { Name = "help", Val = 'h' },
                Description = "Display command usage details (this screen).",
                InGroup = _groups[0] },
            new OptionExtra() { 
                Option = new Option() { Name = "merge", Val = 'm' },
                Description = "Install the selected package(s). This is the default action.",
                InGroup = _groups[1] },
            new OptionExtra() { 
                Option = new Option() { Name = "noreplace", Val = 'n' },
                Description = "Do not re-merge packages that are already installed. This is implied by 'update'.",
                InGroup = _groups[2] },
            new OptionExtra() { 
                Option = new Option() { Name = "pretend", Val = 'p' },
                Description = "Instead of performing the install, show what packages would be installed.",
                InGroup = _groups[2] },
            new OptionExtra() { 
                Option = new Option() { Name = "search", HasArg = ArgFlags.Required, Val = 's' },
                Description = "Search the local ports tree for package titles matching STRING.",
                InGroup = _groups[1],
                ArgLabel = "STRING" },
            new OptionExtra() { 
                Option = new Option() { Name = "update", Val = 'u' },
                Description = "Update selected packages to the best version available.",
                InGroup = _groups[2] },
            new OptionExtra() { 
                Option = new Option() { Name = "version", Val = 'V' },
                Description = "Display the currently installed version of Fusion.",
                InGroup = _groups[1] },
            new OptionExtra() { 
                Option = new Option() { Name = "verbose", Val = 'v' },
                Description = "Provide as much detail as possible while performing the requested action.",
                InGroup = _groups[2] },
            new OptionExtra() { 
                Option = new Option() { Name = "sync", Val = 'y' },
                Description = "Synchronise the local ports tree with a remote ports mirror.",
                InGroup = _groups[1] },
            new OptionExtra() { 
                Option = new Option() { Name = "oneshot", Val = '1' },
                Description = "Merge the package normally, but do not include the package in the world set.",
                InGroup = _groups[2] },
        };

        public static ILog _log = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// Application entry-point function.
        /// </summary>
        /// <param name="args">command-line arguments</param>
        /// <returns>an error code</returns>
        static int Main(string[] args)
        {
            string[] argv = System.Environment.GetCommandLineArgs();
            OptionTransformer ot = new OptionTransformer(_options);
            OptionPrinter op = new OptionPrinter(
                Path.GetFileName(argv[0]) + " [ options ] [ action ] [ atom ] [ ... ]",
                _options,
                _groups);
            GlibcOpt gopt = new GlibcOpt();
            int longindex = 0;
            IAction act = null;
            Options opts = new Options();
            int result;

            Console.CancelKeyPress += Program.sigtermproc;

            /* no args... show help */
            if (args.Length == 0)
                argv = new string[2] { argv[0], "-h" };

            while ((result = gopt.GetOptLong(ref argv, ot.ToOptString(), ot.ToLongOpts(), ref longindex)) != -1) {

                switch (result) {
                    case 'h':
                        op.PrintUsage();
                        return 0;

                    case 'c':
                        if (!set_action(new DepCleanAction(), ref act))
                            return 1;
                        break;

                    case 'e':
                        opts.exact = true;
                        break;

                    case 'E':
                        opts.emptytree = true;
                        break;

                    case 'f':
                        opts.fetchonly = true;
                        break;

                    case 'm':
                        if (!set_action(new MergeAction(), ref act))
                            return 1;
                        break;

                    case 'n':
                        opts.noreplace = true;
                        break;

                    case 'p':
                        opts.pretend = true;
                        break;

                    case 'C':
                        if (!set_action(new UnmergeAction(), ref act))
                            return 1;
                        break;

                    case 's':
                        if (!set_action(new SearchAction(gopt.OptionArg), ref act))
                            return 1;
                        break;

                    case 'u':
                        opts.update = true;
                        break;

                    case 'v':
                        opts.verbose = true;
                        break;

                    case 'V':
                        if (!set_action(new VersionAction(), ref act))
                            return 1;
                        break;

                    case 'y':
                        if (!set_action(new SyncAction(), ref act))
                            return 1;
                        break;

                    case '1':
                        opts.oneshot = true;
                        break;

                    default:
                        Console.WriteLine("Type 'fuse --help' for command usage\n");
                        return 1;
                }
            }

            if ((act is MergeAction || act == null) && gopt.NextOption < argv.Length) {
                string[] atoms = new string[argv.Length - gopt.NextOption];
                Array.Copy(argv, gopt.NextOption, atoms, 0, argv.Length - gopt.NextOption);

                if (act == null)
                    act = new MergeAction();
                ((MergeAction)act).Atoms.AddRange(atoms);
            } else if (act is UnmergeAction && gopt.NextOption < argv.Length) {
                string[] atoms = new string[argv.Length - gopt.NextOption];
                Array.Copy(argv, gopt.NextOption, atoms, 0, argv.Length - gopt.NextOption);
                ((UnmergeAction)act).Atoms.AddRange(atoms);
            } else if (act == null) {
                error_msg("\n!!! No action requested.\n");
                return 1;
            }

            ConnectionStringSettings css = ConfigurationManager.ConnectionStrings["PackageDB"];
            string connstr = css != null ? css.ConnectionString : null;
            if (String.IsNullOrEmpty(connstr)) {
                error_msg("\n!!! No database connection string defined in configuration.\n");
                return 1;
            }

            Environment.SetEnvironmentVariable("CONSOLE", ConsoleEx.GetConsoleProcessId().ToString());

            try {
                XmlConfiguration cfg = XmlConfiguration.LoadSeries();
                GlobalContext.Properties["logdir"] = cfg.LogDir.FullName;
                log4net.Config.XmlConfigurator.Configure();

                if (opts.verbose) {
                    Hierarchy h = (Hierarchy)LogManager.GetRepository();
                    h.Root.Level = Level.Debug;
                    h.RaiseConfigurationChanged(EventArgs.Empty);
                }

                connstr = connstr.Replace("$(CONFDIR)", cfg.ConfDir.FullName);
                using (IPackageManager pkgmgr = PackageDatabase.Open(connstr, cfg)) {
                    act.Options = opts;
                    act.Execute(pkgmgr, cfg);
                }
            } catch (Exception ex) {
                error_msg("\n!!! {0}", ex.Message.Replace("\n", "\n!!! "));
                if (opts.verbose)
                    error_msg(ex.StackTrace);
                return 1;
            } finally {
                Console.Write("\n");
            }

            return 0;
        }

        /// <summary>
        /// TERM signal handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void sigtermproc(object sender, EventArgs e)
        {
            Console.WriteLine("\n");
            Console.CursorVisible = true;
        }

        /// <summary>
        /// Writes an error message to the console.
        /// </summary>
        /// <param name="fmt">message format to write</param>
        public static void error_msg(string fmt, params string[] args)
        {
            string msg = String.Format(fmt, args);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        /// <summary>
        /// Sets the given action. Guards against multiple actions.
        /// </summary>
        /// <param name="inact">new action</param>
        /// <param name="outact">reference to the action var</param>
        /// <returns>true on success, false on error</returns>
        static bool set_action(IAction inact, ref IAction outact)
        {
            if (outact != null) {
                error_msg("\n!!! Multiple actions requested... please choose one only.\n");
                return false;
            }

            outact = inact;
            return true;
        }
    }
}
