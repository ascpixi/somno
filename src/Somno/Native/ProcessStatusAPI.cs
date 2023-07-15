using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Native
{
    internal static class ProcessStatusAPI
    {
        [DllImport("psapi.dll")]
        public static extern uint GetProcessImageFileNameW(
            IntPtr hProcess,
            [Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpImageFileName,
            [In]  [MarshalAs(UnmanagedType.U4)] int nSize
        );

        [DllImport("psapi.dll")]
        public static extern uint GetModuleFileNameEx(
            IntPtr hProcess,
            IntPtr hModule,
            [Out] StringBuilder lpBaseName,
            [In][MarshalAs(UnmanagedType.U4)] int nSize
        );
    }
}
