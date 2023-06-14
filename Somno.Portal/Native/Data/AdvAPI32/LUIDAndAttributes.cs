using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Somno.Portal.Native.Data.AdvAPI32
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct LUIDAndAttributes
    {
        public LUID Luid;
        public uint Attributes;
    }
}
