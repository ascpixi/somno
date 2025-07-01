using System;

namespace Somno.WindowHost.Native.WinUSER;

[Flags]
internal enum WindowClassStyles : uint
{
    ByteAlignClient = 0x1000,
    ByteAlignWindow = 0x2000,
    ClassDC = 0x0040,
    DoubleClicks = 0x0008,
    DropShadow = 0x00020000,
    GlobalClass = 0x4000,
    HRedraw = 0x0002,
    NoClose = 0x0200,
    OwnDC = 0x0020,
    ParentDC = 0x0080,
    SaveBits = 0x0800,
    VRedraw = 0x0001
}
