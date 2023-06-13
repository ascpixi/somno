using Somno.Portal.Native;
using System;
using System.Collections.Generic;
using System.Text;

namespace Somno.Portal
{
    internal unsafe struct UnusedXMem
    {
        public MemoryBasicInformation RegionInfo;
        public void* Start;
        public nuint Size;
    }
}
