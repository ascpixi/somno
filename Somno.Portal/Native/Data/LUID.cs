using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Somno.Portal.Native.Data
{
    /// <summary>
    /// Describes a local identifier for an adapter.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID
    {
        /// <summary>
        /// Specifies a DWORD that contains the unsigned lower numbers of the id.
        /// </summary>
        public uint LowPart;

        /// <summary>
        /// Specifies a LONG that contains the signed high numbers of the id.
        /// </summary>
        public uint HighPart;
    }
}
