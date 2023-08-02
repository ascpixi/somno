using System;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Somno.Native
{
    /// <summary>
    /// Exposes functions from the <c>KERNEL32</c> Dynamic-Link Library.
    /// </summary>
    internal unsafe static partial class Kernel32
    {
        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="hObject">A valid handle to an open object.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(
            [In] IntPtr hObject
        );

        /// <summary>
        /// Retrieves a module handle for the specified module. The module must have been loaded by the calling process.
        /// </summary>
        /// <param name="lpModuleName">The name of the loaded module (either a .dll or .exe file). If the file name extension is omitted, the default library extension .dll is appended. The file name string can include a trailing point character (.) to indicate that the module name has no extension. The string does not have to specify a path. When specifying a path, be sure to use backslashes (\), not forward slashes (/). The name is compared (case independently) to the names of modules currently mapped into the address space of the calling process.</param>
        /// <returns>If the function succeeds, the return value is a handle to the specified module.</returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string? lpModuleName);

        /// <summary>
        /// Loads the specified module into the address space of the calling process. The specified module may cause other modules to be loaded.
        /// </summary>
        /// <param name="lpLibFileName">The name of the module. This can be either a library module (a .dll file) or an executable module (an .exe file).</param>
        /// <returns>If the function succeeds, the return value is a handle to the module.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        public static extern nint LoadLibrary([In] string lpLibFileName);

        /// <summary>
        /// Retrieves the address of an exported function (also known as a procedure) or variable from the specified dynamic-link library (DLL).
        /// </summary>
        /// <param name="hModule">A handle to the DLL module that contains the function or variable. The LoadLibrary, LoadLibraryEx, LoadPackagedLibrary, or GetModuleHandle function returns this handle.</param>
        /// <param name="lpProcName">The function or variable name, or the function's ordinal value. If this parameter is an ordinal value, it must be in the low-order word; the high-order word must be zero.</param>
        /// <returns>If the function succeeds, the return value is the address of the exported function or variable.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        public static extern void* GetProcAddress(
            [In] nint hModule,
            [In] string lpProcName
        );

        // https://learn.microsoft.com/en-us/windows/console/setconsolectrlhandler?WT.mc_id=DT-MVP-5003978
        /// <summary>
        /// Adds or removes an application-defined HandlerRoutine function from the list of handler functions for the calling process.
        /// </summary>
        /// <param name="handler">A pointer to the application-defined HandlerRoutine function to be added or removed. This parameter can be NULL.</param>
        /// <param name="add">If this parameter is TRUE, the handler is added; if it is FALSE, the handler is removed.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleCtrlHandler(
            [In, Optional] SetConsoleCtrlEventHandler handler, 
            [In]           bool add
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DeviceIoControl(
            [In]                nint hDevice,
            [In]                uint dwIoControlCode,
            [In, Optional]      void* lpInBuffer,
            [In]                uint nInBufferSize,
            [Out, Optional]     void* lpOutBuffer,
            [In]                uint nOutBufferSize,
            [Out]               out uint lpBytesReturned,
            [In, Out, Optional] void* lpOverlapped
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern nint CreateFile(
            [In]           string lpFileName,
            [In]           uint dwDesiredAccess,
            [In]           uint dwShareMode,
            [In, Optional] nint lpSecurityAttributes,
            [In]           uint dwCreationDisposition,
            [In]           uint dwFlagsAndAttributes,
            [In, Optional] nint hTemplateFile
        );

        /// <summary>
        /// An application-defined function used with the SetConsoleCtrlHandler function. A console process uses this function to handle control signals received by the process. When the signal is received, the system creates a new thread in the process to execute the function.
        /// </summary>
        /// <param name="sig">The type of control signal received by the handler.</param>
        /// <returns>If the function handles the control signal, it should return TRUE. If it returns FALSE, the next handler function in the list of handlers for this process is used.</returns>
        // https://learn.microsoft.com/en-us/windows/console/handlerroutine?WT.mc_id=DT-MVP-5003978
        public delegate bool SetConsoleCtrlEventHandler(CtrlType sig);

        public enum CtrlType
        {
            /// <summary>
            /// A CTRL+C signal was received, either from keyboard input or from a signal generated by the GenerateConsoleCtrlEvent function.
            /// </summary>
            CtrlCEvent = 0,

            /// <summary>
            /// A CTRL+BREAK signal was received, either from keyboard input or from a signal generated by GenerateConsoleCtrlEvent.
            /// </summary>
            CtrlBreakEvent = 1,

            /// <summary>
            /// A signal that the system sends to all processes attached to a console when the user closes the console (either by clicking Close on the console window's window menu, or by clicking the End Task button command from Task Manager).
            /// </summary>
            CtrlCloseEvent = 2,

            /// <summary>
            /// A signal that the system sends to all console processes when a user is logging off. This signal does not indicate which user is logging off, so no assumptions can be made.
            /// <br/><br/>
            /// Note that this signal is received only by services. Interactive applications are terminated at logoff, so they are not present when the system sends this signal.
            /// </summary>
            CtrlLogoffEvent = 5,

            /// <summary>
            /// A signal that the system sends when the system is shutting down. Interactive applications are not present by the time the system sends this signal, therefore it can be received only be services in this situation. Services also have their own notification mechanism for shutdown events. For more information, see Handler.
            /// </summary>
            CtrlShutdownEvent = 6
        }
    }
}
