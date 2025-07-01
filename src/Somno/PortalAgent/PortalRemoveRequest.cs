using System.Runtime.InteropServices;

namespace Somno.PortalAgent;

[StructLayout(LayoutKind.Explicit, Size = 32)]
internal unsafe struct PortalRemoveRequest
{
    [FieldOffset(0)] public readonly byte Type = 2;
    [FieldOffset(1)] public void* OutputAddress;

    public PortalRemoveRequest()
    {
    }
}
