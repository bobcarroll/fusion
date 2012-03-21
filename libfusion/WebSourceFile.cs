using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Represents a source file that is downloaded from the web.
    /// </summary>
    public sealed class WebSourceFile : SourceFile
    {
        private Uri _location;

        /// <summary>
        /// Initialises the web source.
        /// </summary>
        /// <param name="location">remote URL to download from</param>
        /// <param name="digest">expected digest of the file</param>
        /// <param name="localname">file name to use when saving to distfiles</param>
        /// <param name="size">size of the file in bytes</param>
        internal WebSourceFile(Uri location, string digest, string localname, long size)
            : base(digest, localname, size)
        {
            _location = location;
        }

        /// <summary>
        /// Fetch the file and put it in the given directory.
        /// </summary>
        /// <param name="destdir">destination directory</param>
        public void Fetch(DirectoryInfo destdir)
        {
            try {
                WebClient wc = new WebClient();
                wc.DownloadFile(_location, destdir.FullName + @"\" + this.LocalName);
            } catch (WebException ex) {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Remote URI to download from.
        /// </summary>
        public Uri Location
        {
            get { return _location; }
        }
    }
}
