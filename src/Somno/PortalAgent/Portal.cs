using Somno.Native;
using Somno.Packager;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Somno.PortalAgent
{
    /// <summary>
    /// Controls the IPC portal agent.
    /// </summary>
    internal unsafe class Portal : IDisposable
    {
        readonly delegate* unmanaged[Stdcall]<ulong, ulong, ulong, void> communicate;
        readonly Process targetProcess;
        readonly nint libraryHandle;

        const ulong IPCSignature = 0xACE77777DEADDEAD;
        const string HookedFunctionLibrary = "win32u.dll";
        const string HookedFunctionName = "NtDxgkDisplayPortOperation";

        static Process AwaitProcess(string target, out bool wasAlreadyRunning)
        {
            wasAlreadyRunning = true;

            bool waitMessageWritten = false;
            while (true) {
                var proc = Process.GetProcessesByName(target);
                if (proc.Length == 0) {
                    wasAlreadyRunning = false;

                    if (!waitMessageWritten) {
                        Terminal.LogInfo($"Waiting for '{target}.exe'...");
                        waitMessageWritten = true;
                    }

                    Thread.Sleep(250);
                    continue;
                }

                return proc[0];
            }
        }

        /// <summary>
        /// Establishes the IPC portal connection. This will inject the
        /// portal DLL into a vector process that already has an open
        /// virtual memory R/W handle to the target process.
        /// </summary>
        /// <param name="target">The target process name.</param>
        /// <exception cref="InvalidDataException">Thrown when a part of received IPC data is invalid. </exception>
        public Portal(string target)
        {
            targetProcess = AwaitProcess(target, out var wasAlreadyRunning);
            if (wasAlreadyRunning) {
                Terminal.LogWarning("The given process was already running when Somno was started.");
                Terminal.LogWarning("It's recommended to run Somno first, before the target process.");
                Terminal.LogWarning("Press [ENTER] to load the Somno Portal Agent.");
            }
            else {
                Terminal.LogInfo("Process detected. Press [ENTER] to load the Somno Portal Agent.");
                Terminal.LogInfo("Please note that it is recommended to wait ~5 minutes before loading SPA.");
            }

            while (Console.ReadKey().Key != ConsoleKey.Enter) { }

            libraryHandle = Kernel32.LoadLibrary(HookedFunctionLibrary);
            communicate =
                (delegate* unmanaged[Stdcall]<ulong, ulong, ulong, void>)
                Kernel32.GetProcAddress(libraryHandle, HookedFunctionName);

            // Check if the hook is already set-up
            var response = stackalloc byte[1];
            var request = new PortalQueryStatusRequest() {
                OutputAddress = response
            };

            communicate(IPCSignature, (ulong)Environment.ProcessId, (ulong)&request);

            if (response[0] == 1) {
                // Hook is running - no need to load in the driver
                Terminal.LogInfo("The portal agent is already running.");
                return;
            }

            // Unpack the driver into memory
            var key = GeneratePackageKey();
            byte[] drv = Packaging.OpenPackedFile(File.ReadAllBytes("./resource/data001.bin"), key);
            Array.Clear(key);

            fixed(byte* drvData = drv) {
                if(KDMapper.MapDriver(true, drvData)) {
                    Terminal.LogInfo("The portal agent has been successfully loaded.");
                } else {
                    Terminal.LogError("Could not load the portal agent!");
                    throw new Exception("Cannot load the portal agent.");
                }
            }
        }

        /// <summary>
        /// Reads the given structure from the memory of the process
        /// the portal is directed to.
        /// </summary>
        /// <param name="address">The virtual address to read from.</param>
        /// <returns>The value at <paramref name="address"/> in the address space of the process the portal is directed to.</returns>
        /// <exception cref="InvalidOperationException">Thrown when a type is given, of which the size exceeds the IPC shared memory region.</exception>
        public T ReadProcessMemory<T>(ulong address) where T : unmanaged
        {
            if(sizeof(T) > byte.MaxValue) {
                throw new InvalidOperationException("The provided structure is too large.");
            }

            T response = default;
            var request = new PortalReadMemoryRequest() {
                TargetAddress = address,
                BufferAddress = &response,
                Size = (byte)sizeof(T),
                TargetPID = (ulong)targetProcess.Id
            };

            communicate(IPCSignature, (ulong)Environment.ProcessId, (ulong)&request);
            return response;
        }

        /// <summary>
        /// Sends an IPC termination request, and un-maps the portal agent
        /// from the vector process.
        /// </summary>
        public void Close()
        {
            // TODO: for some reason, we can write to a R/X region ONLY ONCE?????
            //       if we do it the second time, we get a bugcheck (a.k.a. a BSoD)
            //       for now, this method is a no-op.
        }

        public void Dispose() => Close();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte[] GeneratePackageKey()
        {
            Random r1, r2, r3, r4, r5;

            r1 = new Random(-2137577458);
            r2 = new Random(-2146987921);
            r3 = new Random(-2127003278);
            r4 = new Random(-2146856769);
            r5 = new Random(-2118774814);

            Span<byte> b = stackalloc byte[8];

            for (int i = 0; i < 2; i++) {
                for (int j = 0; j < 3; j++) {
                    b[j + (i * 3)] = (byte)(i == 0 ? r1 : r2).Next(0, 255);
                }
            }

            b[6] = (byte)r3.Next(0, 255);
            b[7] = (byte)r3.Next(0, 255);

            byte[] ba = b.ToArray();
            Array.Resize(ref ba, 16);

            ba[8] = (byte)r3.Next(0, 255);
            for (int j = 9; j < 12; j++) {
                ba[j] = (byte)r4.Next(0, 255);
            }

            ba[12] = (byte)r5.Next(0, 255);
            ba[13] = (byte)r5.Next(0, 255);
            ba[14] = (byte)r5.Next(0, 255);
            ba[15] = 0x61;

            return ba;
        }
    }
}
