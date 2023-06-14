using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Portal.Native.Data.Kernel32
{
    internal enum FileMapAccess : uint
    {
        AllAccess = 0x000F0000 | 0x0001 | 0x0002 | 0x0004 | 0x0008 | 0x0010,
        Read = 0x0004,
        Write = 0x0002
    }
}
