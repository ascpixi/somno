using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.WindowHost.Native.WinUSER
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowMessage
    {
        public nint Handle;
        public uint Message;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public Point Pt;
        public uint Private;
    }
}
