using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Portal.Native.Data.NTDll
{
    internal struct IOStatusBlock
    {
        public uint Status;
        public ulong Information;
    }
}
