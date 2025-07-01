using Somno.Native;
using Somno.Native.WinUSER;
using System.ComponentModel;

namespace Somno
{
    /// <summary>
    /// Provides methods related to keyboard I/O.
    /// </summary>
    internal static class Keyboard
    {
        private const int WH_KEYBOARD_LL = 13;

        /// <summary>
        /// Registers a low-level keyboard hook, which calls the given
        /// callback for any key-press.
        /// </summary>
        /// <param name="callback">The callback to call for each key-press in the system.</param>
        /// <returns>The handle (ID) for the hook.</returns>
        public static nint SetHook(User32.LowLevelKeyboardProc callback)
        {
            nint handle = User32.SetWindowsHookEx(
                WH_KEYBOARD_LL,
                callback,
                Kernel32.GetModuleHandle(null),
                default
            );

            if (handle is 0 or -1)
                throw new Win32Exception();

            return handle;
        }

        /// <summary>
        /// Unregisters a low-level keyboard hook by the given handle (ID).
        /// </summary>
        /// <param name="hookId">The handle (ID) to un-register.</param>
        public static void Unhook(nint hookId)
        {
            User32.UnhookWindowsHookEx(hookId);
        }
    }
}
