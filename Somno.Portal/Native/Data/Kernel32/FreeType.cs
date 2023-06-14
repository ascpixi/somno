using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Portal.Native.Data.Kernel32
{
    [Flags]
    internal enum FreeType : uint
    {
        Decommit = 0x4000,
        Release = 0x8000,
    }
}
