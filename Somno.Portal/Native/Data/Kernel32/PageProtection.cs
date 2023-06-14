using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Portal.Native.Data.Kernel32
{
    [Flags]
    internal enum PageProtection : uint
    {
        NoAccess = 0x01,
        Readonly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        Guard = 0x100,
        NoCache = 0x200,
        WriteCombine = 0x400,
        FileMapAllAccess = ((0x000F0000) | 0x0001 | 0x0002 | 0x0004 | 0x0008 | 0x0010),
    }
}
