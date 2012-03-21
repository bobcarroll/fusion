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
using System.Security.Cryptography;
using System.IO;

using log4net;

namespace Fusion.Framework
{
    /// <summary>
    /// Compute and check MD5 message digest. This class complies with the
    /// format supported by md5sum.
    /// </summary>
    public static class Md5Sum
    {
        /// <summary>
        /// MD5 checksum compute mode.
        /// </summary>
        public enum MD5SUMMODE : int
        {
            BINARY = 0,
            TEXT = 1
        }

        /// <summary>
        /// Computes the given file's MD5 digest.
        /// </summary>
        /// <param name="infile">the file to read</param>
        /// <param name="mode">the compute mode</param>
        /// <returns>a digest string for the given file</returns>
        public static string Compute(string infile, MD5SUMMODE mode)
        {
            if (mode != MD5SUMMODE.BINARY)
                throw new InvalidOperationException("md5sum mode is not supported!");

            byte[] buf = File.ReadAllBytes(infile);
            return Md5Sum.Compute(buf);
        }

        /// <summary>
        /// Computes the given buffer's MD5 digest.
        /// </summary>
        /// <param name="buf">the bytes to read</param>
        /// <returns>a digest string for the given buffer</returns>
        public static string Compute(byte[] buf)
        {
            string hash =
                BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(buf));

            return hash.Replace("-", "").ToLower();
        }

        /// <summary>
        /// Computes and checks MD5 digests in the given file.
        /// </summary>
        /// <param name="outfile">the .md5 file to read</param>
        /// <returns>true if all digests match, false otherwise</returns>
        public static bool Check(string outfile)
        {
            ILog log = LogManager.GetLogger(typeof(Md5Sum));

            if (!File.Exists(outfile))
                throw new FileNotFoundException("md5sum output not found: " + outfile);

            string[] lines = File.ReadAllLines(outfile);

            foreach (var l in lines) {
                string hash = l.Substring(0, l.IndexOf(' '));
                string mode = l.Substring(l.IndexOf(' ') + 1, 1);
                string infile = l.Substring(l.IndexOf(' ') + 2);

                string cmphash = Md5Sum.Compute(
                    infile,
                    mode == "*" ? MD5SUMMODE.BINARY : MD5SUMMODE.TEXT);
                if (hash.CompareTo(cmphash) != 0) {
                    log.WarnFormat("Computed checksum did not match: {0}", infile);
                    return false;
                } else
                    log.InfoFormat("Computed checksum matched: {0}", infile);
            }

            return true;
        }

        /// <summary>
        /// Computes and checks MD5 digests in the given file.
        /// </summary>
        /// <param name="infile">the file to check</param>
        /// <param name="expected">the expected digest value</param>
        /// <param name="mode">the compute mode</param>
        /// <returns>true if the digests match, false otherwise</returns>
        public static bool Check(string infile, string expected, MD5SUMMODE mode)
        {
            string computed = Md5Sum.Compute(infile, mode);
            return (computed == expected);
        }
    }
}
