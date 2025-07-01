using Somno.WindowHost.Native;
using Somno.WindowHost.Native.WinUSER;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;

namespace Somno.WindowHost;

internal class Program
{
    const string VigenerePipeNameKey = "VBGNSJVAUQZMZPOAIJHU";

    static NamedPipeClientStream? client;
    static string? pipeName;
    static nint hwnd;
    static bool ready;

    static unsafe void Main(string[] args)
    {
        if (args.Length != 1) {
            return;
        }

        pipeName = Vigenere.Decipher(args[0].Trim(), VigenerePipeNameKey);

#if DEBUG
        try {
#endif
            Initialize();
#if DEBUG
        }
        catch (Exception ex) {
            User32.MessageBox(
                0,
                $"Could not initialize the window host.\n\n{ex.GetType().FullName}\n{ex.Message}\n\nstacktrace:\n{ex.StackTrace}",
                "Somno Window Host",
                MessageBoxFlags.OK | MessageBoxFlags.Error
            );
            return;
        }
#endif

#if DEBUG
        try {
#endif
            while (User32.GetMessage(out WindowMessage msg, hwnd, 0, 0)) {
                User32.TranslateMessage(ref msg);
                User32.DispatchMessage(ref msg);
            }
#if DEBUG
        }
        catch (Exception ex) {
            User32.MessageBox(
                0,
                $"An exception occured in the message pump.\n\n{ex.GetType().FullName}\n{ex.Message}\n\nat\n{ex.StackTrace}",
                "Somno Window Host",
                MessageBoxFlags.OK | MessageBoxFlags.Error
            );

            return;
        }
#endif
    }

    static unsafe void Initialize()
    {
        if (Debugger.IsAttached) {
            Environment.Exit(Random.Shared.Next());
        }

        Thread.Sleep(10);
        client = new NamedPipeClientStream(".", pipeName!, PipeDirection.InOut);
        client.Connect();

        var className = new char[Random.Shared.Next(6, 14)]
            .Fill(() => (char)Random.Shared.Next('A', 'Z'))
            .Transform(x => Random.Shared.Next(2) == 1 ? char.ToUpper(x) : x)
            .CharsToString();

        var wndTitle = RandomProvider.GenerateSpacedSentence(Random.Shared.Next(3, 5));

        nint hInstance = Kernel32.GetModuleHandle(null);
        if (hInstance is 0 or -1) {
            throw new Exception("Could not retrieve the hInstance for the main module.");
        }

        fixed (char* classNamePtr = className) {
            var wndClass = new WndClassExW() {
                Size = (uint)Marshal.SizeOf<WndClassExW>(),
                WndProc = &WndProc,
                Instance = hInstance,
                ClassName = classNamePtr,
                Style = WindowClassStyles.HRedraw | WindowClassStyles.VRedraw | WindowClassStyles.ParentDC,
                Cursor = User32.LoadCursor(IntPtr.Zero, 32512),
                Brush = default,
                Icon = default,
                IconSmall = default,
            };
            
            var atom = User32.RegisterClassEx(ref wndClass);
            if(atom == 0) {
                throw new Win32Exception();
            }
        }

        hwnd = User32.CreateWindowEx(
            ExWindowStyles.AcceptFiles | ExWindowStyles.TopMost | ExWindowStyles.ToolWindow,
            className,
            wndTitle,
            WindowStyles.Popup,
            30, 30,
            400, 400,
            default, default,
            hInstance,
            default
        );

        if(hwnd is 0 or -1) {
            throw new Win32Exception();
        }

        // Write the HWND of the created window - the server should now
        // await WndProc messages
        client.Write(hwnd);

        ready = true;
    }

    static nint WndProc(nint hwnd, uint msg, nuint wParam, nint lParam)
    {
        if (!ready) {
            // Not ready for IPC yet - resort back to the default WndProc.
            return User32.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        // Forward the message to the controller
        client!.Write(hwnd);
        client!.Write(msg);
        client!.Write(wParam);
        client!.Write(lParam);

        // ...and await a response.
        var response = (PipeResponse)client!.Read<byte>();

        if(response.HasFlag(PipeResponse.HideFromCapture)) {
            User32.SetWindowDisplayAffinity(hwnd, 0x00000011);
        } else if (response.HasFlag(PipeResponse.ShowOnCapture)) {
            User32.SetWindowDisplayAffinity(hwnd, 0x00000000);
        }

        nint hResult = 0;
        if (response.HasFlag(PipeResponse.Handled))
            hResult = client!.Read<nint>();

        if (response.HasFlag(PipeResponse.ChangeCursor)) {
            var cursor = client!.Read<ushort>();
            User32.SetCursor(User32.LoadCursor(default, cursor));
        }

        if (!response.HasFlag(PipeResponse.Handled)) {
            return User32.DefWindowProc(hwnd, msg, wParam, lParam);
        } else {
            return hResult;
        }
    }
}