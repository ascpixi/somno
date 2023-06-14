using Somno.Portal.Native.Data;
using Somno.Portal.Native.Data.Kernel32;
using Somno.Portal.Windows;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Runtime.InteropServices.ComTypes;

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
        public static extern Handle CreateMutex(
            [In, Optional] nint lpMutexAttributes,
            [In]           bool bInitialOwner,
            [In, Optional] string lpName
        );

        /// <summary>
        /// Retrieves a pseudo handle for the current process.
        /// </summary>
        /// <returns>The return value is a pseudo handle to the current process.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern Handle GetCurrentProcess();

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="hObject">A valid handle to an open object.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle([In] IntPtr hObject);

        /// <summary>
        /// Takes a snapshot of the specified processes, as well as the heaps, modules, and threads used by these processes.
        /// </summary>
        /// <param name="dwFlags">The portions of the system to be included in the snapshot.</param>
        /// <param name="th32ProcessID">The process identifier of the process to be included in the snapshot. This parameter can be zero to indicate the current process. This parameter is used when the TH32CS_SNAPHEAPLIST, TH32CS_SNAPMODULE, TH32CS_SNAPMODULE32, or TH32CS_SNAPALL value is specified. Otherwise, it is ignored and all processes are included in the snapshot.</param>
        /// <returns>If the function succeeds, it returns an open handle to the specified snapshot.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(
            [In] SnapshotFlags dwFlags,
            [In] uint th32ProcessID
        );

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32FirstW(
            IntPtr hSnapshot,
            ref ProcessEntry32W entry
        );

        /// <summary>
        /// Retrieves information about the first process encountered in a system snapshot.
        /// </summary>
        /// <param name="hSnapshot">A handle to the snapshot returned from a previous call to the CreateToolhelp32Snapshot function.</param>
        /// <param name="entry">A pointer to a PROCESSENTRY32W structure. It contains process information such as the name of the executable file, the process identifier, and the process identifier of the parent process.</param>
        /// <returns>Returns TRUE if the first entry of the process list has been copied to the buffer or FALSE otherwise. The ERROR_NO_MORE_FILES error value is returned by the GetLastError function if no processes exist or the snapshot does not contain process information.</returns>
        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32NextW(
            [In]      IntPtr hSnapshot,
            [In, Out] ref ProcessEntry32W entry
        );

        /// <summary>
        /// Opens a named file mapping object.
        /// </summary>
        /// <param name="dwDesiredAccess">The access to the file mapping object. This access is checked against any security descriptor on the target file mapping object. For a list of values, see File Mapping Security and Access Rights.</param>
        /// <param name="bInheritHandle">If this parameter is TRUE, a process created by the CreateProcess function can inherit the handle; otherwise, the handle cannot be inherited.</param>
        /// <param name="lpName">The name of the file mapping object to be opened. If there is an open handle to a file mapping object by this name and the security descriptor on the mapping object does not conflict with the dwDesiredAccess parameter, the open operation succeeds. The name can have a "Global\" or "Local\" prefix to explicitly open an object in the global or session namespace. The remainder of the name can contain any character except the backslash character (\). For more information, see Kernel Object Namespaces. Fast user switching is implemented using Terminal Services sessions. The first user to log on uses session 0, the next user to log on uses session 1, and so on. Kernel object names must follow the guidelines outlined for Terminal Services so that applications can support multiple users.</param>
        /// <returns>If the function succeeds, the return value is an open handle to the specified file mapping object.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern Handle OpenFileMapping(
            [In] FileMapAccess dwDesiredAccess,
            [In] bool bInheritHandle,
            [In] string lpName
        );

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern Handle CreateFileMapping(
            [In]           Handle hFile,
            [In, Optional] IntPtr lpFileMappingAttributes,
            [In]           PageProtection flProtect,
            [In]           uint dwMaximumSizeHigh,
            [In]           uint dwMaximumSizeLow,
            [In, Optional] string lpName
        );

        /// <summary>
        /// Opens an existing local process object.
        /// </summary>
        /// <param name="processAccess">The access to the process object. This access right is checked against the security descriptor for the process. This parameter can be one or more of the process access rights.</param>
        /// <param name="bInheritHandle">If this value is TRUE, processes created by this process will inherit the handle. Otherwise, the processes do not inherit this handle.</param>
        /// <param name="processId">The identifier of the local process to be opened.</param>
        /// <returns>If the function succeeds, the return value is an open handle to the specified process.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern Handle OpenProcess(
             [In] ProcessAccessFlags processAccess,
             [In] bool bInheritHandle,
             [In] uint processId
        );

        /// <summary>
        /// Retrieves information about a range of pages within the virtual address space of a specified process.
        /// </summary>
        /// <param name="hProcess">A handle to the process whose memory information is queried. The handle must have been opened with the PROCESS_QUERY_INFORMATION access right, which enables using the handle to read information from the process object. For more information, see Process Security and Access Rights.</param>
        /// <param name="lpAddress">A pointer to the base address of the region of pages to be queried. This value is rounded down to the next page boundary. To determine the size of a page on the host computer, use the GetSystemInfo function.</param>
        /// <param name="lpBuffer">A pointer to a MEMORY_BASIC_INFORMATION structure in which information about the specified page range is returned.</param>
        /// <param name="dwLength">The size of the buffer pointed to by the lpBuffer parameter, in bytes.</param>
        /// <returns>The return value is the actual number of bytes returned in the information buffer.</returns>
        [DllImport("kernel32.dll")]
        public static extern nuint VirtualQueryEx(
            [In]           Handle hProcess,
            [In, Optional] IntPtr lpAddress,
            [Out]          out MemoryBasicInformation lpBuffer,
            [In]           nuint dwLength
        );

        /// <summary>
        /// Reserves, commits, or changes the state of a region of pages in the virtual address space of the calling process. Memory allocated by this function is automatically initialized to zero.
        /// </summary>
        /// <param name="lpAddress">The starting address of the region to allocate. If the memory is being reserved, the specified address is rounded down to the nearest multiple of the allocation granularity. If the memory is already reserved and is being committed, the address is rounded down to the next page boundary. To determine the size of a page and the allocation granularity on the host computer, use the GetSystemInfo function. If this parameter is NULL, the system determines where to allocate the region.</param>
        /// <param name="dwSize">The size of the region, in bytes. If the lpAddress parameter is NULL, this value is rounded up to the next page boundary. Otherwise, the allocated pages include all pages containing one or more bytes in the range from lpAddress to lpAddress+dwSize. This means that a 2-byte range straddling a page boundary causes both pages to be included in the allocated region.</param>
        /// <param name="flAllocationType">The type of memory allocation.</param>
        /// <param name="flProtect">The memory protection for the region of pages to be allocated. If the pages are being committed, you can specify any one of the memory protection constants.</param>
        /// <returns>If the function succeeds, the return value is the base address of the allocated region of pages.</returns>
        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAlloc(
            [In, Optional] IntPtr lpAddress,
            [In]           nuint dwSize,
            [In]           AllocationType flAllocationType,
            [In]           MemoryProtection flProtect
        );

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hProcess">A handle to the process with memory that is being read. The handle must have PROCESS_VM_READ access to the process.</param>
        /// <param name="lpBaseAddress">A pointer to the base address in the specified process from which to read. Before any data transfer occurs, the system verifies that all data in the base address and memory of the specified size is accessible for read access, and if it is not accessible the function fails.</param>
        /// <param name="lpBuffer">A pointer to a buffer that receives the contents from the address space of the specified process.</param>
        /// <param name="dwSize">The number of bytes to be read from the specified process.</param>
        /// <param name="lpNumberOfBytesRead">A pointer to a variable that receives the number of bytes transferred into the specified buffer. If lpNumberOfBytesRead is NULL, the parameter is ignored.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public unsafe static extern bool ReadProcessMemory(
            [In]  Handle hProcess,
            [In]  void* lpBaseAddress,
            [Out] void* lpBuffer,
            [In]  nuint dwSize,
            [Out] out nuint lpNumberOfBytesRead
        );

        /// <summary>
        /// Releases, decommits, or releases and decommits a region of pages within the virtual address space of the calling process.
        /// </summary>
        /// <param name="lpAddress">A pointer to the base address of the region of pages to be freed.</param>
        /// <param name="dwSize">The size of the region of memory to be freed, in bytes.</param>
        /// <param name="dwFreeType">The type of free operation.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public unsafe static extern bool VirtualFree(
          [In] void* lpAddress,
          [In] nuint dwSize,
          [In] FreeType dwFreeType
        );

        /// <summary>
        /// Copies a block of memory from one location to another.
        /// </summary>
        /// <param name="dest">A pointer to the starting address of the copied block's destination.</param>
        /// <param name="src">A pointer to the starting address of the block of memory to copy.</param>
        /// <param name="count">The size of the block of memory to copy, in bytes.</param>
        // https://learn.microsoft.com/en-us/previous-versions/windows/desktop/legacy/aa366535(v=vs.85)
        // TODO: Are we sure we need to use CopyMemory?
        [DllImport("kernel32.dll", EntryPoint = "RtlCopyMemory", SetLastError = false)]
        public unsafe static extern void CopyMemory(
            [In] void* dest,
            [In] void* src,
            [In] nuint count
        );

        /// <summary>
        /// Retrieves information about the first thread of any process encountered in a system snapshot.
        /// </summary>
        /// <param name="hSnapshot">A handle to the snapshot returned from a previous call to the CreateToolhelp32Snapshot function.</param>
        /// <param name="lpte">A pointer to a THREADENTRY32 structure.</param>
        /// <returns>Returns TRUE if the first entry of the thread list has been copied to the buffer or FALSE otherwise. The ERROR_NO_MORE_FILES error value is returned by the GetLastError function if no threads exist or the snapshot does not contain thread information.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Thread32First(
            [In]      Handle hSnapshot,
            [In, Out] ref ThreadEntry32 lpte
        );

        /// <summary>
        /// Retrieves information about the next thread of any process encountered in the system memory snapshot.
        /// </summary>
        /// <param name="hSnapshot">A handle to the snapshot returned from a previous call to the CreateToolhelp32Snapshot function.</param>
        /// <param name="lpte">A pointer to a THREADENTRY32 structure.</param>
        /// <returns>Returns TRUE if the next entry of the thread list has been copied to the buffer or FALSE otherwise. The ERROR_NO_MORE_FILES error value is returned by the GetLastError function if no threads exist or the snapshot does not contain thread information.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Thread32Next(
            [In]  Handle hSnapshot,
            [Out] out ThreadEntry32 lpte
        );

        /// <summary>
        /// Opens an existing thread object.
        /// </summary>
        /// <param name="dwDesiredAccess">The access to the thread object. This access right is checked against the security descriptor for the thread. This parameter can be one or more of the thread access rights.</param>
        /// <param name="bInheritHandle">If this value is TRUE, processes created by this process will inherit the handle. Otherwise, the processes do not inherit this handle.</param>
        /// <param name="dwThreadId">The identifier of the thread to be opened.</param>
        /// <returns>If the function succeeds, the return value is an open handle to the specified thread.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern Handle OpenThread(
            [In] ThreadAccessFlags dwDesiredAccess,
            [In] bool bInheritHandle,
            [In] uint dwThreadId
        );

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hThread"></param>
        /// <param name="lpCreationTime"></param>
        /// <param name="lpExitTime"></param>
        /// <param name="lpKernelTime"></param>
        /// <param name="lpUserTime"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetThreadTimes(
            [In]  Handle hThread,
            [Out] out FILETIME lpCreationTime,
            [Out] out FILETIME lpExitTime,
            [Out] out FILETIME lpKernelTime,
            [Out] out FILETIME lpUserTime
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static unsafe extern void* MapViewOfFile(
            [In] Handle hFileMappingObject,
            [In] PageProtection dwDesiredAccess,
            [In] uint dwFileOffsetHigh,
            [In] uint dwFileOffsetLow,
            [In] nuint dwNumberOfBytesToMap
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DuplicateHandle(
            [In]  Handle hSourceProcessHandle,
            [In]  Handle hSourceHandle,
            [In]  Handle hTargetProcessHandle,
            [Out] out Handle lpTargetHandle,
            [In]  uint dwDesiredAccess,
            [In]  bool bInheritHandle,
            [In]  uint dwOptions
        );

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(
            IntPtr hModule,
            string procName
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(
            [MarshalAs(UnmanagedType.LPWStr)] string lpModuleName
        );
    }
}
