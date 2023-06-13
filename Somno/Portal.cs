using Lunar;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;


namespace Somno
{
    internal enum PortalCommand : byte
    {
        GetHandlePID = 0x5B,
        WriteMemory = 0x5C,
        ReadMemory = 0x5D,
        Close = 0x5E
    }

    /// <summary>
    /// Handles communication with the Somno.Portal injected DLL.
    /// </summary>
    [SupportedOSPlatform("Windows")]
    internal static class Portal
    {
        static NamedPipeServerStream? pipe;
        static LibraryMapper? dllInjector;

        static void ValidatePipe([NotNull] NamedPipeServerStream? pipe)
        {
            if (pipe == null) {
                throw new InvalidOperationException("The portal named pipe hasn't been intialized.");
            }
        }

        static void WriteToPipe(byte value) => pipe!.WriteByte(value);
        static void WriteToPipe(byte[] buffer, long value)
        {
            BitConverter.TryWriteBytes(buffer, value);
            pipe!.Write(buffer, 0, 8);
        }

        /// <summary>
        /// Gets the process ID that a process handle of the injected process relates to.
        /// </summary>
        /// <param name="processHandle">The process handle to inspect.</param>
        /// <returns>The process ID of the handle.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the portal named pipe has not yet been initialized.</exception>
        public static ulong GetProcessHandlePID(long processHandle)
        {
            ValidatePipe(pipe);

            byte[] buffer = new byte[8];

            WriteToPipe((byte)PortalCommand.GetHandlePID);
            WriteToPipe(buffer, processHandle);

            pipe.Flush();
            pipe.WaitForPipeDrain();

            pipe.ReadExactly(buffer, 0, 8);
            return BitConverter.ToUInt64(buffer);
        }

        /// <summary>
        /// Reads a byte from the memory of a process, using a handle
        /// belonging to the process the portal DLL is attached to.
        /// </summary>
        /// <param name="processHandle">The process handle, belonging to the attached process, to use.</param>
        /// <param name="target">The address to read the value of.</param>
        /// <returns>The return value of the <c>WriteProcessMemory</c> function.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the portal named pipe has not yet been initialized.</exception>
        public static ulong ReadMemory(long processHandle, nint target)
        {
            ValidatePipe(pipe);

            byte[] buffer = new byte[8];

            WriteToPipe((byte)PortalCommand.ReadMemory);
            WriteToPipe(buffer, processHandle);
            WriteToPipe(buffer, target);

            pipe.Flush();
            pipe.WaitForPipeDrain();

            pipe.ReadExactly(buffer, 0, 8);
            return BitConverter.ToUInt64(buffer);
        }

        /// <summary>
        /// Writes a byte to a memory address of a process, using a
        /// handle belonging to the process the portal DLL is attached to.
        /// </summary>
        /// <param name="processHandle">The process handle, belonging to the attached process, to use.</param>
        /// <param name="target">The address to write to.</param>
        /// <param name="value">The byte to write to the given address.</param>
        /// <returns>The return value of the <c>ReadProcessMemory</c> function.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the portal named pipe has not yet been initialized.</exception>
        public static ulong WriteMemory(long processHandle, nint target, byte value)
        {
            ValidatePipe(pipe);

            byte[] buffer = new byte[8];

            WriteToPipe((byte)PortalCommand.WriteMemory);
            WriteToPipe(buffer, processHandle);
            WriteToPipe(buffer, target);
            WriteToPipe(buffer, value);

            pipe.Flush();
            pipe.WaitForPipeDrain();

            pipe.ReadExactly(buffer, 0, 8);
            return BitConverter.ToUInt64(buffer);
        }

        /// <summary>
        /// Injects the portal agent to the given process ID. 
        /// </summary>
        /// <param name="processId">The process ID to inject to.</param>
        public static void Inject(int processId)
        {
            Terminal.LogInfo("Starting portal agent named pipe...");

            // Start the named pipe server stream; the DLL will try to
            // connect to it as soon as it starts.
            pipe = new NamedPipeServerStream(
                "somno_game_replay_capture",
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte
            );

            Terminal.LogInfo("Loading portal agent DLL...");

            // The pipe server is running; we can now inject the DLL.
            dllInjector = new LibraryMapper(
                Process.GetProcessById(processId),
                @".\Resources\Portal\Somno.Portal.dll",
                MappingFlags.None
            );

            Terminal.LogInfo($"Injecting to PID {processId}.");

            dllInjector.MapLibrary();

            Terminal.LogInfo("Injection complete. Awaiting named pipe connection.");

            // Wait for the DLL client to connect to the pipe.
            pipe.WaitForConnection();

            Terminal.LogInfo($"PID {processId} connected; IPC ready.");
        }
    
        public static void Close()
        {
            if (pipe != null) {
                Terminal.LogInfo("Sending termination message to DLL...");
                pipe.WriteByte((byte)PortalCommand.Close);
                pipe.Flush();
                pipe.WaitForPipeDrain();

                Terminal.LogInfo("Closing pipe...");
                pipe.Dispose();
            }

            if (dllInjector != null) {
                Terminal.LogInfo("Unmapping library...");
                dllInjector.UnmapLibrary();
            }
        }
    }
}
