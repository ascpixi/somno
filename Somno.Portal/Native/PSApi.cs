using Somno.Portal.Native.Data.PSApi;
using Somno.Portal.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Portal.Native
{
    internal static class PSApi
    {
        /// <summary>
        /// Retrieves a handle for each module in the specified process.
        /// </summary>
        /// <param name="hProcess">A handle to the process.</param>
        /// <param name="lphModule">An array that receives the list of module handles.</param>
        /// <param name="cb">The size of the lphModule array, in bytes.</param>
        /// <param name="lpcbNeeded">The number of bytes required to store all module handles in the lphModule array.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("psapi.dll", SetLastError = true)]
        public static unsafe extern bool EnumProcessModules(
            [In]  Handle hProcess,
            [Out] IntPtr* lphModule,
            [In]  uint cb,
            [Out] out uint lpcbNeeded
        );

        /// <summary>
        /// Retrieves the fully qualified path for the file containing the specified module.
        /// </summary>
        /// <param name="hProcess">A handle to the process that contains the module.</param>
        /// <param name="hModule">A handle to the module. If this parameter is NULL, GetModuleFileNameEx returns the path of the executable file of the process specified in hProcess.</param>
        /// <param name="lpBaseName">A pointer to a buffer that receives the fully qualified path to the module. If the size of the file name is larger than the value of the nSize parameter, the function succeeds but the file name is truncated and null-terminated.</param>
        /// <param name="nSize">The size of the lpFilename buffer, in characters.</param>
        /// <returns>If the function succeeds, the return value specifies the length of the string copied to the buffer.</returns>
        [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint GetModuleFileNameEx(
            [In]           Handle hProcess,
            [In, Optional] IntPtr hModule,
            [Out]          StringBuilder lpBaseName,
            [In]           uint nSize
        );

        /// <summary>
        /// Retrieves information about the specified module in the MODULEINFO structure.
        /// </summary>
        /// <param name="hProcess">A handle to the process that contains the module.</param>
        /// <param name="hModule">A handle to the module.</param>
        /// <param name="lpmodinfo">A pointer to the MODULEINFO structure that receives information about the module.</param>
        /// <param name="cb">The size of the MODULEINFO structure, in bytes.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool GetModuleInformation(
            [In]  Handle hProcess,
            [In]  IntPtr hModule,
            [Out] out ModuleInfo lpmodinfo,
            [In]  uint cb
        );
    }
}
