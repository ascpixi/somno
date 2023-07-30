using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.PortalAgent
{
    internal static unsafe class KDMapper
    {
        [DllImport("KDMapper", EntryPoint = "kdmapper_main")]
        public extern static bool MapDriver(
            bool passAllocationPtr,
            void* drvImage
        );
    }
}
