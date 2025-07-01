using System;
using System.Runtime.InteropServices;

namespace Somno.WindowHost;

internal static class SpanExtensions
{
    public static ReadOnlySpan<byte> GetBytes<T>(this ReadOnlySpan<T> self) where T : unmanaged
        => MemoryMarshal.AsBytes(self);

    public static ReadOnlySpan<byte> GetBytes<T>(in T self) where T : unmanaged
        => new ReadOnlySpan<T>(in self).GetBytes();
}
