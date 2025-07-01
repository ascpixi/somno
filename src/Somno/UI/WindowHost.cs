using Somno.Evasion;
using Somno.LanguageExtensions;
using Somno.Native.WinUSER;
using Somno.Packager;
using Somno.WindowHost;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vortice;

namespace Somno.UI
{
    /// <summary>
    /// Provides functionality for operating the window host, which provides
    /// window creation capabilities.
    /// </summary>
    internal class WindowHost : IDisposable
    {
        bool changeCursor;
        ushort cursorResource;
        bool hiddenFromScreenCapture;
        PipeResponse affinityFlag;

        const string VigenerePipeNameKey = "VBGNSJVAUQZMZPOAIJHU";

        /// <summary>
        /// The handle of the host window.
        /// </summary>
        public readonly nint WindowHandle;

        readonly string executableDirectory;
        readonly Process hostProcess;
        readonly NamedPipeServerStream pipeServer;
        readonly HostWndProc callbackWndProc;
        readonly CancellationTokenSource cancelSrc;
        readonly Task? listenTask;
        bool running;

        public delegate (nint, bool) HostWndProc(nint hwnd, uint msg, nuint wParam, nint lParam);

        /// <summary>
        /// Creates and starts a new window host instance.
        /// </summary>
        /// <param name="wndProc">The WndProc callback to execute on each window message.</param>
        public WindowHost(HostWndProc wndProc)
        {
            callbackWndProc = wndProc;
            cancelSrc = new CancellationTokenSource();

            // Create a random temporary folder in Program Files
            executableDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                RandomProvider.GenerateWord(Random.Shared.Next(5, 8))
            );

            var processName = RandomProvider.GenerateWord(Random.Shared.Next(6, 12));

            var executablePath = Path.Combine(
                executableDirectory,
                processName + ".exe"
            );

            // Extract the window host executable
            var key = GeneratePackageKey();
            byte[] bytes = Packaging.OpenPackedFile(File.ReadAllBytes("./resource/data002.bin"), key);
            Array.Clear(key);

            // Replace all references to "Somno.WindowHost"
            BytePatch.Patch(bytes, "Somno.WindowHost.exe", processName + ".exe");
            BytePatch.Patch(bytes, "Somno.WindowHost", processName);

            Directory.CreateDirectory(executableDirectory);
            File.SetAttributes(
                executableDirectory,
                FileAttributes.NoScrubData | FileAttributes.Directory | FileAttributes.NotContentIndexed
            );

            File.WriteAllBytes(executablePath, bytes);
            Array.Clear(bytes);

            FileOrigin.RandomizeFileSystemTime(
                executableDirectory,
                executablePath
            );

            // Set up the named pipe server
            var pipeName = RandomProvider.GenerateString(Random.Shared.Next(11, 16));
            var encipheredPipeName = Vigenere.Encipher(pipeName, VigenerePipeNameKey);
            pipeServer = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte
            );

            // Run the window host executable
            var psi = new ProcessStartInfo() {
                FileName = "cmd",
                Arguments = $"/c start \"\" \"{executablePath}\" \"{encipheredPipeName}\"",
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            Process.Start(psi);

            pipeServer.WaitForConnection();
            WindowHandle = pipeServer.Read<nint>();

            do {
                hostProcess = Process.GetProcessesByName(processName)?.FirstOrDefault()!;
                Thread.Sleep(0);
            } while (hostProcess == null);

            running = true;
            listenTask = Task.Run(ListenTask);
        }

        async Task<T> ReadFromPipe<T>() where T : unmanaged
        {
            var value = await pipeServer.ReadAsync<T>(cancelSrc.Token);

            if(!running)
                throw new OperationCanceledException();

            return value;
        }

        async Task WriteToPipe<T>(T value) where T : unmanaged
        {
            await pipeServer.WriteAsync(value, cancelSrc.Token);

            if (!running)
                throw new OperationCanceledException();
        }

        async Task ListenTask()
        {
            try {
                while (running) {
                    var hwnd = await ReadFromPipe<nint>();
                    var msg = await ReadFromPipe<uint>();
                    var wParam = await ReadFromPipe<nuint>();
                    var lParam = await ReadFromPipe<nint>();

                    // Full WndProc response request received, respond back
                    var (hResult, handled) = callbackWndProc(hwnd, msg, wParam, lParam);

                    PipeResponse response = default;
                    response |= affinityFlag;
                    if (handled) response |= PipeResponse.Handled;
                    if (changeCursor) response |= PipeResponse.ChangeCursor;

                    await WriteToPipe((byte)response);
                    if (handled) await WriteToPipe(hResult);
                    if (changeCursor) await WriteToPipe(cursorResource);

                    changeCursor = false;
                    affinityFlag = 0;
                }
            } catch (OperationCanceledException) {
                // operation cancelled, thread will terminate...
                return;
            }
        }

        public void SetCursor(ushort resource)
        {
            changeCursor = true;
            cursorResource = resource;
        }

        public void ShowOnScreenCapture(bool force = false)
        {
            if(hiddenFromScreenCapture || force) {
                affinityFlag = PipeResponse.ShowOnCapture;
                hiddenFromScreenCapture = false;
            }
        }

        public void HideFromScreenCapture(bool force = false)
        {
            if (!hiddenFromScreenCapture || force) {
                affinityFlag = PipeResponse.HideFromCapture;
                hiddenFromScreenCapture = true;
            }
        }

        public void Dispose()
        {
            if(!running)
                return;

            cancelSrc.Cancel();
            running = false;

            pipeServer.Flush();
            pipeServer.Dispose();

            var procName = hostProcess.ProcessName;
            hostProcess.Kill();
            hostProcess.Dispose();

            while(Process.GetProcessesByName(procName).Any()) {
                Thread.Sleep(0);
            }

            Directory.Delete(executableDirectory, true);

            cancelSrc.Dispose();

            if(listenTask != null) {
                listenTask.Wait();
                listenTask.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte[] GeneratePackageKey()
        {
            Random r1, r2, r3, r4, r5;

            r1 = new(-2146063673);
            r2 = new(-2146977981);
            r3 = new(-2144427894);
            r4 = new(-2145806024);
            r5 = new(-2143162204);

            byte[] o = new byte[16];

            for (int i = 0; i < 3; i++)
                o[i + 0] = (byte)r1.Next(66, 154);
            for (int i = 0; i < 3; i++)
                o[i + 3] = (byte)r2.Next(20, 154);
            for (int i = 0; i < 3; i++)
                o[i + 6] = (byte)r3.Next(23, 177);
            for (int i = 0; i < 3; i++)
                o[i + 9] = (byte)r4.Next(28, 173);
            for (int i = 0; i < 4; i++)
                o[i + 12] = (byte)r5.Next(42, 110);

            return o;
        }
    }
}
