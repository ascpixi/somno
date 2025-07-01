using System.Runtime.InteropServices;

namespace Somno.WindowHost.Native.WinUSER;

[StructLayout(LayoutKind.Sequential)]
internal struct Point
{
    public int X;
    public int Y;
}
