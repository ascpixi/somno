using System;
using System.Collections.Generic;
using System.Text;

namespace Somno.Portal
{
    internal unsafe struct PortalConfiguration
    {
        public nuint RemoteExecuteMemorySize;
        public void* RemoteExecuteMemory;
        public nuint SharedMemorySize;
        public void* RemoteSharedMemory;
    }
}
