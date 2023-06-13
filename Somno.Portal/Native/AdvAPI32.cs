using Somno.Portal.Native.Structures.AdvAPI32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Somno.Portal.Native
{
    internal static class AdvAPI32
    {
        internal const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        internal const uint SE_PRIVILEGE_REMOVED = 0x00000004;

        /// <summary>
        /// The OpenProcessToken function opens the access token associated with a process.
        /// </summary>
        /// <param name="processHandle">A handle to the process whose access token is opened. The process must have the PROCESS_QUERY_LIMITED_INFORMATION access permission. See Process Security and Access Rights for more info.</param>
        /// <param name="desiredAccess">Specifies an access mask that specifies the requested types of access to the access token. These requested access types are compared with the discretionary access control list (DACL) of the token to determine which accesses are granted or denied.</param>
        /// <param name="tokenHandle">A pointer to a handle that identifies the newly opened access token when the function returns.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(
            IntPtr processHandle,
            uint desiredAccess,
            out IntPtr tokenHandle
        );

        /// <summary>
        /// The LookupPrivilegeValue function retrieves the locally unique identifier (LUID) used on a specified system to locally represent the specified privilege name.
        /// </summary>
        /// <param name="lpSystemName">A pointer to a null-terminated string that specifies the name of the system on which the privilege name is retrieved. If a null string is specified, the function attempts to find the privilege name on the local system.</param>
        /// <param name="lpName">A pointer to a null-terminated string that specifies the name of the privilege, as defined in the Winnt.h header file. For example, this parameter could specify the constant, SE_SECURITY_NAME, or its corresponding string, "SeSecurityPrivilege".</param>
        /// <param name="lpLuid">A pointer to a variable that receives the LUID by which the privilege is known on the system specified by the lpSystemName parameter.</param>
        /// <returns>If the function succeeds, the function returns nonzero.</returns>
        [DllImport("advapi32.dll")]
        public static extern bool LookupPrivilegeValue(
            string lpSystemName,
            string lpName,
            ref LUID lpLuid
        );

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AdjustTokenPrivileges(
            IntPtr tokenHandle,
            [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
            ref TokenPrivileges newState,
            UInt32 bufferLengthInBytes,
            IntPtr previousState,
            IntPtr returnLengthInBytes
        );
    }
}
