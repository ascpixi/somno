using System.Runtime.InteropServices;

namespace Somno.PortalAgent;

[StructLayout(LayoutKind.Explicit, Size = 32)]
internal unsafe struct PortalQueryStatusRequest
{
    [FieldOffset(0)] public readonly byte Type;
    [FieldOffset(1)] public void* OutputAddress;

    public PortalQueryStatusRequest()
    {
        Type = 1;
    }
}
