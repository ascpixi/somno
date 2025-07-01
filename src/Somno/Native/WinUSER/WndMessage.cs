using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Somno.Native.WinUSER;

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
