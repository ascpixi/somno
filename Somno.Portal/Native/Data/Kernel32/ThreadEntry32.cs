using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Portal.Native.Data.Kernel32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ThreadEntry32
    {
        public uint Size;
        [Obsolete] public uint Usage;
        public uint ThreadID;
        public uint OwnerProcessID;
        public int BasePri;
        [Obsolete] public int DeltaPri;
        [Obsolete] public uint Flags;
    }
}
