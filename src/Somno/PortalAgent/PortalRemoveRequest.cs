using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.PortalAgent
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    internal unsafe struct PortalRemoveRequest
    {
        [FieldOffset(0)] public readonly byte Type = 2;
        [FieldOffset(1)] public void* OutputAddress;

        public PortalRemoveRequest()
        {
        }
    }
}
