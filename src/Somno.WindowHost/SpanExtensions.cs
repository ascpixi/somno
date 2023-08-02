using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.WindowHost
{
    internal static class SpanExtensions
    {
        public static ReadOnlySpan<byte> GetBytes<T>(this ReadOnlySpan<T> self) where T : unmanaged
        {
            return MemoryMarshal.AsBytes<T>(self);
        }

        public static ReadOnlySpan<byte> GetBytes<T>(in T self) where T : unmanaged
        {
            return new ReadOnlySpan<T>(in self).GetBytes();
        }
    }
}
