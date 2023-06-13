using Somno.Portal.Native;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Somno.Portal
{
    internal static class ProcessUtility
    {
        internal static unsafe List<uint> GetPIDs(string targetProcessName)
        {
            List<uint> pids = new();
            if (string.IsNullOrEmpty(targetProcessName))
		        return pids;

            IntPtr snap = Kernel32.CreateToolhelp32Snapshot(SnapshotFlags.Process, 0);
            
            ProcessEntry32W entry = default;
            entry.Size = (uint)Marshal.SizeOf<ProcessEntry32W>(); // is this correct...?

            if (!Kernel32.Process32FirstW(snap, ref entry))
                return pids;

            do {
                if (entry.ExeFile == targetProcessName) {
                    pids.Add(entry.ProcessID);
                }
            } while (Kernel32.Process32NextW(snap, ref entry));

            return pids;
        }
    }
}
