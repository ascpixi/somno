using System.Runtime.InteropServices;

namespace Somno.PortalAgent;

[StructLayout(LayoutKind.Explicit, Size = 32)]
internal unsafe struct PortalCheckPIDRequest
{
    [FieldOffset(0)] public readonly byte Type;
    [FieldOffset(1)] public ulong PID;
    [FieldOffset(1 + sizeof(ulong))] public void* OutputAddress;

    public PortalCheckPIDRequest()
    {
        Type = 2;
    }
}
