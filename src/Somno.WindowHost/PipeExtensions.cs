using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno.WindowHost
{
    internal static class PipeExtensions
    {
        public static unsafe T Read<T>(this PipeStream self) where T : unmanaged
        {
            byte* buffer = stackalloc byte[sizeof(T)];
            self.Read(new Span<byte>(buffer, sizeof(T)));
            return *(T*)buffer;
        }

        public static unsafe void Write<T>(this PipeStream self, T value) where T : unmanaged
        {
            byte* buffer = (byte*)&value;
            self.Write(new ReadOnlySpan<byte>(buffer, sizeof(T)));
        }
    }
}
