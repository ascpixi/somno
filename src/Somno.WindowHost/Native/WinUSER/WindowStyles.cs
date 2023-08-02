using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno.WindowHost.Native.WinUSER
{
    [Flags]
    internal enum WindowStyles : uint
    {
        Border = 0x00800000,
        Caption = 0x00C00000,
        Child = 0x40000000,
        ClipChildren = 0x02000000,
        ClipSiblings = 0x04000000,
        Disabled = 0x08000000,
        DialogFrame = 0x00400000,
        Group = 0x00020000,
        HorizontalScroll = 0x00100000,
        Minimize = 0x20000000,
        Maximized = 0x01000000,
        MaximizeBox = 0x00010000,
        MinimizeBox = 0x00020000,
        Overlapped = 0x00000000,
        Popup = 0x80000000,
        SizeBox = 0x00040000,
        SystemMenu = 0x00080000,
        TabStop = 0x00010000,
        ThickFrame = 0x00040000,
        Visible = 0x10000000,
        VerticalScroll = 0x00200000
    }
}
