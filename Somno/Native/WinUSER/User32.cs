using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace Somno.Native.WinUSER
{
    /// <summary>
    /// Exposes functions from the <c>USER32</c> Dynamic-Link Library.
    /// </summary>
    internal static class User32
    {
        /// <summary>
        /// Registers a window class for subsequent use in calls to the CreateWindow or CreateWindowEx function.
        /// </summary>
        /// <param name="lpwcx">A pointer to a WNDCLASSEX structure. You must fill the structure with the appropriate class attributes before passing it to the function.</param>
        /// <returns>If the function succeeds, the return value is a class atom that uniquely identifies the class being registered. This atom can only be used by the CreateWindow, CreateWindowEx, GetClassInfo, GetClassInfoEx, FindWindow, FindWindowEx, and UnregisterClass functions and the IActiveIMMap::FilterClientWindows method.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern ushort RegisterClassEx(
            [In] ref WndClassEx lpwcx
        );

        /// <summary>
        /// Unregisters a window class, freeing the memory required for the class.
        /// </summary>
        /// <param name="lpClassName">A null-terminated string or a class atom. If lpClassName is a string, it specifies the window class name. This class name must have been registered by a previous call to the RegisterClass or RegisterClassEx function. System classes, such as dialog box controls, cannot be unregistered. If this parameter is an atom, it must be a class atom created by a previous call to the RegisterClass or RegisterClassEx function. The atom must be in the low-order word of lpClassName; the high-order word must be zero.</param>
        /// <param name="hInstance">A handle to the instance of the module that created the class.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool UnregisterClass(
            string lpClassName,
            IntPtr hInstance
        );

        /// <summary>
        /// Calls the default window procedure to provide default processing for any window messages that an application does not process. This function ensures that every message is processed. DefWindowProc is called with the same parameters received by the window procedure.
        /// </summary>
        /// <param name="hWnd">A handle to the window procedure that received the message.</param>
        /// <param name="msg">The message</param>
        /// <param name="wParam">Additional message information. The content of this parameter depends on the value of the Msg parameter.</param>
        /// <param name="lParam">Additional message information</param>
        /// <returns>The return value is the result of the message processing and depends on the message.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr DefWindowProc(
            IntPtr hWnd,
            uint msg,
            UIntPtr wParam,
            IntPtr lParam
        );

        /// <summary>
        /// Loads the specified cursor resource from the executable (.EXE) file associated with an application instance.
        /// </summary>
        /// <param name="hInstance">A handle to an instance of the module whose executable file contains the cursor to be loaded.</param>
        /// <param name="lpCursorResource">The name of the cursor resource to be loaded. Alternatively, this parameter can consist of the resource identifier in the low-order word and zero in the high-order word. The MAKEINTRESOURCE macro can also be used to create this value.</param>
        /// <returns>If the function succeeds, the return value is the handle to the newly loaded cursor.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadCursor(
            IntPtr hInstance,
            IntPtr lpCursorResource
        );

        /// <summary>
        /// Sets the process-default DPI awareness to system-DPI awareness. This is equivalent to calling SetProcessDpiAwarenessContext with a DPI_AWARENESS_CONTEXT value of DPI_AWARENESS_CONTEXT_SYSTEM_AWARE.
        /// </summary>
        /// <returns>If the function succeeds, the return value is nonzero. Otherwise, the return value is zero.</returns>
        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        /// <inheritdoc cref="LoadCursor(nint, nint)"/>
        public static IntPtr LoadCursor(IntPtr hInstance, SystemCursor cursor)
        {
            return LoadCursor(hInstance, new IntPtr((int)cursor));
        }

        /// <summary>
        /// Dispatches incoming nonqueued messages, checks the thread message queue for a posted message, and retrieves the message (if any exist).
        /// </summary>
        /// <param name="lpMsg">A pointer to an MSG structure that receives message information.</param>
        /// <param name="hWnd">A handle to the window whose messages are to be retrieved. The window must belong to the current thread.</param>
        /// <param name="wMsgFilterMin">The value of the first message in the range of messages to be examined. Use WM_KEYFIRST (0x0100) to specify the first keyboard message or WM_MOUSEFIRST (0x0200) to specify the first mouse message</param>
        /// <param name="wMsgFilterMax">The value of the last message in the range of messages to be examined. Use WM_KEYLAST to specify the last keyboard message or WM_MOUSELAST to specify the last mouse message</param>
        /// <param name="wRemoveMsg">Specifies how messages are to be handled</param>
        /// <returns>If a message is available, the return value is nonzero.</returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "PeekMessageW")]
        public static extern bool PeekMessage(
            out WndMessage lpMsg,
            IntPtr hWnd,
            uint wMsgFilterMin,
            uint wMsgFilterMax,
            uint wRemoveMsg
        );

        /// <summary>
        /// Translates virtual-key messages into character messages. The character messages are posted to the calling thread's message queue, to be read the next time the thread calls the GetMessage or PeekMessage function.
        /// </summary>
        /// <param name="lpMsg">A pointer to an MSG structure that contains message information retrieved from the calling thread's message queue by using the GetMessage or PeekMessage function.</param>
        /// <returns>If the message is translated (that is, a character message is posted to the thread's message queue), the return value is nonzero.</returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern bool TranslateMessage(
            [In] ref WndMessage lpMsg
        );

        /// <summary>
        /// Dispatches a message to a window procedure. It is typically used to dispatch a message retrieved by the GetMessage function.
        /// </summary>
        /// <param name="lpmsg">A pointer to a structure that contains the message.</param>
        /// <returns>The return value specifies the value returned by the window procedure. Although its meaning depends on the message being dispatched, the return value generally is ignored.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr DispatchMessage(
            [In] ref WndMessage lpmsg
        );

        /// <summary>
        /// Retrieves information about the specified window. The function also retrieves the value at a specified offset into the extra window memory.
        /// </summary>
        /// <param name="hWnd">A handle to the window and, indirectly, the class to which the window belongs.</param>
        /// <param name="nIndex">The zero-based offset to the value to be retrieved. Valid values are in the range zero through the number of bytes of extra window memory, minus the size of a LONG_PTR</param>
        /// <returns>If the function succeeds, the return value is the requested value</returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowLongPtr(
            IntPtr hWnd,
            int nIndex
        );

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLong")]
        private static extern uint GetWindowLong32b(
            IntPtr hWnd,
            int nIndex
        );

        /// <summary>
        /// Retrieves information about the specified window. The function also retrieves the 32-bit (DWORD) value at the specified offset into the extra window memory.
        /// </summary>
        /// <param name="hWnd">A handle to the window and, indirectly, the class to which the window belongs.</param>
        /// <param name="nIndex">The zero-based offset to the value to be retrieved. Valid values are in the range zero through the number of bytes of extra window memory, minus four; for example, if you specified 12 or more bytes of extra memory, a value of 8 would be an index to the third 32-bit integer</param>
        /// <returns>If the function succeeds, the return value is the requested value.</returns>
        public static uint GetWindowLong(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4) {
                return GetWindowLong32b(hWnd, nIndex);
            }

            return GetWindowLongPtr(hWnd, nIndex);
        }

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLong")]
        private static extern uint SetWindowLong32b(
            IntPtr hWnd,
            int nIndex,
            uint value
        );

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SetWindowLongPtr(
            IntPtr hWnd,
            int nIndex,
            uint value
        );

        /// <summary>
        /// Changes an attribute of the specified window. The function also sets the 32-bit (long) value at the specified offset into the extra window memory.
        /// </summary>
        /// <param name="hWnd">A handle to the window and, indirectly, the class to which the window belongs</param>
        /// <param name="nIndex">The zero-based offset to the value to be set. Valid values are in the range zero through the number of bytes of extra window memory, minus the size of an integer</param>
        /// <param name="value">The replacement value.</param>
        /// <returns>If the function succeeds, the return value is the previous value of the specified 32-bit integer.</returns>
        public static uint SetWindowLong(IntPtr hWnd, int nIndex, uint value)
        {
            if (IntPtr.Size == 4) {
                return SetWindowLong32b(hWnd, nIndex, value);
            }

            return SetWindowLongPtr(hWnd, nIndex, value);
        }

        /// <summary>
        /// Creates an overlapped, pop-up, or child window with an extended window style; otherwise, this function is identical to the CreateWindow function. For more information about creating a window and for full descriptions of the other parameters of CreateWindowEx, see CreateWindow.
        /// </summary>
        /// <param name="exStyle">The extended window style of the window being created</param>
        /// <param name="className">A null-terminated string or a class atom created by a previous call to the RegisterClass or RegisterClassEx function. The atom must be in the low-order word of lpClassName; the high-order word must be zero. If lpClassName is a string, it specifies the window class name. The class name can be any name registered with RegisterClass or RegisterClassEx, provided that the module that registers the class is also the module that creates the window. The class name can also be any of the predefined system class names</param>
        /// <param name="windowName">The window name. If the window style specifies a title bar, the window title pointed to by lpWindowName is displayed in the title bar. When using CreateWindow to create controls, such as buttons, check boxes, and static controls, use lpWindowName to specify the text of the control. When creating a static control with the SS_ICON style, use lpWindowName to specify the icon name or identifier.</param>
        /// <param name="style">The style of the window being created. This parameter can be a combination of the window style values, plus the control styles indicated in the Remarks section</param>
        /// <param name="x">The initial horizontal position of the window. For an overlapped or pop-up window, the x parameter is the initial x-coordinate of the window's upper-left corner, in screen coordinates. For a child window, x is the x-coordinate of the upper-left corner of the window relative to the upper-left corner of the parent window's client area. If x is set to CW_USEDEFAULT, the system selects the default position for the window's upper-left corner and ignores the y parameter. CW_USEDEFAULT is valid only for overlapped windows; if it is specified for a pop-up or child window, the x and y parameters are set to zero.</param>
        /// <param name="y">The initial vertical position of the window. For an overlapped or pop-up window, the y parameter is the initial y-coordinate of the window's upper-left corner, in screen coordinates. For a child window, y is the initial y-coordinate of the upper-left corner of the child window relative to the upper-left corner of the parent window's client area. For a list box y is the initial y-coordinate of the upper-left corner of the list box's client area relative to the upper-left corner of the parent window's client area</param>
        /// <param name="width">The width, in device units, of the window. For overlapped windows, nWidth is the window's width, in screen coordinates, or CW_USEDEFAULT. If nWidth is CW_USEDEFAULT, the system selects a default width and height for the window; the default width extends from the initial x-coordinates to the right edge of the screen; the default height extends from the initial y-coordinate to the top of the icon area. CW_USEDEFAULT is valid only for overlapped windows; if CW_USEDEFAULT is specified for a pop-up or child window, the nWidth and nHeight parameter are set to zero</param>
        /// <param name="height">The height, in device units, of the window. For overlapped windows, nHeight is the window's height, in screen coordinates. If the nWidth parameter is set to CW_USEDEFAULT, the system ignores nHeight</param>
        /// <param name="hwndParent">A handle to the parent or owner window of the window being created. To create a child window or an owned window, supply a valid window handle. This parameter is optional for pop-up windows</param>
        /// <param name="menu">A handle to a menu, or specifies a child-window identifier, depending on the window style. For an overlapped or pop-up window, hMenu identifies the menu to be used with the window; it can be NULL if the class menu is to be used. For a child window, hMenu specifies the child-window identifier, an integer value used by a dialog box control to notify its parent about events. The application determines the child-window identifier; it must be unique for all child windows with the same parent window</param>
        /// <param name="instance">A handle to the instance of the module to be associated with the window</param>
        /// <param name="pvParam">Pointer to a value to be passed to the window through the CREATESTRUCT structure (lpCreateParams member) pointed to by the lParam param of the WM_CREATE message. This message is sent to the created window by this function before it returns</param>
        /// <returns>If the function succeeds, the return value is a handle to the new window</returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowEx(
            int exStyle,
            string className,
            string windowName,
            int style,
            int x, int y,
            int width, int height,
            IntPtr hwndParent,
            IntPtr menu,
            IntPtr instance,
            IntPtr pvParam
        );

        /// <summary>
        /// Destroys the specified window. The function sends WM_DESTROY and WM_NCDESTROY messages to the window to deactivate it and remove the keyboard focus from it. The function also destroys the window's menu, flushes the thread message queue, destroys timers, removes clipboard ownership, and breaks the clipboard viewer chain (if the window is at the top of the viewer chain).
        /// </summary>
        /// <param name="windowHandle">A handle to the window to be destroyed.</param>
        /// <returns></returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern bool DestroyWindow(
            IntPtr windowHandle
        );

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern bool ShowWindow(
            IntPtr hWnd,
            ShowWindowCommand nCmdShow
        );

        [DllImport("user32.dll")]
        public static extern IntPtr SetCursor(
            IntPtr handle
        );

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(
            IntPtr hWnd,
            ref Point lpPoint
        );

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(
            out Point lpPoint
        );

        [DllImport("user32.dll")]
        public static extern short GetKeyState(
            VirtualKey nVirtKey
        );

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(
            IntPtr hWnd
        );

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(
            IntPtr hWnd,
            int x,
            int y,
            int nWidth,
            int nHeight,
            bool bRepaint
        );

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern int GetSystemMetrics(
            int smIndex
        );
    }
}
