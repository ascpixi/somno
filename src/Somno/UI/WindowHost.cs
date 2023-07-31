using Lunar;
using Somno.Native.WinUSER;
using Somno.Packager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Somno.UI
{
    [SupportedOSPlatform("windows")]
    internal class WindowHost : IDisposable
    {
        readonly LibraryMapper mapper;
        readonly NamedPipeServerStream pipe;
        readonly WndProc wndprocCallback;
        CancellationTokenSource listenTaskCancel;
        bool running;

        public IntPtr Handle { get; private set; }

        public WindowHost(Process vectorProcess, WndProc wndProc)
        {
            wndprocCallback = wndProc;

            // Unpack the DLL into memory
            var key = GeneratePackageKey();
            byte[] dll = Packaging.OpenPackedFile(File.ReadAllBytes("./resource/data002.bin"), key);
            Array.Clear(key);

            pipe = new NamedPipeServerStream(
                @"RpRZOLoxHp",
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte
            );

            mapper = new LibraryMapper(vectorProcess, dll, MappingFlags.DiscardHeaders);

            listenTaskCancel = new();
        }

        public void Start()
        {
            mapper.MapLibrary();
            pipe.WaitForConnection();

            using var reader = new BinaryReader(pipe);
            Handle = (nint)reader.ReadInt64();

            Task.Run(ListenThreadProcedure);
            running = true;
        }

        private async Task? ListenThreadProcedure()
        {
            byte[] buffer = new byte[8];
            Memory<byte> u64 = buffer.AsMemory(0, sizeof(ulong));
            Memory<byte> u32 = buffer.AsMemory(0, sizeof(uint));

            while (running) {
                await pipe.ReadAsync(u64, listenTaskCancel.Token);
                nint hwnd = (nint)BitConverter.ToInt64(u64.Span);
                if (!running) break;

                Console.WriteLine("got hwnd");

                await pipe.ReadAsync(u32, listenTaskCancel.Token);
                uint msg = BitConverter.ToUInt32(u32.Span);
                if (!running) break;

                Console.WriteLine("got msg");

                await pipe.ReadAsync(u64, listenTaskCancel.Token);
                nuint wParam = (nuint)BitConverter.ToUInt64(u64.Span);
                if (!running) break;

                Console.WriteLine("got wparam");


                await pipe.ReadAsync(u64, listenTaskCancel.Token);
                nint lParam = (nint)BitConverter.ToInt64(u64.Span);
                if (!running) break;

                Console.WriteLine("got lparam");


                var hResult = wndprocCallback(hwnd, msg, wParam, lParam);
                if (!running) break;

                BitConverter.TryWriteBytes(u64.Span, (ulong)hResult);
                await pipe.WriteAsync(u64, listenTaskCancel.Token);
            }
        }

        public void Dispose()
        {
            if(running) {
                mapper.UnmapLibrary();
                listenTaskCancel.Cancel();
                running = false;
            }

            pipe.Dispose();
            listenTaskCancel.Dispose();
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

            for (int i = 0; i < 3; i++) o[i + 0] = (byte)r1.Next(66, 154);
            for (int i = 0; i < 3; i++) o[i + 3] = (byte)r2.Next(20, 154);
            for (int i = 0; i < 3; i++) o[i + 6] = (byte)r3.Next(23, 177);
            for (int i = 0; i < 3; i++) o[i + 9] = (byte)r4.Next(28, 173);
            for (int i = 0; i < 4; i++) o[i + 12] = (byte)r5.Next(42, 110);

            return o;
        }
    }
}
