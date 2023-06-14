using Somno.Portal.Native;
using Somno.Portal.Native.Data;
using Somno.Portal.Windows;
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
        public Handle ProcessHandle;
        public nuint Size;
        public nuint* BytesReadOrWritten;

        public PortalOrder()
        {
            Exec = 1;
            Status = (NTStatus)0xFFFFFFFF;
        }
    }
}
