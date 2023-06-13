using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Somno.Portal.Native
{
    /// <summary>
    /// Represents a locally unique identifier (LUID).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID
    {
        public uint LowPart;
        public uint HighPart;
    }
}
