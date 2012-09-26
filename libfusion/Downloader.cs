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
using System.Threading;

using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;

namespace Fusion.Framework
{
    /// <summary>
    /// Asynchronous HTTP package downloader.
    /// </summary>
    public sealed class Downloader
    {
        private static ILog _log = LogManager.GetLogger("fetch");

        private List<Tuple<Guid, IDistribution>> _queue;
        private HashSet<Guid> _finished;
        private Thread _thread;
        private DirectoryInfo _destdir;
        private string _logfile;

        /// <summary>
        /// Initialises the downloader.
        /// </summary>
        /// <param name="destdir">the destination download directory</param>
        public Downloader(DirectoryInfo destdir)
        {
            _queue = new List<Tuple<Guid, IDistribution>>();
            _finished = new HashSet<Guid>();
            _thread = new Thread(this.ThreadProc);
            _destdir = destdir;
            _logfile = null;

            foreach (IAppender ia in ((Logger)_log.Logger).Appenders) {
                if (ia is FileAppender) {
                    _logfile = ((FileAppender)ia).File;
                    break;
                }
            }
        }

        /// <summary>
        /// Adds a items to the queue for download.
        /// </summary>
        /// <param name="dist">distribution to enqueue</param>
        /// <returns>a fetch handle</returns>
        public Guid Enqueue(IDistribution dist)
        {
            Guid handle = Guid.NewGuid();

            lock (_queue) {
                _queue.Add(new Tuple<Guid, IDistribution>(handle, dist));
            }

            return handle;
        }

        /// <summary>
        /// Starts the download thread.
        /// </summary>
        public void FetchAsync()
        {
            if (!_thread.IsAlive)
                _thread.Start();
        }

        /// <summary>
        /// Quickly checks to see if the given fetch handle has finished.
        /// </summary>
        /// <param name="handle">fetch handle</param>
        /// <returns>true if the download is finished, false otherwise</returns>
        public bool Peek(Guid handle)
        {
            bool result;

            lock (_finished) {
                result = _finished.Contains(handle);
            }

            return result;
        }

        /// <summary>
        /// Download thread procedure.
        /// </summary>
        /// <param name="pv">not used</param>
        private void ThreadProc(object pv)
        {
            int tid = Thread.CurrentThread.ManagedThreadId;

            if (!_destdir.Exists) {
                _log.DebugFormat("[{0}] Creating temporary directory: {1}", tid, _destdir.FullName);
                System.IO.Directory.CreateDirectory(_destdir.FullName);
            }

            _log.InfoFormat("[{0}] Starting parallel fetch", tid);

            while (true) {
                Tuple<Guid, IDistribution> current = null;

                lock (_queue) {
                    if (_queue.Count > 0) {
                        current = _queue[0];
                        _queue.RemoveAt(0);
                    }
                }

                if (current == null) {
                    _log.InfoFormat("[{0}] Parallel fetch completed", tid);
                    break;
                }

                try {
                    foreach (SourceFile src in current.Item2.Sources) {
                        FileInfo fi = new FileInfo(_destdir + @"\" + src.LocalName);

                        if (fi.Exists) {
                            if (!Md5Sum.Check(fi.FullName, src.Digest, Md5Sum.MD5SUMMODE.BINARY)) {
                                _log.InfoFormat("[{0}] Previous download is corrupted", tid);
                                File.Delete(fi.FullName);   /* bad checksum so force re-download */
                            } else {
                                _log.InfoFormat("[{0}] Found cached archive: {1}", tid, src.LocalName);
                                continue;
                            }
                        }

                        if (src is WebSourceFile) {
                            _log.InfoFormat("[{0}] Downloading {1}", tid, ((WebSourceFile)src).Location);
                            ((WebSourceFile)src).Fetch(_destdir);
                        }
                    }
                } catch (Exception ex) {
                    _log.ErrorFormat("[{0}] {1}", tid, ex.Message);
                }

                lock (_finished) {
                    _finished.Add(current.Item1);
                }
            }
        }

        /// <summary>
        /// Blocks until the given fetch handle is finished downloading.
        /// </summary>
        /// <param name="handle">fetch handle</param>
        public void WaitFor(Guid handle)
        {
            while (true) {
                lock (_finished) {
                    if (_finished.Contains(handle)) {
                        _finished.Remove(handle);
                        break;
                    }
                }

                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Indicates if the downloader is currently running.
        /// </summary>
        public bool IsRunning
        {
            get { return _thread.IsAlive; }
        }

        /// <summary>
        /// File path of the first log file appender.
        /// </summary>
        public string LogFile
        {
            get { return _logfile; }
        }

        /// <summary>
        /// Number of items remaining in the queue.
        /// </summary>
        public int QueueCount
        {
            get
            {
                int count;

                lock (_queue) {
                    count = _queue.Count;
                }

                return count;
            }
        }
    }
}
