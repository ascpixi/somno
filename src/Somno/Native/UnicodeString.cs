using System.Runtime.InteropServices;

namespace Somno.Native
{
    /// <summary>
    /// The UNICODE_STRING structure keeps the address and size of a Unicode string, presumably to save on passing them as separate arguments for subsequent work with the string and to save on repeated re-reading of the whole string to rediscover its size.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct UnicodeString
    {
        [FieldOffset(0x00)] public ushort Length;
        [FieldOffset(0x02)] public ushort MaximumLength;
        [FieldOffset(0x08)] public char* Buffer;

        public UnicodeString(string s)
        {
            Length = (ushort)(s.Length * 2);
            MaximumLength = (ushort)(Length + 2);
            Buffer = (char*)Marshal.StringToHGlobalUni(s);
        }

        public override string ToString()
        {
            return Marshal.PtrToStringUni((nint)Buffer, Length / sizeof(char));
        }
    }
}
