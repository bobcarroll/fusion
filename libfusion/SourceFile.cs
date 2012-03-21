using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Represents a source file that is on the local disk.
    /// </summary>
    public class SourceFile
    {
        private string _digest;
        private string _localname;
        private long _size;

        /// <summary>
        /// Initialises the local source.
        /// </summary>
        /// <param name="digest">expected digest of the file</param>
        /// <param name="localname">file name to use when saving to distfiles</param>
        /// <param name="size">size of the file in bytes</param>
        internal SourceFile(string digest, string localname, long size)
        {
            _digest = digest;
            _localname = localname;
            _size = size;
        }

        /// <summary>
        /// Expected digest of the file.
        /// </summary>
        public string Digest
        {
            get { return _digest; }
        }

        /// <summary>
        /// File name to use when saving to distfiles.
        /// </summary>
        public string LocalName
        {
            get { return _localname; }
        }

        /// <summary>
        /// Size of the file in bytes.
        /// </summary>
        public long Size
        {
            get { return _size; }
        }
    }
}
