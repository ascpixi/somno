using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Portal.Native.Data.Kernel32
{
    [Flags]
    internal enum ThreadAccessFlags : uint
    {
        Terminate = (0x0001),
        SuspendResume = (0x0002),
        GetContext = (0x0008),
        SetContext = (0x0010),
        SetInformation = (0x0020),
        QueryInformation = (0x0040),
        SetThreadToken = (0x0080),
        Impersonate = (0x0100),
        DirectImpersonation = (0x0200),
        AllAccess = ((0x000F0000) | (0x00100000) | 0xFFFF)
    }
}
