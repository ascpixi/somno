using System.Runtime.InteropServices;

namespace Somno.WindowHost.Native.WinUSER;

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
