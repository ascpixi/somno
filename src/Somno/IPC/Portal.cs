using Lunar;
using Somno.Native;
using Somno.Packager;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Somno.IPC
{
    /// <summary>
    /// Controls the IPC portal agent.
    /// </summary>
    internal unsafe class Portal : IDisposable
    {
        MemoryProcessHandle procHandle;
        LibraryMapper? mapper;
        PortalIPCRegion* ipc;
        Process vectorProcess;

        const string IPCFileMapName = @"Global\AqW5p2FhqX";
        const ulong IPCHandshakeSignature = 0x488D0411EBFE90C3;
        const ulong IPCHandshakeResponse  = 0xDEADBEEFDEADBEEF;

        static Process AwaitProcess(string target)
        {
            bool waitMessageWritten = false;
            while (true) {
                var proc = Process.GetProcessesByName(target);
                if (proc.Length == 0) {
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
            Process targetProcess = AwaitProcess(target);

            // Find an external, vector process that already has a handle
            // to the target process, with virtual memory R/W permissions
            do {
                procHandle = HandleTraverse.FindMemoryHandle(targetProcess.Id);
                if (!procHandle.IsValid) {
                    Terminal.LogWarning($"Couldn't find a process handle for {target}.exe. Retrying in 10 seconds.");
                    Thread.Sleep(10 * 1000);
                }
            } while (!procHandle.IsValid);

            vectorProcess = Process.GetProcessById(procHandle.OwnerPID);
            Terminal.LogInfo($"Using process '{vectorProcess.ProcessName}' (PID {vectorProcess.Id}) as the vector.");

            // Unpack the DLL into memory
            var key = GeneratePackageKey();
            byte[] dll = Packaging.OpenPackedFile(
                File.ReadAllBytes("./resource/data001.bin"),
                key
            );

            Array.Clear(key); // make sure the key remains in memory for the least amount of time possible

            mapper = new LibraryMapper(vectorProcess, dll, MappingFlags.DiscardHeaders);

            // Mapper ready - now initialize the IPC memory region.
            var fileMapHandle = Kernel32.CreateFileMapping(
                Kernel32.InvalidHandleValue,
                0,
                Kernel32.PageReadWrite | Kernel32.SecCommit | Kernel32.SecNoCache,
                0, (uint)sizeof(PortalIPCRegion),
                IPCFileMapName
            );

            ipc = (PortalIPCRegion*)Kernel32.MapViewOfFile(
                fileMapHandle,
                Kernel32.FileMapAllAccess,
                0, 0,
                (uint)sizeof(PortalIPCRegion)
            );

            ipc->RequestID = PortalIPCRequest.Handshake;
            ipc->WritePayload(IPCHandshakeSignature);
            ipc->PendingRequest = true;

            // IPC region ready, now inject the DLL and wait for an answer.
            mapper.MapLibrary();

            while(ipc->PendingRequest) {
                Thread.Sleep(0);
            }

            // As we have mapped the view of file, we can close the handle.
            Kernel32.CloseHandle(fileMapHandle);

            var verification = ipc->ReadPayload64();
            if (verification == IPCHandshakeResponse) {
                ipc->PendingRequest = false;
                Terminal.LogInfo("Portal connection established!");
                GC.Collect();
                return;
            }

            ipc = null;

            Terminal.LogError("Received invalid verification value from the portal agent.");
            Terminal.LogError($"   Value: 0x{verification:X2}");
            throw new InvalidDataException();
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
            if(vectorProcess.HasExited) {
                throw new Exception("The vector process is not running.");
            }

            if(address == 0) {
                throw new InvalidOperationException("Attempted to read from a null memory location.");
            }

            int size = sizeof(T);
            if (size > 32) {
                throw new InvalidOperationException($"Tried to read {size} at once; cannot read more than 32.");
            }

            int counter = 0;
            ipc->RequestID = PortalIPCRequest.ReadProcessMemory;
            ipc->WritePayload((ulong)procHandle.Handle, ref counter);
            ipc->WritePayload((ulong)address, ref counter);
            ipc->WritePayload((uint)size, ref counter);
            ipc->PendingRequest = true;

            while (ipc->PendingRequest) {
                Thread.Sleep(0);
            }

            return *(T*)ipc->Payload;
        }

        /// <summary>
        /// Sends an IPC termination request, and un-maps the portal agent
        /// from the vector process.
        /// </summary>
        public void Close()
        {
            if(ipc != null) {
                ipc->RequestID = PortalIPCRequest.Terminate;
                ipc->PendingRequest = true;

                Terminal.LogInfo("Waiting for the portal agent to terminate...");
                while(ipc->PendingRequest && !vectorProcess.HasExited) {
                    Thread.Sleep(0);
                }

                Terminal.LogInfo("The portal agent has acknowledged the termination request.");
                ipc = null;
            }

            mapper?.UnmapLibrary();
            mapper = null;
        }

        public override string ToString()
        {
            if(ipc == null) {
                return "(closed portal agent)";
            } else {
                return $"(portal agent: P: {ipc->PendingRequest}, ID: 0x{(byte)ipc->RequestID:X2}, d64: {ipc->ReadPayload64():X2})";
            }
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
                    b[j + i * 3] = (byte)(i == 0 ? r1 : r2).Next(0, 255);
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
