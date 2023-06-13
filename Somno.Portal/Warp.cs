using Somno.Portal;
using Somno.Portal.Native;
using Somno.Portal.Native.Structures.AdvAPI32;
using Somno.Portal.Native.Structures.Kernel32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;

namespace Somno.Portal
{
    internal unsafe static class Warp
    {
        static void* remoteExecMem = null;
        static nuint remoteExecMemSize = 0;

        const int PADDING_IN_XMEM = 8;
        const int SHARED_MEM_SIZE = 4096;

        public static bool Initialize()
        {
            var mutex = Kernel32.CreateMutex(0, true, @"Global\SMNWARPMutex");
            if (Marshal.GetLastWin32Error() == Win32Status.ERROR_ALREADY_EXISTS) {
                // Security: An instance of either the installer or the client is already running, terminate now
                Environment.Exit(1);
            }

            // TODO: Make sure no anti-cheats are running here.

            if(!SetPrivilege("SeDebugPrivilege")) {
                return false;
            }

            // Get the PID of LSASS (TODO: maybe we can get a less intrusive process?)
            var lsassPIDs = ProcessUtility.GetPIDs("lsass.exe");
            if(lsassPIDs.Count == 0) {
                return false;
            }

            lsassPIDs.Sort();
            var pivotPID = lsassPIDs[0];
            if(pivotPID == 0) {
                return false;
            }

            var sharedMem = Kernel32.OpenFileMapping(FileMapAccess.AllAccess, false, @"Global\SMNWARPMem");
            if(sharedMem != default) {
                return true; // already initialized
            }

            // Attachment to process: Get PID and OpenProcess
            var process = Kernel32.OpenProcess(
                ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VMOperation | ProcessAccessFlags.VMRead | ProcessAccessFlags.VMWrite,
                false,
                pivotPID
            );

            // Getting executable memory
            var availableXMem = FindExecutableMemory(process);
            if (availableXMem.Count == 0 || availableXMem[0].Start == null || availableXMem[0].Size == 0) {
                return false; // No executable memory
            }

            if (availableXMem[0].Size <= PADDING_IN_XMEM) {
                // Executable memory smaller or equal to demanded padding
                return false; 
            }

            remoteExecMem = (void*)((ulong)availableXMem[0].Start + PADDING_IN_XMEM);
            remoteExecMemSize = availableXMem[0].Size - PADDING_IN_XMEM;

            // Attaching to thread for thread hijacking, auto finds usable thread
            var startModulesTIDs 
        }

        static bool SetPrivilege(string lpszPrivilege, bool bEnablePrivilege = true)
        {
            IntPtr hToken;
            if (!AdvAPI32.OpenProcessToken(Kernel32.GetCurrentProcess(), Kernel32.TokenAdjustPrivilidges, out hToken)) {
                if (hToken != default) {
                    Kernel32.CloseHandle(hToken);
                }

                return false;
            }

            LUID luid = default;
            if (!AdvAPI32.LookupPrivilegeValue(null, lpszPrivilege, ref luid)) {
                if (hToken != default) {
                    Kernel32.CloseHandle(hToken);
                }

                return false;
            }

            TokenPrivileges priv = default;

            priv.PrivilegeCount = 1;
            priv.Privileges[0].Luid = luid;
            priv.Privileges[0].Attributes = bEnablePrivilege ? AdvAPI32.SE_PRIVILEGE_ENABLED : AdvAPI32.SE_PRIVILEGE_REMOVED;
            
            if (!AdvAPI32.AdjustTokenPrivileges(hToken, false, ref priv, 0, default, default)) {
                if (hToken != default) {
                    Kernel32.CloseHandle(hToken);
                }

                return false;
            }

            if (hToken != default) {
                Kernel32.CloseHandle(hToken);
            }
            return true;
        }

        static unsafe List<UnusedXMem> FindExecutableMemory(IntPtr hProcess, bool onlyInBase = false) {
            List<MemoryBasicInformation> memInfos = new();
            List<MemoryBasicInformation> execMemInfos = new();
            List<UnusedXMem> freeXMems = new();
            void* baseAddr = null;
 
	        if (onlyInBase) {
                baseAddr = GetBaseAddress(hProcess);
            }

            // Getting all MEMORY_BASIC_INFORMATION of the target process
            byte* addr = null;
	        for (
                addr = null;
                Kernel32.VirtualQueryEx(hProcess, (nint)addr, out var memInfo, (uint)sizeof(MemoryBasicInformation)) == sizeof(MemoryBasicInformation);
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

        static unsafe void* GetBaseAddress(IntPtr hProcess) {
	        if (hProcess == default) {
                return null;
            }

            var lphModule = new IntPtr[1024];

	        if (!PSApi.EnumProcessModules(hProcess, lphModule, (uint)lphModule.Length, out uint lpcbNeeded))
		        return null; // Impossible to read modules (hProcess requires PROCESS_QUERY_INFORMATION | PROCESS_VM_READ)
	        
            return (void*)lphModule[0]; // Module 0 is the EXE itself, returning its address
        }

        Dictionary<string, ulong> GetModulesNamesAndBaseAddresses(uint pid)
        {
            Dictionary<string, ulong> modsStartAddrs = new();

            if (pid == 0)
                return modsStartAddrs;

            var hMods = new IntPtr[1024];
            uint cbNeeded;
            uint i;

            IntPtr hProcess = Kernel32.OpenProcess(
                ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VMRead,
                false,
                pid
            );

            if (hProcess == 0)
                return modsStartAddrs;

            // Get a list of all the modules in this process
            if (!Kernel32.EnumProcessModules(hProcess, hMods, sizeof(hMods), &cbNeeded)) {
                Kernel32.CloseHandle(hProcess);
                return modsStartAddrs;
            }

            // Get each module's infos
            for (i = 0; i < (cbNeeded / sizeof(HMODULE)); i++) {
                TCHAR szModName[MAX_PATH];
                if (!GetModuleFileNameEx(hProcess, hMods[i], szModName, sizeof(szModName) / sizeof(TCHAR))) // Get the full path to the module's file
                    continue;

                string modName = szModName;
                int pos = modName.find_last_of(L"\\");
                modName = modName.substr(pos + 1, modName.length());

                MODULEINFO modInfo;
                if (!GetModuleInformation(hProcess, hMods[i], &modInfo, sizeof(modInfo)))
                    continue;

                DWORD64 baseAddr = (DWORD64)modInfo.lpBaseOfDll;
                modsStartAddrs[modName] = baseAddr;
            }

            // Release the handle to the process
            Kernel32.CloseHandle(hProcess);
            return modsStartAddrs;
        }

        static Dictionary<uint, string> GetTIDsModuleStartAddr(uint pid)
        {
            Dictionary<uint, string> tidsStartModule = new();

            Dictionary<string, ulong> modsStartAddrs = GetModulesNamesAndBaseAddresses(pid);
            if (modsStartAddrs.Count == 0)
                return tidsStartModule;

            vector<DWORD> tids = GetTIDChronologically(pid);
            if (tids.empty())
                return tidsStartModule;

            map<DWORD, DWORD64> tidsStartAddresses = GetThreadsStartAddresses(tids);
            if (tidsStartAddresses.empty())
                return tidsStartModule;

            for (auto const&thisTid : tidsStartAddresses) {
                DWORD tid = thisTid.first;
                DWORD64 startAddress = thisTid.second;
                DWORD64 nearestModuleAtLowerAddrBase = 0;
                wstring nearestModuleAtLowerAddrName = L"";
                for (auto const&thisModule : modsStartAddrs) {
                    wstring moduleName = thisModule.first;
                    DWORD64 moduleBase = thisModule.second;
                    if (moduleBase > startAddress)
                        continue;
                    if (moduleBase > nearestModuleAtLowerAddrBase) {
                        nearestModuleAtLowerAddrBase = moduleBase;
                        nearestModuleAtLowerAddrName = moduleName;
                    }
                }
                if (nearestModuleAtLowerAddrBase > 0 && nearestModuleAtLowerAddrName != L"")
			tidsStartModule[tid] = nearestModuleAtLowerAddrName;
            }

            return tidsStartModule;
        }
    }
}
