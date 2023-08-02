using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vortice;

namespace Somno.LanguageExtensions
{
    internal static class PipeExtensions
    {
        public static unsafe T Read<T>(this PipeStream self) where T : unmanaged
        {
            byte* buffer = stackalloc byte[sizeof(T)];
            self.Read(new Span<byte>(buffer, sizeof(T)));
            return *(T*)buffer;
        }

        public static async Task<T> ReadAsync<T>(this PipeStream self, CancellationToken token = default) where T : unmanaged
        {
            byte[] buffer;
            unsafe { buffer = new byte[sizeof(T)]; }

            await self.ReadAsync(buffer, token);

            unsafe {
                fixed (byte* bptr = buffer)
                    return *(T*)bptr;
            }
        }

        public static unsafe void Write<T>(this PipeStream self, T value) where T : unmanaged
        {
            byte* buffer = (byte*)&value;
            self.Write(new ReadOnlySpan<byte>(buffer, sizeof(T)));
        }

        public static async Task WriteAsync<T>(this PipeStream self, T value, CancellationToken token = default) where T : unmanaged
        {
            byte[] buffer;
            unsafe {
                buffer = new byte[sizeof(T)];
                byte* bytes = (byte*)&value;
                new Span<byte>(bytes, sizeof(T)).CopyTo(new Span<byte>(buffer, 0, sizeof(T)));
            }

            await self.WriteAsync(buffer, token);
        }
    }
}
