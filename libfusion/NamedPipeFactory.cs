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
using System.ComponentModel;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Win32.SafeHandles;

namespace Fusion.Framework
{
    /// <summary>
    /// Creates named pipes for interprocess communication.
    /// </summary>
    public static class NamedPipeFactory
    {
        /// <summary>
        /// Contains the security descriptor for an object and specifies whether the handle 
        /// retrieved by specifying this structure is inheritable
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        /// <summary>
        /// Creates an instance of a named pipe and returns a handle for subsequent pipe operations.
        /// </summary>
        /// <param name="lpName">unique pipe name</param>
        /// <param name="dwOpenMode">open mode</param>
        /// <param name="dwPipeMode">pipe mode</param>
        /// <param name="nMaxInstances"> maximum number of instances that can be created for this pipe</param>
        /// <param name="nOutBufferSize">number of bytes to reserve for the output buffer</param>
        /// <param name="nInBufferSize">number of bytes to reserve for the input buffer</param>
        /// <param name="nDefaultTimeOut">default time-out value, in milliseconds</param>
        /// <param name="lpSecurityAttributes">specifies a security descriptor for the new named pipe</param>
        /// <returns>on succeess, the return value is a handle to the server end of a named pipe instance</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafePipeHandle CreateNamedPipe(string lpName, int dwOpenMode,
            int dwPipeMode, int nMaxInstances, int nOutBufferSize, int nInBufferSize, int nDefaultTimeOut,
            IntPtr lpSecurityAttributes);

        /// <summary>
        /// Converts a string-format security descriptor into a valid, functional security descriptor.
        /// </summary>
        /// <param name="StringSecurityDescriptor">string-format security descriptor to conver</param>
        /// <param name="StringSDRevision">revision level of the StringSecurityDescriptor string</param>
        /// <param name="SecurityDescriptor">pointer to the converted security descriptor</param>
        /// <param name="SecurityDescriptorSize">size, in bytes, of the converted security descriptor</param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = false)]
        private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
            [In] string StringSecurityDescriptor,
            [In] uint StringSDRevision,
            [Out] out IntPtr SecurityDescriptor,
            [Out] out int SecurityDescriptorSize
        );

        /// <summary>
        /// Creates a new named pipe with a low integrity level.
        /// </summary>
        /// <param name="name">name of the pipe</param>
        /// <param name="direction">pip direction</param>
        /// <returns>handle to the server end of a named pipe instance</returns>
        public static SafePipeHandle CreateLowIntegrityPipe(string name, PipeDirection direction)
        {
            IntPtr sdptr = IntPtr.Zero;
            int sdsz = 0;

            if (!ConvertStringSecurityDescriptorToSecurityDescriptor("S:(ML;;NW;;;LW)", 1, out sdptr, out sdsz))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            SECURITY_ATTRIBUTES security = new SECURITY_ATTRIBUTES();
            security.nLength = Marshal.SizeOf(security);
            security.bInheritHandle = 1;
            security.lpSecurityDescriptor = sdptr;

            IntPtr secptr = Marshal.AllocHGlobal(Marshal.SizeOf(security));
            Marshal.StructureToPtr(security, secptr, false);

            string path = @"\\.\pipe\" + name;
            int mode = (int)direction | (int)PipeOptions.Asynchronous;

            SafePipeHandle result = CreateNamedPipe(path, mode, 0, 1, 0, 0, 0, secptr);
            if (result.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            Marshal.FreeHGlobal(secptr);

            return result;
        }
    }
}
