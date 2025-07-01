using Somno.Native;
using System;
using System.Runtime.InteropServices;

namespace Somno;

/// <summary>
/// Facilitates safe process querying.
/// </summary>
internal static class ProcessQuery
{
    public static string? GetPathByPID(int pid)
    {
        nint hSnapshot = Kernel32.CreateToolhelp32Snapshot(
            TH32SnapshotFlags.Module | TH32SnapshotFlags.Module32,
            (uint)pid
        );

        if (hSnapshot == 0)
            return null;

        try {
            var me32 = new ModuleEntry32();
            me32.dwSize = (uint)Marshal.SizeOf<ModuleEntry32>();

            if (!Kernel32.Module32First(hSnapshot, ref me32))
                return null;

            return me32.szExePath;
        }
        finally {
            Kernel32.CloseHandle(hSnapshot);
        }
    }

    public static bool TryGetPIDByName(string name, out int pid)
    {
        nint hSnapshot = Kernel32.CreateToolhelp32Snapshot(
            TH32SnapshotFlags.Process, 0
        );

        if (hSnapshot == 0) {
            pid = -1;
            return false;
        }

        try {
            var pe32 = new ProcessEntry32();
            pe32.dwSize = (uint)Marshal.SizeOf<ProcessEntry32>();

            if (!Kernel32.Process32First(hSnapshot, ref pe32)) {
                pid = -1;
                return false;
            }

            do {
                if (string.Equals(pe32.szExeFile, name, StringComparison.OrdinalIgnoreCase)) {
                    pid = (int)pe32.th32ProcessID;
                    return true;
                }
            } while (Kernel32.Process32Next(hSnapshot, ref pe32));

            pid = -1;
            return false;
        }
        finally {
            Kernel32.CloseHandle(hSnapshot);
        }
    }

    public static bool TryGetModuleAddress(int pid, string moduleName, out nint moduleAddress)
    {
        nint hSnapshot = Kernel32.CreateToolhelp32Snapshot(
            TH32SnapshotFlags.Module | TH32SnapshotFlags.Module32,
            (uint)pid
        );

        if (hSnapshot == 0) {
            moduleAddress = 0;
            return false;
        }

        try {
            var me32 = new ModuleEntry32();
            me32.dwSize = (uint)Marshal.SizeOf<ModuleEntry32>();

            if (!Kernel32.Module32First(hSnapshot, ref me32)) {
                moduleAddress = 0;
                return false;
            }

            do {
                if (string.Equals(me32.szModule, moduleName, StringComparison.OrdinalIgnoreCase)) {
                    moduleAddress = me32.modBaseAddr;
                    return true;
                }
            } while (Kernel32.Module32Next(hSnapshot, ref me32));

            moduleAddress = 0;
            return false;
        }
        finally {
            Kernel32.CloseHandle(hSnapshot);
        }
    }
}
