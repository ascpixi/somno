using System;
using System.Diagnostics;

namespace Somno.Evasion;

internal static class SafetyCheck
{
    static bool ProcessIsRunning(string name)
    {
        return Process.GetProcessesByName(name).Length != 0;
    }

    public static void CheckForIncompatibleSoftware()
    {
        bool unsafeToRun = false;

        if (ProcessIsRunning("EasyAntiCheat")) {
            Terminal.LogError("EasyAntiCheat is currently running on your system.");
            unsafeToRun = true;
        }

        if(ProcessIsRunning("vgtray")) {
            Terminal.LogError("Vanguard is currently running on your system.");
            unsafeToRun = true;
        }

        if(ProcessIsRunning("BEService") || ProcessIsRunning("BEService_fn")) {
            Terminal.LogError("BattlEye is currently running on your system.");
            unsafeToRun = true;
        }

        if(unsafeToRun) {
            Terminal.LogError("Please restart your system, and avoid running any software");
            Terminal.LogError("that depends on the services listed above.");
            Environment.Exit(1);
        }
    }
}
