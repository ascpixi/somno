using Somno.Portal.Native.Data;
using Somno.Portal.Native.Data.NTDll;
using Somno.Portal.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Portal.Native
{
    internal static class NTDll
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        public static unsafe extern NTStatus NtQueryInformationThread(
            [In]            Handle threadHandle,
            [In]            ThreadInfoClass threadInfoClass,
            [In, Out]       void* threadInformation,
            [In]            uint threadInformationLength,
            [Out, Optional] out uint returnLength
        );
    }
}
