using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.WindowHost.Native.WinUSER
{
    internal static partial class User32
    {
        [DllImport("user32.dll")]
        public static extern bool GetMessage(
            [Out]          out WindowMessage lpMsg,
            [In, Optional] nint hwnd,
            [In]           uint wMsgFilterMin,
            [In]           uint wMsgFilterMax
        );

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage(
            [In] ref WindowMessage lpMsg
        );

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(
            [In, Optional] nint hwnd,
            [In, Optional] string lpText,
            [In, Optional] string lpCaption,
            [In] MessageBoxFlags lpType
        );

        [DllImport("user32.dll")]
        public static extern nint DispatchMessage(
            [In] ref WindowMessage lpMessage
        );

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern ushort RegisterClassEx(
            [In] ref WndClassExW wndClass
        );

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern nint CreateWindowEx(
                       ExWindowStyles exStyle,
            [Optional] string? className,
            [Optional] string? windowName,
                       WindowStyles style,
                       int x,
                       int y,
                       int width,
                       int height,
            [Optional] nint hWndParent,
            [Optional] nint hMenu,
            [Optional] nint hInstance,
            [Optional] nint lpParam
        );

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern nint DefWindowProc(
            [In] nint hWnd,
            [In] uint msg,
            [In] nuint wParam,
            [In] nint lParam
        );

        [DllImport("user32.dll")]
        public static extern nint SetCursor(
            [In, Optional] nint hCursor
        );

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        public static extern nint LoadCursor(
              [In, Optional] nint hInstance,
              [In] int lpCursorName
        );

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern bool SetWindowDisplayAffinity(
            [In] IntPtr hWnd,
            [In] uint dwAffinity
        );
    }
}
