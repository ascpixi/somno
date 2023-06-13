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
        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool EnumProcessModules(
            [In] IntPtr hProcess,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.SysUInt)][In][Out] IntPtr[] lphModule,
            uint cb,
            out uint lpcbNeeded
        );
    }
}
