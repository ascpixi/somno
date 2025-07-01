using System.Runtime.InteropServices;

namespace Somno.WindowHost.Native;

internal static class Kernel32
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern nint GetModuleHandle(
        [In, Optional] string? lpModuleName
    );
}
