using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno.WindowHost.Native.WinUSER
{
    [Flags]
    internal enum MessageBoxFlags : uint
    {
        OK = 0x0,
        Error = 0x10,
    }
}
