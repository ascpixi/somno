using System.Runtime.InteropServices;

namespace Somno.PortalAgent;

internal static unsafe class KDMapper
{
    [DllImport("KDMapper", EntryPoint = "kdmapper_main")]
    public extern static bool MapDriver(
        bool passAllocationPtr,
        void* drvImage
    );
}
