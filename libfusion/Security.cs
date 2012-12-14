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
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace Fusion.Framework
{
    /// <summary>
    /// Provides security helpers.
    /// </summary>
    public static class Security
    {
        #region Win32 Imports
        private const int ACL_REVISION = 2;

        private const uint LABEL_SECURITY_INFORMATION = 0x10;

        private static readonly byte[] MANDATORY_LABEL_AUTHORITY = new byte[] { 0, 0, 0, 0, 0, 16};

        private const int OBJECT_INHERIT_ACE = 0x1;
        private const int CONTAINER_INHERIT_ACE = 0x2;
        private const int NO_PROPAGATE_INHERIT_ACE = 0x4;
        private const int INHERIT_ONLY_ACE = 0x8;
        private const int INHERITED_ACE = 0x10;

        private const int SYSTEM_MANDATORY_LABEL_NO_WRITE_UP = 0x1;
        private const int SYSTEM_MANDATORY_LABEL_NO_READ_UP = 0x2;
        private const int SYSTEM_MANDATORY_LABEL_NO_EXECUTE_UP = 0x4;

        private const int SECURITY_MANDATORY_LOW_RID = 0x1000;
        private const int SECURITY_MANDATORY_MEDIUM_RID = 0x2000;
        private const int SECURITY_MANDATORY_HIGH_RID = 0x3000;

        private const int SE_GROUP_INTEGRITY = 0x20;

        private const int SAFER_SCOPEID_MACHINE = 1;
        private const int SAFER_SCOPEID_USER = 2;

        private const int SAFER_LEVELID_CONSTRAINED = 0x10000;
        private const int SAFER_LEVELID_DISALLOWED = 0x00000;
        private const int SAFER_LEVELID_FULLYTRUSTED = 0x40000;
        private const int SAFER_LEVELID_NORMALUSER = 0x20000;
        private const int SAFER_LEVELID_UNTRUSTED = 0x10000;

        private const int SAFER_LEVEL_OPEN = 1;

        private enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ACE_HEADER
        {
            public byte AceType;
            public byte AceFlags;
            public int AceSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ACL
        {
            public byte AclRevision;
            public byte Sbz1;
            public int AclSize;
            public int AceCount;
            public int Sbz2;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_DESCRIPTOR
        {
            public byte revision;
            public byte size;
            public short control;
            public IntPtr owner;
            public IntPtr group;
            public IntPtr sacl;
            public IntPtr dacl;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public int Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SID_IDENTIFIER_AUTHORITY
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] Value;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public uint cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_MANDATORY_LABEL_ACE
        {
            public ACE_HEADER Header;
            public ulong Mask;
            public int SidStart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_MANDATORY_LABEL
        {
            public SID_AND_ATTRIBUTES Label;
        }

        public class AccessToken
        {
            private IntPtr _handle;

            internal AccessToken(IntPtr handle)
            {
                _handle = handle;
            }

            internal IntPtr Handle
            {
                get { return _handle; }
            }
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AddMandatoryAce(IntPtr pAcl, int dwAceRevision,
            int AceFlags, int MandatoryPolicy, IntPtr pLabelSid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AllocateAndInitializeSid(IntPtr pIdentifierAuthority,
            byte nSubAuthorityCount, int dwSubAuthority0, int dwSubAuthority1,
            int dwSubAuthority2, int dwSubAuthority3, int dwSubAuthority4, int dwSubAuthority5,
            int dwSubAuthority6, int dwSubAuthority7, out IntPtr pSid);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, 
            string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles,
            uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetFileSecurity(string lpFileName, uint RequestInformation, 
            IntPtr pSecurityDescriptor, int nLength, ref int lpnLengthNeeded);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int GetLengthSid(IntPtr pSid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetSecurityDescriptorSacl(IntPtr pSecurityDescriptor,
            ref bool lpbSaclPresent, out IntPtr pSacl, ref bool lpbSaclDefaulted);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool InitializeAcl(IntPtr pAcl, int nAclLength, int dwAclRevision);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool MakeAbsoluteSD(IntPtr pSelfRelativeSD, IntPtr pAbsoluteSD,
            ref int lpdwAbsoluteSDSize, IntPtr pDacl, ref int lpdwDaclSize, IntPtr pSacl,
            ref int lpdwSaclSize, IntPtr pOwner, ref int lpdwOwnerSize, IntPtr pPrimaryGroup,
            ref int lpdwPrimaryGroupSize);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SaferCloseLevel(IntPtr hLevelHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SaferCreateLevel(int dwScopeId, int dwLevelId, int OpenFlags,
            out IntPtr pLevelHandle, IntPtr lpReserved);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SaferComputeTokenFromLevel(IntPtr LevelHandle, IntPtr InAccessToken,
            out IntPtr OutAccessToken, int dwFlags, IntPtr lpReserved);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetFileSecurity(string lpFileName, uint SecurityInformation,
            IntPtr pSecurityDescriptor);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetSecurityDescriptorSacl(IntPtr pSecurityDescriptor,
            bool lpbSaclPresent, IntPtr pSacl, bool lpbSaclDefaulted);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetTokenInformation(IntPtr TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, 
            int TokenInformationLength);
        #endregion

        /// <summary>
        /// Flags to control ACE inheritance.
        /// </summary>
        [Flags]
        public enum AceFlags : int
        {
            ObjectInherit = OBJECT_INHERIT_ACE,
            ContainerInherit = CONTAINER_INHERIT_ACE,
            NoPropagateInherit = NO_PROPAGATE_INHERIT_ACE,
            InheritOny = INHERIT_ONLY_ACE,
            Inherited = INHERITED_ACE
        }

        /// <summary>
        /// Windows mandatory integrity levels.
        /// </summary>
        public enum MandatoryLabel : int
        {
            LowIntegrity = SECURITY_MANDATORY_LOW_RID,
            MediumIntegrity = SECURITY_MANDATORY_MEDIUM_RID,
            HighIntegrity = SECURITY_MANDATORY_HIGH_RID
        }

        /// <summary>
        /// Access policy from principals with a mandatory integrity level lower
        /// than the object associated with the SACL.
        /// </summary>
        [Flags]
        public enum MandatoryPolicy : int
        {
            NoWriteUp = SYSTEM_MANDATORY_LABEL_NO_WRITE_UP,
            NoReadUP = SYSTEM_MANDATORY_LABEL_NO_READ_UP,
            NoExecuteUp = SYSTEM_MANDATORY_LABEL_NO_EXECUTE_UP
        }

        /// <summary>
        /// Creates a new process with the given access token.
        /// </summary>
        /// <param name="token">handle to an access token</param>
        /// <param name="filename">image file path</param>
        /// <param name="arguments">optional command arguments</param>
        /// <returns>the ID of the new process</returns>
        public static uint CreateProcessWithToken(AccessToken token, string filename, params string[] arguments)
        {
            STARTUPINFO si = new STARTUPINFO();
            PROCESS_INFORMATION pi;
            int errno;

            List<string> argsv = new List<string>() { "\"" + filename + "\"" };
            argsv.AddRange(arguments);

            if (!Security.CreateProcessAsUser(
                    token.Handle,
                    filename,
                    String.Join(" ", argsv),
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    0,
                    IntPtr.Zero,
                    null,
                    ref si,
                    out pi)) {
                errno = Marshal.GetLastWin32Error();
                throw new IOException("Failed to create new process. (" + errno + ")");
            }

            return pi.dwProcessId;
        }

        /// <summary>
        /// Creates a restricted access token. The caller is responsible for freeing
        /// the result with Security.FreeToken().
        /// </summary>
        /// <param name="low">flag for low integrity level</param>
        /// <returns>a handle to the access token</returns>
        public static AccessToken CreateRestrictedToken(bool low)
        {
            IntPtr lev;
            IntPtr token;
            int errno;

            if (!Security.SaferCreateLevel(
                    SAFER_SCOPEID_USER,
                    SAFER_LEVELID_NORMALUSER,
                    SAFER_LEVEL_OPEN,
                    out lev,
                    IntPtr.Zero)) {
                errno = Marshal.GetLastWin32Error();
                throw new IOException("Failed to create new safer level. (" + errno + ")");
            }

            if (!Security.SaferComputeTokenFromLevel(lev, IntPtr.Zero, out token, 0, IntPtr.Zero)) {
                errno = Marshal.GetLastWin32Error();
                SaferCloseLevel(lev);
                throw new IOException("Failed to create restricted token. (" + errno + ")");
            }

            SaferCloseLevel(lev);

            try {
                Security.SetTokenMandatoryLabel(
                    token,
                    low ? MandatoryLabel.LowIntegrity : MandatoryLabel.MediumIntegrity);
            } finally {
                Marshal.FreeHGlobal(token);
            }

            return new AccessToken(token);
        }

        /// <summary>
        /// Converts a self-relative security descriptor to an absolute security descriptor.
        /// </summary>
        /// <param name="srcsdptr">pointer to the source security descriptor</param>
        /// <param name="dstsdptr">output pointer to the destination security descriptor</param>
        /// <param name="errno">out buffer for the error code</param>
        /// <returns>true on success, false otherwise</returns>
        private static bool ConvertSecurityDescriptor(IntPtr srcsdptr, out IntPtr dstsdptr, out int errno)
        {
            dstsdptr = new IntPtr(0);
            IntPtr daclptr = new IntPtr(0);
            IntPtr saclptr = new IntPtr(0);
            IntPtr ownerptr = new IntPtr(0);
            IntPtr groupptr = new IntPtr(0);
            int sdsz = 0;
            int daclsz = 0;
            int saclsz = 0;
            int ownersz = 0;
            int groupsz = 0;
            bool result;
            errno = 0;

            Security.MakeAbsoluteSD(
                srcsdptr, 
                dstsdptr, 
                ref sdsz, 
                daclptr, 
                ref daclsz,
                saclptr, 
                ref saclsz, 
                ownerptr, 
                ref ownersz, 
                groupptr, 
                ref groupsz);

            dstsdptr = Marshal.AllocHGlobal(sdsz);
            daclptr = Marshal.AllocHGlobal(daclsz);
            saclptr = Marshal.AllocHGlobal(saclsz);
            ownerptr = Marshal.AllocHGlobal(ownersz);
            groupptr = Marshal.AllocHGlobal(groupsz);

            result = Security.MakeAbsoluteSD(
                srcsdptr, 
                dstsdptr, 
                ref sdsz, 
                daclptr, 
                ref daclsz, 
                saclptr, 
                ref 
                saclsz, 
                ownerptr, 
                ref ownersz, 
                groupptr, 
                ref groupsz);

            if (!result) {
                errno = Marshal.GetLastWin32Error();
                Marshal.FreeHGlobal(dstsdptr);
                dstsdptr = IntPtr.Zero;
            }

            Marshal.FreeHGlobal(daclptr);
            Marshal.FreeHGlobal(saclptr);
            Marshal.FreeHGlobal(ownerptr);
            Marshal.FreeHGlobal(groupptr);

            return result;
        }

        /// <summary>
        /// Determines if the Windows user that owns the thread has administrator
        /// rights on the local machine. If UAC is enabled, this function will fail
        /// for protected administrators and succeed for elevated administrators.
        /// </summary>
        /// <returns>true if admin, false otherwise</returns>
        public static bool IsNTAdmin()
        {
            WindowsPrincipal wp = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return wp.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// This function is a wrapper for Security.IsNTAdmin(). If the user is not
        /// an admin then an exception is thrown, otherwise no action is taken.
        /// </summary>
        public static void DemandNTAdmin()
        {
            if (!Security.IsNTAdmin())
                throw new UnauthorizedAccessException("You must be an administrator to execute this command.");
        }

        /// <summary>
        /// Frees the given access token.
        /// </summary>
        /// <param name="token">handle to the access token</param>
        public static void FreeToken(AccessToken token)
        {
            Marshal.FreeHGlobal(token.Handle);
        }

        /// <summary>
        /// Sets the mandatory label on the given file or directory object.
        /// </summary>
        /// <param name="target">target file or directory path</param>
        /// <param name="flags">flags to control inheritance</param>
        /// <param name="policy">access policy</param>
        /// <param name="mlrid">mandatory integrity level</param>
        public static void SetFileMandatoryLabel(string target, AceFlags flags, MandatoryPolicy policy, 
            MandatoryLabel mlrid)
        {
            SID_IDENTIFIER_AUTHORITY mlauth = new SID_IDENTIFIER_AUTHORITY() {
                Value = MANDATORY_LABEL_AUTHORITY
            };
            SYSTEM_MANDATORY_LABEL_ACE mlace = new SYSTEM_MANDATORY_LABEL_ACE();
            ACL acl = new ACL();
            IntPtr sdptr = new IntPtr(0);
            IntPtr saclptr;
            IntPtr mlauthptr;
            IntPtr sidptr;
            IntPtr newsdptr = new IntPtr(0);
            int szneeded = 0;
            int size = 0;
            bool present = false;
            bool defaulted = false;
            int errno = 0;

            if (!File.Exists(target) && !Directory.Exists(target))
                throw new FileNotFoundException("The path '" + target + "' was not found.");

            Security.GetFileSecurity(target, LABEL_SECURITY_INFORMATION, sdptr, size, ref szneeded);
            sdptr = Marshal.AllocHGlobal(szneeded);
            size = szneeded;

            if (!Security.GetFileSecurity(target, LABEL_SECURITY_INFORMATION, sdptr, size, ref szneeded)) {
                errno = Marshal.GetLastWin32Error();
                Marshal.FreeHGlobal(sdptr);
                throw new IOException("Failed to read file security information. (" + errno + ")");
            }

            if (!Security.GetSecurityDescriptorSacl(sdptr, ref present, out saclptr, ref defaulted)) {
                errno = Marshal.GetLastWin32Error();
                Marshal.FreeHGlobal(sdptr);
                throw new IOException("Failed to read security descriptor SACL. (" + errno + ")");
            }

            if (!Security.ConvertSecurityDescriptor(sdptr, out newsdptr, out errno)) {
                Marshal.FreeHGlobal(sdptr);
                throw new IOException("Failed to convert security descriptor. (" + errno + ")");
            }

            Marshal.FreeHGlobal(sdptr);

            mlauthptr = Marshal.AllocHGlobal(Marshal.SizeOf(mlauth));
            Marshal.StructureToPtr(mlauth, mlauthptr, false);

            if (!Security.AllocateAndInitializeSid(mlauthptr, 1, (int)mlrid, 0, 0, 0, 0, 0, 0, 0, out sidptr)) {
                errno = Marshal.GetLastWin32Error();
                Marshal.FreeHGlobal(newsdptr);
                Marshal.FreeHGlobal(mlauthptr);
                throw new IOException("Failed to allocate new SID. (" + errno + ")");
            }

            Marshal.FreeHGlobal(mlauthptr);

            int cbAcl = Marshal.SizeOf(acl) + Marshal.SizeOf(mlace) + (Security.GetLengthSid(sidptr) - sizeof(int));
            cbAcl = (cbAcl + (sizeof(int) - 1)) & 0xfffc;   /* align cbAcl to an int */
            saclptr = Marshal.AllocHGlobal(cbAcl);

            if (!InitializeAcl(saclptr, cbAcl, ACL_REVISION)) {
                errno = Marshal.GetLastWin32Error();
                Marshal.FreeHGlobal(newsdptr);
                Marshal.FreeHGlobal(sidptr);
                Marshal.FreeHGlobal(saclptr);
                throw new IOException("Failed to initialise new ACL. (" + errno + ")");
            }

            if (!Security.AddMandatoryAce(saclptr, ACL_REVISION, (int)flags, (int)policy, sidptr)) {
                errno = Marshal.GetLastWin32Error();
                Marshal.FreeHGlobal(newsdptr);
                Marshal.FreeHGlobal(sidptr);
                Marshal.FreeHGlobal(saclptr);
                throw new IOException("Failed to add new mandatory ACE. (" + errno + ")");
            }

            Marshal.FreeHGlobal(sidptr);

            if (!Security.SetSecurityDescriptorSacl(newsdptr, true, saclptr, false)) {
                errno = Marshal.GetLastWin32Error();
                Marshal.FreeHGlobal(newsdptr);
                Marshal.FreeHGlobal(saclptr);
                throw new IOException("Failed to set security descriptor SACL. (" + errno + ")");
            }

            if (!Security.SetFileSecurity(target, LABEL_SECURITY_INFORMATION, newsdptr)) {
                errno = Marshal.GetLastWin32Error();
                Marshal.FreeHGlobal(newsdptr);
                Marshal.FreeHGlobal(saclptr);
                throw new IOException("Failed to write file security information. (" + errno + ")");
            }

            Marshal.FreeHGlobal(newsdptr);
            Marshal.FreeHGlobal(saclptr);
        }

        /// <summary>
        /// Sets the integrity of the given access token.
        /// </summary>
        /// <param name="token">handle to the access token</param>
        /// <param name="mlrid">mandatory integrity level</param>
        private static void SetTokenMandatoryLabel(IntPtr token, MandatoryLabel mlrid)
        {
            SID_IDENTIFIER_AUTHORITY mlauth = new SID_IDENTIFIER_AUTHORITY() {
                Value = MANDATORY_LABEL_AUTHORITY
            };
            TOKEN_MANDATORY_LABEL tml = new TOKEN_MANDATORY_LABEL();
            IntPtr mlauthptr;
            IntPtr sidptr;
            IntPtr tmlptr;
            int tmlsz;
            int errno = 0;

            mlauthptr = Marshal.AllocHGlobal(Marshal.SizeOf(mlauth));
            Marshal.StructureToPtr(mlauth, mlauthptr, false);

            if (!Security.AllocateAndInitializeSid(mlauthptr, 1, (int)mlrid, 0, 0, 0, 0, 0, 0, 0, out sidptr)) {
                errno = Marshal.GetLastWin32Error();
                Marshal.FreeHGlobal(mlauthptr);
                throw new IOException("Failed to allocate new SID. (" + errno + ")");
            }

            Marshal.FreeHGlobal(mlauthptr);

            tml.Label.Sid = sidptr;
            tml.Label.Attributes = SE_GROUP_INTEGRITY;

            tmlsz = Marshal.SizeOf(tml);
            tmlptr = Marshal.AllocHGlobal(tmlsz);
            Marshal.StructureToPtr(tml, tmlptr, false);

            if (!SetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, tmlptr, tmlsz)) {
                errno = Marshal.GetLastWin32Error();
                Marshal.FreeHGlobal(sidptr);
                Marshal.FreeHGlobal(tmlptr);
                throw new IOException("Failed to set token mandatory label. (" + errno + ")");
            }

            Marshal.FreeHGlobal(sidptr);
            Marshal.FreeHGlobal(tmlptr);
        }
    }
}
