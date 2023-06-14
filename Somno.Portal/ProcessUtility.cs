using Somno.Portal.Native;
using Somno.Portal.Native.Data;
using Somno.Portal.Native.Data.Kernel32;
using Somno.Portal.Native.Data.NTDll;
using Somno.Portal.Native.Data.PSApi;
using Somno.Portal.Windows;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Somno.Portal
{
    internal static class ProcessUtility
    {
        const int MAX_PATH = 260;

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

        internal static unsafe void* GetBaseAddress(Handle hProcess)
        {
            if (hProcess.IsInvalidOrNull) {
                return null;
            }

            var lphModule = stackalloc IntPtr[1024];

            if (!PSApi.EnumProcessModules(hProcess, lphModule, 1024 * (uint)sizeof(IntPtr), out uint lpcbNeeded)) {
                // Impossible to read modules (hProcess requires PROCESS_QUERY_INFORMATION | PROCESS_VM_READ)
                return null;
            }

            // Module 0 is the EXE itself, returning its address
            return (void*)lphModule[0];
        }

        unsafe static Dictionary<string, ulong> GetModulesNamesAndBaseAddresses(uint pid)
        {
            Dictionary<string, ulong> modsStartAddrs = new();

            if (pid == 0)
                return modsStartAddrs;

            var hMods = stackalloc IntPtr[1024];
            uint i;

            Handle hProcess = Kernel32.OpenProcess(
                ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VMRead,
                false,
                pid
            );

            if (hProcess.IsInvalidOrNull)
                return modsStartAddrs;

            // Get a list of all the modules in this process
            if (!PSApi.EnumProcessModules(hProcess, hMods, 1024 * (uint)sizeof(IntPtr), out uint cbNeeded)) {
                hProcess.TryClose();
                return modsStartAddrs;
            }

            // Get each module's infos
            for (i = 0; i < (cbNeeded / sizeof(IntPtr)); i++) {
                var szModName = new StringBuilder(MAX_PATH);

                // Get the full path to the module's file
                if (PSApi.GetModuleFileNameEx(hProcess, hMods[i], szModName, (uint)szModName.Capacity) == 0)
                    continue;

                string modName = szModName.ToString();
                int pos = modName.LastIndexOf('\\');
                modName = modName.Substring(pos + 1, modName.Length);

                ModuleInfo modInfo;
                if (!PSApi.GetModuleInformation(hProcess, hMods[i], out modInfo, (uint)sizeof(ModuleInfo))) {
                    continue;
                }

                ulong baseAddr = (ulong)modInfo.BaseOfDll;
                modsStartAddrs[modName] = baseAddr;
            }

            // Release the handle to the process
            hProcess.TryClose();
            return modsStartAddrs;
        }

        public static Dictionary<uint, string> GetTIDsModuleStartAddr(uint pid)
        {
            Dictionary<uint, string> tidsStartModule = new();

            Dictionary<string, ulong> modsStartAddrs = GetModulesNamesAndBaseAddresses(pid);
            if (modsStartAddrs.Count == 0)
                return tidsStartModule;

            List<uint> tids = GetTIDChronologically(pid);
            if (tids.Count == 0)
                return tidsStartModule;

            Dictionary<uint, ulong> tidsStartAddresses = GetThreadsStartAddresses(tids);
            if (tidsStartAddresses.Count == 0)
                return tidsStartModule;

            foreach (var thisTid in tidsStartAddresses) {
                uint tid = thisTid.Key;
                ulong startAddress = thisTid.Value;
                ulong nearestModuleAtLowerAddrBase = 0;
                string nearestModuleAtLowerAddrName = "";

                foreach (var thisModule in modsStartAddrs) {
                    string moduleName = thisModule.Key;
                    ulong moduleBase = thisModule.Value;

                    if (moduleBase > startAddress)
                        continue;

                    if (moduleBase > nearestModuleAtLowerAddrBase) {
                        nearestModuleAtLowerAddrBase = moduleBase;
                        nearestModuleAtLowerAddrName = moduleName;
                    }
                }

                if (nearestModuleAtLowerAddrBase > 0 && nearestModuleAtLowerAddrName != "")
			        tidsStartModule[tid] = nearestModuleAtLowerAddrName;
            }

            return tidsStartModule;
        }

        unsafe static List<uint> GetTIDChronologically(uint pid)
        {
            List<uint> tids = new();

            if (pid == 0)
                return tids;

            SortedDictionary<ulong, uint> tidsWithStartTimes = new();

            Handle hThreadSnap = Kernel32.CreateToolhelp32Snapshot(SnapshotFlags.Thread, 0);
            if (!hThreadSnap.IsInvalid) {
                ThreadEntry32 th32 = new();
                th32.Size = (uint)sizeof(ThreadEntry32);

                var afTimes = stackalloc FILETIME[4];

                bool bOK = true;
                for (
                    bOK = Kernel32.Thread32First(hThreadSnap, ref th32);
                    bOK;
                    bOK = Kernel32.Thread32Next(hThreadSnap, out th32)
                ) {
                    if (th32.OwnerProcessID == pid) {
                        Handle hThread = Kernel32.OpenThread(ThreadAccessFlags.QueryInformation, false, th32.ThreadID);
                        
                        if (!hThread.IsInvalidOrNull) {
                            if (Kernel32.GetThreadTimes(hThread, out afTimes[0], out afTimes[1], out afTimes[2], out afTimes[3])) {
                                ulong ullTest =
                                    (uint)(ulong)afTimes[0].dwLowDateTime |
                                    (((ulong)afTimes[0].dwHighDateTime) << 32);

                                tidsWithStartTimes[ullTest] = th32.ThreadID;
                            }

                            hThread.TryClose();
                        }
                    }
                }

                hThreadSnap.TryClose();
            }

            foreach (var thread in tidsWithStartTimes) {
                tids.Add(thread.Value);
            }

            return tids;
        }

        unsafe static Dictionary<uint, ulong> GetThreadsStartAddresses(List<uint> tids)
        {
            Dictionary<uint, ulong> tidsStartAddresses = new();

            if (tids.Count == 0)
                return tidsStartAddresses;

            for (int i = 0; i < tids.Count; ++i) {
                Handle hThread = Kernel32.OpenThread(ThreadAccessFlags.AllAccess, false, tids[i]);
                void* startAddress = null;
                uint returnLength = 0;

                NTStatus ntQIT = NTDll.NtQueryInformationThread(
                    hThread,
                    ThreadInfoClass.ThreadQuerySetWin32StartAddress,
                    &startAddress,
                    (uint)sizeof(void*),
                    out returnLength
                );

                hThread.TryClose();
                if (tids[i] != 0 && startAddress != null)
                    tidsStartAddresses[tids[i]] = (ulong)startAddress;
            }

            return tidsStartAddresses;
        }

        public static unsafe List<UnusedXMem> FindExecutableMemory(Handle hProcess, bool onlyInBase = false)
        {
            List<MemoryBasicInformation> memInfos = new();
            List<MemoryBasicInformation> execMemInfos = new();
            List<UnusedXMem> freeXMems = new();
            void* baseAddr = null;

            if (onlyInBase) {
                baseAddr = ProcessUtility.GetBaseAddress(hProcess);
            }

            int memBasicInfoSize = sizeof(MemoryBasicInformation);


            // Getting all MEMORY_BASIC_INFORMATION of the target process
            byte* addr;
            for (
                addr = null;
                Kernel32.VirtualQueryEx(hProcess, (nint)addr, out var memInfo, (uint)memBasicInfoSize) == (nuint)memBasicInfoSize;
                addr += memInfo.RegionSize
            ) {
                if (!onlyInBase || (onlyInBase && memInfo.AllocationBase == baseAddr)) {
                    memInfos.Add(memInfo);
                }
            }

            if (memInfos.Count == 0) {
                return freeXMems;
            }

            // Filtering only executable memory regions
            for (int i = 0; i < memInfos.Count; ++i) {
                var prot = memInfos[i].Protect;
                if (
                    prot is MemoryProtection.Execute
                         or MemoryProtection.ExecuteRead
                         or MemoryProtection.ExecuteReadWrite
                         or MemoryProtection.ExecuteWriteCopy
                ) {
                    execMemInfos.Add(memInfos[i]);
                }
            }

            if (execMemInfos.Count == 0)
                return freeXMems;

            // Duplicating memory locally for analysis, finding unused memory at the end of executable regions
            for (int i = 0; i < execMemInfos.Count; ++i) {
                // Getting local buffer
                void* localMemCopy = (void*)Kernel32.VirtualAlloc(
                    0,
                    (uint)execMemInfos[i].RegionSize,
                    AllocationType.Commit,
                    MemoryProtection.ReadWrite
                );

                if (localMemCopy == null)
                    continue; // Error, no local buffer

                // Copying executable memory content locally
                if (!Kernel32.ReadProcessMemory(hProcess, execMemInfos[i].BaseAddress, localMemCopy, execMemInfos[i].RegionSize, out nuint bytesRead)) {
                    // Error while copying the executable memory content to local process for analysis
                    Kernel32.VirtualFree(localMemCopy, execMemInfos[i].RegionSize, FreeType.Release);
                    continue;
                }

                // Analysing unused executable memory size and location locally
                byte currentByte = 0;
                nuint unusedSize = 0;
                ulong analysingByteAddr = (ulong)localMemCopy + execMemInfos[i].RegionSize - 1;
                while (analysingByteAddr >= (ulong)localMemCopy) {
                    Kernel32.CopyMemory(&currentByte, (void*)analysingByteAddr, sizeof(byte));
                    if (currentByte != 0)
                        break;
                    ++unusedSize;
                    --analysingByteAddr; // Next byte
                }
                if (unusedSize == 0) {
                    // No unused bytes
                    Kernel32.VirtualFree(localMemCopy, execMemInfos[i].RegionSize, FreeType.Release);
                    continue;
                }

                // Found unused executable memory, pushing it into the result vector
                UnusedXMem unusedXMem = new();
                unusedXMem.RegionInfo = execMemInfos[i];
                unusedXMem.Size = unusedSize;
                unusedXMem.Start = (void*)((ulong)execMemInfos[i].BaseAddress + execMemInfos[i].RegionSize - unusedSize);
                freeXMems.Add(unusedXMem);

                // Clean-up
                Kernel32.VirtualFree(localMemCopy, execMemInfos[i].RegionSize, FreeType.Release);
            }

            return freeXMems;
        }
    }
}
