using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Portal.Native.Data.NTDll
{
    // https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ps/psquery/class.htm
    internal enum ThreadInfoClass : uint
    {
        ThreadQuerySetWin32StartAddress = 0x09
    }
}
