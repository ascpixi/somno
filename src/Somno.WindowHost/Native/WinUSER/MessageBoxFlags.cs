using System;

namespace Somno.WindowHost.Native.WinUSER;

[Flags]
internal enum MessageBoxFlags : uint
{
    OK = 0x0,
    Error = 0x10,
}
