using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Somno.Portal.Native
{
    internal struct TokenPrivilidges
    {
        public int PrivilegeCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] // ANYSIZE_ARRAY
        public LUIDAndAttributes[] Privileges;
    }
}
