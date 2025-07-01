using ImGuiNET;
using Somno.Native.WinUSER;
using System;
using System.Drawing;

namespace Somno.UI.Engine
{
    internal class ImGuiInputHandler
    {
        readonly WindowHost host;
        ImGuiMouseCursor lastCursor;

        public ImGuiInputHandler(WindowHost host)
        {
            this.host = host;
        }

        public bool Update()
        {
            var io = ImGui.GetIO();
            UpdateMousePosition(io, host.WindowHandle);
            var mouseCursor = io.MouseDrawCursor ? ImGuiMouseCursor.None : ImGui.GetMouseCursor();
            if (mouseCursor != lastCursor) {
                lastCursor = mouseCursor;
                UpdateMouseCursor(io, mouseCursor);
            }

            if (!io.WantCaptureMouse && ImGui.IsAnyMouseDown()) {
                // workaround: where overlay gets stuck in a non-clickable mode forever.
                for (var i = 0; i < 5; i++) {
                    io.AddMouseButtonEvent(i, false);
                }
            }

            return io.WantCaptureMouse;
        }

        public bool ProcessMessage(WindowMessage msg, UIntPtr wParam, IntPtr lParam)
        {
            if (ImGui.GetCurrentContext() == IntPtr.Zero)
                return false;

            var io = ImGui.GetIO();
            switch (msg) {
                case WindowMessage.SETFOCUS:
                case WindowMessage.KILLFOCUS:
                    io.AddFocusEvent(msg == WindowMessage.SETFOCUS);
                    break;
                case WindowMessage.LBUTTONDOWN:
                case WindowMessage.LBUTTONDBLCLK:
                case WindowMessage.LBUTTONUP:
                    io.AddMouseButtonEvent(0, msg != WindowMessage.LBUTTONUP);
                    break;
                case WindowMessage.RBUTTONDOWN:
                case WindowMessage.RBUTTONDBLCLK:
                case WindowMessage.RBUTTONUP:
                    io.AddMouseButtonEvent(1, msg != WindowMessage.RBUTTONUP);
                    break;
                case WindowMessage.MBUTTONDOWN:
                case WindowMessage.MBUTTONDBLCLK:
                case WindowMessage.MBUTTONUP:
                    io.AddMouseButtonEvent(2, msg != WindowMessage.MBUTTONUP);
                    break;
                case WindowMessage.XBUTTONDOWN:
                case WindowMessage.XBUTTONDBLCLK:
                case WindowMessage.XBUTTONUP:
                    io.AddMouseButtonEvent(
                        GetXButtonWParam(wParam) == 1 ? 3 : 4,
                        msg != WindowMessage.XBUTTONUP);
                    break;
                case WindowMessage.MOUSEWHEEL:
                    io.AddMouseWheelEvent(0.0f, GetWheelDeltaParam(wParam) / WHEEL_DELTA);
                    break;
                case WindowMessage.MOUSEHWHEEL:
                    io.AddMouseWheelEvent(-GetWheelDeltaParam(wParam) / WHEEL_DELTA, 0.0f);
                    break;
                case WindowMessage.KEYDOWN:
                case WindowMessage.SYSKEYDOWN:
                case WindowMessage.KEYUP:
                case WindowMessage.SYSKEYUP:
                    bool isKeyDown = msg is WindowMessage.SYSKEYDOWN or WindowMessage.KEYDOWN;
                    if ((ulong)wParam < 256 && TryMapKey((VirtualKey)wParam, out ImGuiKey imguikey)) {
                        io.AddKeyEvent(imguikey, isKeyDown);
                    }

                    break;
                case WindowMessage.CHAR:
                    io.AddInputCharacterUTF16((ushort)wParam);
                    break;
                case WindowMessage.SETCURSOR:
                    if ((((int)(long)lParam) & 0xFFFF) == 1) {
                        var mouseCursor = io.MouseDrawCursor ? ImGuiMouseCursor.None : ImGui.GetMouseCursor();
                        lastCursor = mouseCursor;
                        if (UpdateMouseCursor(io, mouseCursor)) {
                            return true;
                        }
                    }

                    break;
            }

            return false;
        }

        private static void UpdateMousePosition(ImGuiIOPtr io, IntPtr handleWindow)
        {
            if (User32.GetCursorPos(out Point pos) && User32.ScreenToClient(handleWindow, ref pos)) {
                io.AddMousePosEvent(pos.X, pos.Y);
            }
        }

        private bool UpdateMouseCursor(ImGuiIOPtr io, ImGuiMouseCursor requestedcursor)
        {
            if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0)
                return false;

            if (requestedcursor == ImGuiMouseCursor.None) {
                User32.SetCursor(IntPtr.Zero);
            }
            else {
                var cursor = SystemCursor.IDC_ARROW;
                switch (requestedcursor) {
                    case ImGuiMouseCursor.Arrow:
                        cursor = SystemCursor.IDC_ARROW;
                        break;
                    case ImGuiMouseCursor.TextInput:
                        cursor = SystemCursor.IDC_IBEAM;
                        break;
                    case ImGuiMouseCursor.ResizeAll:
                        cursor = SystemCursor.IDC_SIZEALL;
                        break;
                    case ImGuiMouseCursor.ResizeEW:
                        cursor = SystemCursor.IDC_SIZEWE;
                        break;
                    case ImGuiMouseCursor.ResizeNS:
                        cursor = SystemCursor.IDC_SIZENS;
                        break;
                    case ImGuiMouseCursor.ResizeNESW:
                        cursor = SystemCursor.IDC_SIZENESW;
                        break;
                    case ImGuiMouseCursor.ResizeNWSE:
                        cursor = SystemCursor.IDC_SIZENWSE;
                        break;
                    case ImGuiMouseCursor.Hand:
                        cursor = SystemCursor.IDC_HAND;
                        break;
                    case ImGuiMouseCursor.NotAllowed:
                        cursor = SystemCursor.IDC_NO;
                        break;
                }

                host.SetCursor((ushort)cursor);
            }

            return true;
        }

        private static bool TryMapKey(VirtualKey key, out ImGuiKey result)
        {
            static ImGuiKey KeyToImGuiKeyShortcut(VirtualKey keyToConvert, VirtualKey startKey1, ImGuiKey startKey2)
            {
                var changeFromStart1 = (int)keyToConvert - (int)startKey1;
                return startKey2 + changeFromStart1;
            }

            result = key switch {
                >= VirtualKey.F1 and <= VirtualKey.F12 => KeyToImGuiKeyShortcut(key, VirtualKey.F1, ImGuiKey.F1),
                >= VirtualKey.NUMPAD0 and <= VirtualKey.NUMPAD9 => KeyToImGuiKeyShortcut(key, VirtualKey.NUMPAD0, ImGuiKey.Keypad0),
                >= VirtualKey.KEY_A and <= VirtualKey.KEY_Z => KeyToImGuiKeyShortcut(key, VirtualKey.KEY_A, ImGuiKey.A),
                >= VirtualKey.KEY_0 and <= VirtualKey.KEY_9 => KeyToImGuiKeyShortcut(key, VirtualKey.KEY_0, ImGuiKey._0),
                VirtualKey.TAB => ImGuiKey.Tab,
                VirtualKey.LEFT => ImGuiKey.LeftArrow,
                VirtualKey.RIGHT => ImGuiKey.RightArrow,
                VirtualKey.UP => ImGuiKey.UpArrow,
                VirtualKey.DOWN => ImGuiKey.DownArrow,
                VirtualKey.PRIOR => ImGuiKey.PageUp,
                VirtualKey.NEXT => ImGuiKey.PageDown,
                VirtualKey.HOME => ImGuiKey.Home,
                VirtualKey.END => ImGuiKey.End,
                VirtualKey.INSERT => ImGuiKey.Insert,
                VirtualKey.DELETE => ImGuiKey.Delete,
                VirtualKey.BACK => ImGuiKey.Backspace,
                VirtualKey.SPACE => ImGuiKey.Space,
                VirtualKey.RETURN => ImGuiKey.Enter,
                VirtualKey.ESCAPE => ImGuiKey.Escape,
                VirtualKey.OEM_7 => ImGuiKey.Apostrophe,
                VirtualKey.OEM_COMMA => ImGuiKey.Comma,
                VirtualKey.OEM_MINUS => ImGuiKey.Minus,
                VirtualKey.OEM_PERIOD => ImGuiKey.Period,
                VirtualKey.OEM_2 => ImGuiKey.Slash,
                VirtualKey.OEM_1 => ImGuiKey.Semicolon,
                VirtualKey.OEM_PLUS => ImGuiKey.Equal,
                VirtualKey.OEM_4 => ImGuiKey.LeftBracket,
                VirtualKey.OEM_5 => ImGuiKey.Backslash,
                VirtualKey.OEM_6 => ImGuiKey.RightBracket,
                VirtualKey.OEM_3 => ImGuiKey.GraveAccent,
                VirtualKey.CAPITAL => ImGuiKey.CapsLock,
                VirtualKey.SCROLL => ImGuiKey.ScrollLock,
                VirtualKey.NUMLOCK => ImGuiKey.NumLock,
                VirtualKey.SNAPSHOT => ImGuiKey.PrintScreen,
                VirtualKey.PAUSE => ImGuiKey.Pause,
                VirtualKey.DECIMAL => ImGuiKey.KeypadDecimal,
                VirtualKey.DIVIDE => ImGuiKey.KeypadDivide,
                VirtualKey.MULTIPLY => ImGuiKey.KeypadMultiply,
                VirtualKey.SUBTRACT => ImGuiKey.KeypadSubtract,
                VirtualKey.ADD => ImGuiKey.KeypadAdd,
                VirtualKey.SHIFT => ImGuiKey.ModShift,
                VirtualKey.CONTROL => ImGuiKey.ModCtrl,
                VirtualKey.MENU => ImGuiKey.ModAlt,
                VirtualKey.LSHIFT => ImGuiKey.LeftShift,
                VirtualKey.LCONTROL => ImGuiKey.LeftCtrl,
                VirtualKey.LMENU => ImGuiKey.LeftAlt,
                VirtualKey.LWIN => ImGuiKey.LeftSuper,
                VirtualKey.RSHIFT => ImGuiKey.RightShift,
                VirtualKey.RCONTROL => ImGuiKey.RightCtrl,
                VirtualKey.RMENU => ImGuiKey.RightAlt,
                VirtualKey.RWIN => ImGuiKey.RightSuper,
                VirtualKey.APPS => ImGuiKey.Menu,
                _ => ImGuiKey.None
            };

            return result != ImGuiKey.None;
        }

        private static readonly float WHEEL_DELTA = 120;

        private static int GetWheelDeltaParam(UIntPtr wParam) => ((int)wParam) >> 16;

        private static int GetXButtonWParam(UIntPtr wParam) => ((int)wParam) >> 16;
    }
}
