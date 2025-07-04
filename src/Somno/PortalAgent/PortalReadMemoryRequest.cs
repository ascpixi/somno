﻿using System.Runtime.InteropServices;

namespace Somno.PortalAgent;

[StructLayout(LayoutKind.Explicit, Size = 32)]
internal unsafe struct PortalReadMemoryRequest
{
    [FieldOffset(0)]  public readonly byte Type = 0;
    [FieldOffset(1)]  public ulong TargetPID;
    [FieldOffset(9)]  public ulong TargetAddress;
    [FieldOffset(17)] public void* BufferAddress;
    [FieldOffset(25)] public byte Size;

    public PortalReadMemoryRequest()
    {
    }
}
