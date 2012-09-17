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
using System.Linq;
using System.Text;

using Microsoft.Build.Framework;

using log4net;
using log4net.Core;

namespace Fusion.Framework
{
    /// <summary>
    /// Logger for redirecting MSBuild log messages to log4net.
    /// </summary>
    public sealed class BuildLogRedirector : Microsoft.Build.Framework.ILogger
    {
        private ILog _l4nlog;
        private LoggerVerbosity _ll;
        
        /// <summary>
        /// Initialises the logger.
        /// </summary>
        /// <param name="l4nlog">a log4net log</param>
        public BuildLogRedirector(ILog l4nlog)
        {
            _l4nlog = LogManager.GetLogger(l4nlog.GetType().Assembly, "msbuild");
            _ll = LoggerVerbosity.Normal;

            string ll = ConfigurationManager.AppSettings["MSBuild.Verbosity"];
            if (!String.IsNullOrWhiteSpace(ll))
                Enum.TryParse<LoggerVerbosity>(ll, out _ll);
        }

        #region ILogger Members
        public void Initialize(IEventSource eventSource)
        {
            eventSource.BuildFinished += new BuildFinishedEventHandler(this.Source_OnBuildFinished);
            eventSource.BuildStarted += new BuildStartedEventHandler(this.Source_OnBuildStarted);
            eventSource.CustomEventRaised += new CustomBuildEventHandler(this.Source_OnCustomBuildEvent);
            eventSource.ErrorRaised += new BuildErrorEventHandler(this.Source_OnErrorRaised);
            eventSource.MessageRaised += new BuildMessageEventHandler(this.Source_OnMessageRaised);
            eventSource.ProjectFinished += new ProjectFinishedEventHandler(this.Source_OnProjectFinished);
            eventSource.ProjectStarted += new ProjectStartedEventHandler(this.Source_OnProjectStarted);
            eventSource.TargetFinished += new TargetFinishedEventHandler(this.Source_OnTargetFinished);
            eventSource.TargetStarted += new TargetStartedEventHandler(this.Source_OnTargetStarted);
            eventSource.TaskFinished += new TaskFinishedEventHandler(this.Source_OnTaskFinished);
            eventSource.TaskStarted += new TaskStartedEventHandler(this.Source_OnTaskStarted);
            eventSource.WarningRaised += new BuildWarningEventHandler(this.Source_OnWarningRasied);
        }

        public string Parameters
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public void Shutdown()
        {
            
        }

        public LoggerVerbosity Verbosity
        {
            get { return _ll; }
            set { _ll = value; }
        }
        #endregion

        #region MSBuild event handlers
        private void Source_OnBuildFinished(object sender, BuildFinishedEventArgs e)
        {
            if (this.Verbosity < LoggerVerbosity.Normal)
                return;

            _l4nlog.Logger.Log(
                this.GetType().DeclaringType, 
                log4net.Core.Level.Fine, 
                String.Format("\n{0}", e.Message), 
                null);
        }

        private void Source_OnBuildStarted(object sender, BuildStartedEventArgs e)
        {
            if (this.Verbosity < LoggerVerbosity.Normal)
                return;

            _l4nlog.Logger.Log(
                this.GetType().DeclaringType,
                log4net.Core.Level.Fine,
                String.Format("\n{0}\n", e.Message),
                null);
        }

        private void Source_OnCustomBuildEvent(object sender, CustomBuildEventArgs e)
        {
            if (this.Verbosity < LoggerVerbosity.Minimal)
                return;

            if (e.BuildEventContext.TaskId != -1)
                _l4nlog.Logger.Log(
                    this.GetType().DeclaringType,
                    log4net.Core.Level.Notice,
                    String.Format("    {0}", e.Message),
                    null);
            else if (e.BuildEventContext.TargetId != -1)
                _l4nlog.Logger.Log(
                    this.GetType().DeclaringType,
                    log4net.Core.Level.Notice,
                    String.Format("  {0}", e.Message),
                    null);
            else
                _l4nlog.Logger.Log(
                    this.GetType().DeclaringType,
                    log4net.Core.Level.Notice,
                    e.Message,
                    null);
        }

        private void Source_OnErrorRaised(object sender, BuildErrorEventArgs e)
        {
            if (e.BuildEventContext.TaskId != -1)
                _l4nlog.ErrorFormat("    {0}", e.Message);
            else if (e.BuildEventContext.TargetId != -1)
                _l4nlog.ErrorFormat("  {0}", e.Message);
            else
                _l4nlog.Error(e.Message);
        }

        private void Source_OnMessageRaised(object sender, BuildMessageEventArgs e)
        {
            if (this.Verbosity < LoggerVerbosity.Detailed && e.Importance == MessageImportance.Low)
                return;
            else if (this.Verbosity < LoggerVerbosity.Normal && e.Importance == MessageImportance.Normal)
                return;

            if (e.BuildEventContext.TaskId != -1)
                _l4nlog.Logger.Log(
                    this.GetType().DeclaringType,
                    log4net.Core.Level.Notice,
                    String.Format("    {0}", e.Message),
                    null);
            else if (e.BuildEventContext.TargetId != -1)
                _l4nlog.Logger.Log(
                    this.GetType().DeclaringType,
                    log4net.Core.Level.Notice,
                    String.Format("  {0}", e.Message),
                    null);
            else
                _l4nlog.Logger.Log(
                    this.GetType().DeclaringType,
                    log4net.Core.Level.Notice,
                    e.Message,
                    null);
        }

        private void Source_OnProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            if (this.Verbosity < LoggerVerbosity.Detailed)
                return;

            _l4nlog.InfoFormat("\n{0}", e.Message);
        }

        private void Source_OnProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            if (this.Verbosity < LoggerVerbosity.Normal)
                return;

            _l4nlog.InfoFormat("{0}\n", e.Message);
        }

        private void Source_OnTargetFinished(object sender, TargetFinishedEventArgs e)
        {
            if (this.Verbosity < LoggerVerbosity.Detailed)
                return;

            _l4nlog.Info(e.Message);
        }

        private void Source_OnTargetStarted(object sender, TargetStartedEventArgs e)
        {
            if (this.Verbosity < LoggerVerbosity.Normal)
                return;

            _l4nlog.Info(e.Message);
        }

        private void Source_OnTaskFinished(object sender, TaskFinishedEventArgs e)
        {
            if (this.Verbosity < LoggerVerbosity.Detailed)
                return;

            _l4nlog.InfoFormat("  {0}", e.Message);
        }

        private void Source_OnTaskStarted(object sender, TaskStartedEventArgs e)
        {
            if (this.Verbosity < LoggerVerbosity.Detailed)
                return;

            _l4nlog.InfoFormat("  {0}", e.Message);
        }

        private void Source_OnWarningRasied(object sender, BuildWarningEventArgs e)
        {
            if (e.BuildEventContext.TaskId != -1)
                _l4nlog.WarnFormat("    {0}", e.Message);
            else if (e.BuildEventContext.TargetId != -1)
                _l4nlog.WarnFormat("  {0}", e.Message);
            else
                _l4nlog.Warn(e.Message);
        }
        #endregion
    }
}
