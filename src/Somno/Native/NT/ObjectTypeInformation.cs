using System.Runtime.InteropServices;

namespace Somno.Native.NT;

/// <summary>
/// The OBJECT_TYPE_INFORMATION structure (formally _OBJECT_TYPE_INFORMATION) is what a successful call to ZwQueryObject or NtQueryObject produces at the start of its output buffer when given the information class ObjectTypeInformation (2). The information so obtained is about the type of whatever object is referenced by the Handle argument.
/// </summary>
// https://www.geoffchappell.com/studies/windows/km/ntoskrnl/inc/api/ntobapi/object_type_information.htm
[StructLayout(LayoutKind.Explicit, Size = 0x68)]
internal struct ObjectTypeInformation
{
    [FieldOffset(0x00)] public UnicodeString TypeName;
}
