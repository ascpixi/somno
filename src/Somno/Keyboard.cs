using Somno.Native;
using Somno.Native.WinUSER;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Somno
{
    internal class Keyboard
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        public static nint SetHook(User32.LowLevelKeyboardProc callback)
        {
            return User32.SetWindowsHookEx(
                WH_KEYBOARD_LL,
                callback,
                Kernel32.GetModuleHandle(null),
                default
            );
        }

        public static void Unhook(nint hookId)
        {
            User32.UnhookWindowsHookEx(hookId);
        }
    }
}
