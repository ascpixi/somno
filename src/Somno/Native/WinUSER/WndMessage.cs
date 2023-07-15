using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Native.WinUSER
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct WndMessage
    {
        public IntPtr Hwnd;
        public uint Value;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public Point Point;
    }
}
