using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Somno.Portal.Native.Structures.AdvAPI32
{
    internal struct TokenPrivileges
    {
        public int PrivilegeCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] // ANYSIZE_ARRAY
        public LUIDAndAttributes[] Privileges;
    }
}
