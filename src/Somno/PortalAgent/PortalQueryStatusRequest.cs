using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.PortalAgent
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    internal unsafe struct PortalQueryStatusRequest
    {
        [FieldOffset(0)] public readonly byte Type;
        [FieldOffset(1)] public void* OutputAddress;

        public PortalQueryStatusRequest()
        {
            Type = 1;
        }
    }
}
