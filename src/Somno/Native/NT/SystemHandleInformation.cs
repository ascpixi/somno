using System.Runtime.InteropServices;

namespace Somno.Native.NT;

/// <summary>
/// The SYSTEM_HANDLE_INFORMATION structure is what a successful call to ZwQuerySystemInformation or NtQuerySystemInformation produces in its output buffer when given the information class SystemHandleInformation (0x10).
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal unsafe struct SystemHandleInformation
{
    [FieldOffset(0x00)] public uint NumberOfHandles;
    [FieldOffset(0x08)] SystemHandleTableEntryInfo _Handles;

    public SystemHandleTableEntryInfo* Handles {
        get {
            fixed (SystemHandleInformation* self = &this) {
                return &self->_Handles;
            }
        }
    }
}
