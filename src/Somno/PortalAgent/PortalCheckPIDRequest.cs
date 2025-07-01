using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.PortalAgent
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    internal unsafe struct PortalCheckPIDRequest
    {
        [FieldOffset(0)] public readonly byte Type;
        [FieldOffset(1)] public ulong PID;
        [FieldOffset(1 + sizeof(ulong))] public void* OutputAddress;

        public PortalCheckPIDRequest()
        {
            Type = 2;
        }
    }
}
