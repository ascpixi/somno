using Somno.Portal.Native;
using System;
using System.Collections.Generic;
using System.Text;

namespace Somno.Portal
{
    internal unsafe struct PortalOrder
    {
        public ulong Exec;
        public uint Order;
        public NTStatus Status;
        public IntPtr ProcessHandle;
        public nuint Size;
        public nuint* BytesReadOrWritten;

        public PortalOrder()
        {
            Exec = 1;
            Status = (NTStatus)0xFFFFFFFF;
        }
    }
}
