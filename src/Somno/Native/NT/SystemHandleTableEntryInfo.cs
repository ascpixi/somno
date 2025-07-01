using System.Runtime.InteropServices;

namespace Somno.Native.NT;

[StructLayout(LayoutKind.Explicit)]
internal unsafe struct SystemHandleTableEntryInfo
{
    [FieldOffset(0x00)] public ushort UniqueProcessId;
    [FieldOffset(0x02)] public ushort CreatorBackTraceIndex;
    [FieldOffset(0x04)] public byte ObjectTypeIndex;
    [FieldOffset(0x05)] public byte HandleAttributes;
    [FieldOffset(0x06)] public ushort HandleValue;
    [FieldOffset(0x08)] public void* Object;
    [FieldOffset(0x10)] public uint GrantedAccess;
}
