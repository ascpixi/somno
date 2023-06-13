using Somno.Portal.Native.Structures.Kernel32;
using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Somno.Portal.Native
{
    internal static class Kernel32
    {
        public const uint TokenAdjustPrivilidges = 0x0020;

        /// <summary>
        /// Creates or opens a named or unnamed mutex object.
        /// </summary>
        /// <param name="lpMutexAttributes">A pointer to a SECURITY_ATTRIBUTES structure. If this parameter is NULL, the handle cannot be inherited by child processes.</param>
        /// <param name="bInitialOwner">If this value is TRUE and the caller created the mutex, the calling thread obtains initial ownership of the mutex object. Otherwise, the calling thread does not obtain ownership of the mutex.</param>
        /// <param name="lpName">The name of the mutex object. The name is limited to MAX_PATH characters. Name comparison is case sensitive.</param>
        /// <returns>If the function succeeds, the return value is a handle to the newly created mutex object.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateMutex(
            nint lpMutexAttributes,
            bool bInitialOwner,
            string lpName
        );

        /// <summary>
        /// Retrieves a pseudo handle for the current process.
        /// </summary>
        /// <returns>The return value is a pseudo handle to the current process.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="hObject">A valid handle to an open object.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Takes a snapshot of the specified processes, as well as the heaps, modules, and threads used by these processes.
        /// </summary>
        /// <param name="dwFlags">The portions of the system to be included in the snapshot.</param>
        /// <param name="th32ProcessID">The process identifier of the process to be included in the snapshot. This parameter can be zero to indicate the current process. This parameter is used when the TH32CS_SNAPHEAPLIST, TH32CS_SNAPMODULE, TH32CS_SNAPMODULE32, or TH32CS_SNAPALL value is specified. Otherwise, it is ignored and all processes are included in the snapshot.</param>
        /// <returns>If the function succeeds, it returns an open handle to the specified snapshot.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(
            SnapshotFlags dwFlags,
            uint th32ProcessID
        );

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32FirstW(
            IntPtr hSnapshot,
            ref ProcessEntry32W entry
        );

        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32NextW(
            IntPtr hSnapshot,
            ref ProcessEntry32W entry
        );

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr OpenFileMapping(
            FileMapAccess dwDesiredAccess,
            bool bInheritHandle,
            string lpName
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
             ProcessAccessFlags processAccess,
             bool bInheritHandle,
             uint processId
        );

        [DllImport("kernel32.dll")]
        public static extern int VirtualQueryEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            out MemoryBasicInformation lpBuffer,
            uint dwLength
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            IntPtr dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect
        );

        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAlloc(
            IntPtr lpAddress,
            nuint dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public unsafe static extern bool ReadProcessMemory(
            IntPtr hProcess,
            void* lpBaseAddress,
            void* lpBuffer,
            nuint dwSize,
            out nuint lpNumberOfBytesRead
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public unsafe static extern bool VirtualFree(
          [In] void* lpAddress,
          [In] nuint dwSize,
          [In] FreeType dwFreeType
        );

        [DllImport("kernel32.dll", EntryPoint = "RtlCopyMemory", SetLastError = false)]
        public unsafe static extern void CopyMemory(
            void* dest,
            void* src,
            uint count
        );
    }
}
