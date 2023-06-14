using Somno.Portal;
using Somno.Portal.Windows;
using Somno.Portal.Native;
using Somno.Portal.Native.Data.Kernel32;
using Somno.Portal.Native.Data.AdvAPI32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Somno.Portal.Native.Data;
using Somno.Portal.Native.Data.NTDll;
using Somno.Portal.Shellcode;

namespace Somno.Portal
{
    internal unsafe static class Warp
    {
        const string SharedMemoryName = @"Global\SMNWARPMem";
        const int PaddingInXMem = 8;
        const int InitialSharedMemorySize = 4096;

        static void* remoteExecMem = null;
        static nuint remoteExecMemSize = 0;
        static void* ptrRemoteSharedMem = null;
        static uint targetTID = 0;
        static Handle hThread;
        static Handle hLocalSharedMem;
        static void* ptrLocalSharedMem;
        static nuint sharedMemSize = InitialSharedMemorySize;
        static nuint usableSharedMemSize;
        static Handle hGatekeeperProcess;

        public static bool Initialize()
        {
            var mutex = Kernel32.CreateMutex(0, true, @"Global\SMNWARPMutex");
            if (Marshal.GetLastWin32Error() == Win32Status.ERROR_ALREADY_EXISTS) {
                // Security: An instance of either the installer or the client is already running, terminate now
                Environment.Exit(1);
            }

            // TODO: Make sure no anti-cheats are running here.

            if (!SetPrivilege("SeDebugPrivilege")) {
                return false;
            }

            // Get the PID of LSASS (TODO: maybe we can get a less intrusive process?)
            var lsassPIDs = ProcessUtility.GetPIDs("lsass.exe");
            if (lsassPIDs.Count == 0)
                return false;

            lsassPIDs.Sort();
            var pivotPID = lsassPIDs[0];
            if (pivotPID == 0)
                return false;

            var sharedMem = Kernel32.OpenFileMapping(FileMapAccess.AllAccess, false, SharedMemoryName);
            if (!sharedMem.IsInvalidOrNull)
                return true; // Already intiailized

            // Attachment to process: Get PID and OpenProcess
            var hProcess = Kernel32.OpenProcess(
                ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VMOperation | ProcessAccessFlags.VMRead | ProcessAccessFlags.VMWrite,
                false,
                pivotPID
            );

            // Getting executable memory
            var availableXMem = ProcessUtility.FindExecutableMemory(hProcess);
            if (availableXMem.Count == 0 || availableXMem[0].Start == null || availableXMem[0].Size == 0) {
                return false; // No executable memory
            }

            if (availableXMem[0].Size <= PaddingInXMem) {
                // Executable memory smaller or equal to demanded padding
                return false;
            }

            remoteExecMem = (void*)((ulong)availableXMem[0].Start + PaddingInXMem);
            remoteExecMemSize = availableXMem[0].Size - PaddingInXMem;

            // Attaching to thread for thread hijacking, auto finds usable thread
            var startModulesTIDs = ProcessUtility.GetTIDsModuleStartAddr(pivotPID);
            List<string> preferredModuleTIDs = new() {
                "samsrv.dll",
                "msvcrt.dll",
                "crypt32.dll"
            };

            string? moduleName = null;
            for (int i = 0; i < preferredModuleTIDs.Count; ++i) {
                foreach (var thisTid in startModulesTIDs) {
                    uint tid = thisTid.Key;
                    moduleName = thisTid.Value;

                    if (moduleName == preferredModuleTIDs[i]) {
                        targetTID = tid;
                        break;
                    }
                }

                if (targetTID != 0) {
                    break;
                }
            }

            if (targetTID == 0) {
                // Could not find any of the threads starting in one of the target modules
                return false;
            }

            // TODO: Ability to wait for samsrv

            hThread = Kernel32.OpenThread(
                ThreadAccessFlags.SuspendResume | ThreadAccessFlags.GetContext | ThreadAccessFlags.SetContext,
                false,
                targetTID
            );

            if (hThread.IsNull) {
                return false;
            }

            // Create shared memory
            hLocalSharedMem = Kernel32.CreateFileMapping(
                Handle.InvalidValue,
                IntPtr.Zero,
                PageProtection.FileMapAllAccess,
                0, 0,
                SharedMemoryName
            );

            if (hLocalSharedMem.IsInvalidOrNull) {
                return false;
            }

            ptrLocalSharedMem = Kernel32.MapViewOfFile(
                hLocalSharedMem,
                PageProtection.FileMapAllAccess,
                0, 0,
                sharedMemSize
            );

            if (ptrLocalSharedMem == null) {
                return false;
            }

            usableSharedMemSize = sharedMemSize - (uint)sizeof(PortalConfiguration);

            // Duplicate the handle to shared memory in explorer.exe so a handle keep
            // existing which allows easy reconnection (using OpenFileMapping)
            List<uint> explorerPIDs = ProcessUtility.GetPIDs("explorer.exe");
            if (explorerPIDs.Count == 0) {
                return false;
            }

            hGatekeeperProcess = Kernel32.OpenProcess(
                ProcessAccessFlags.DuplicateHandle,
                false,
                explorerPIDs[0]
            );

            if (hGatekeeperProcess.IsInvalidOrNull) {
                return false;
            }

            if (!Kernel32.DuplicateHandle(
                Kernel32.GetCurrentProcess(),
                hLocalSharedMem,
                hGatekeeperProcess,
                out var hGatekeeper,
                0,
                false,
                0x00000002 // DUPLICATE_SAME_ACCESS
            )) {
                return false;
            }

            hGatekeeperProcess.TryClose();

            // Connecting shared memory in pivot process
            if (!ConnectSharedMemory()) {
                return;
            }

            hLocalSharedMem.TryClose(); // Close handle to shared memory

            if (!Start()) {
                return false;
            }

            // Clean-up, closing now unnecessary handles and other potential detection vectors
            hProcess.TryClose();
            hThread.TryClose();

            // Pushes useful information into shared memory, in case the bypass has to reconnect
            //CPUContext contextEmpty = default;
            PortalConfiguration cfgBackup = new() {
                RemoteSharedMemory = ptrRemoteSharedMem,
                SharedMemorySize = sharedMemSize,
                RemoteExecuteMemory = remoteExecMem,
                RemoteExecuteMemorySize = remoteExecMemSize
            };

            void* endOfUsableLocalSharedMem = (void*)((ulong)ptrLocalSharedMem + sharedMemSize - (uint)sizeof(PortalOrder));
            void* backupAddrInSharedMem = (void*)((ulong)endOfUsableLocalSharedMem - (uint)sizeof(PortalConfiguration));
            Kernel32.CopyMemory(
                backupAddrInSharedMem,
                &cfgBackup,
                (nuint)sizeof(PortalConfiguration)
            );

            Cleanup();
            return true;
        }

        unsafe static bool ConnectSharedMemory()
        {
            // Getting function addresses
            var kernel32 = Kernel32.GetModuleHandle("kernel32.dll");
            var addrOpenFileMappingA = Kernel32.GetProcAddress(kernel32, "OpenFileMappingA");
            var addrMapViewOfFile = Kernel32.GetProcAddress(kernel32, "MapViewOfFile");
            var addrCloseHandle = Kernel32.GetProcAddress(kernel32, "CloseHandle");
            if (addrOpenFileMappingA == 0 || addrMapViewOfFile == 0 || addrCloseHandle == 0)
                return false;

            // Get RW memory to assemble full shellcode from parts
            void* rwMemory = (void*)Kernel32.VirtualAlloc(
                IntPtr.Zero,
                4096,
                AllocationType.Commit,
                MemoryProtection.ReadWrite
            );

            ulong addrEndOfShellCode = (ulong)rwMemory;
            int lpNameOffset;

            const uint DesiredAccess = (uint)PageProtection.FileMapAllAccess;

            // HANDLE fm = OpenFileMapping(FILE_MAP_ALL_ACCESS, FALSE, <lpName>);
            using (var em = new X8664Emitter(256)) {
                em.Move(X86Register.RCX, DesiredAccess);        // dwDesiredAccess = DesiredAccess
                em.Xor(X86Register.RDX, X86Register.RDX);       // bInheritHandle = FALSE
                lpNameOffset = em.Move(RegisterREX.R8, 0x00ul);                // &lpName
                em.Xor(RegisterREX.R9, RegisterREX.R9);         // TODO: ???
                em.Move(X86Register.RAX, (ulong)addrOpenFileMappingA);
                em.Subtract(X86Register.RSP, 0x20);
                em.CallUsingRAX();
                em.Add(X86Register.RSP, 0x20);
                em.Move(RegisterREX.R15, X86Register.RAX);

                var assembled = em.GetAssembled();
                fixed (byte* assembledPtr = assembled)
                    Kernel32.CopyMemory(
                        (void*)addrEndOfShellCode,
                        assembledPtr,
                        (uint)assembled.Length
                    );

                addrEndOfShellCode += (uint)assembled.Length;
            }
            // (R15 + RAX have "fm")

            // MapViewOfFile(fm, FILE_MAP_ALL_ACCESS, NULL, NULL, sharedMemSize);
            using (var em = new X8664Emitter(256)) {
                em.Move(X86Register.RCX, X86Register.RAX);
                em.Move(X86Register.RDX, DesiredAccess);
                em.Xor(RegisterREX.R8, RegisterREX.R8); // dwFileOffsetHigh
                em.Xor(RegisterREX.R9, RegisterREX.R9); // dwFileOffsetLow
                em.Move(X86Register.RAX, sharedMemSize); // dwNumberOfBytesToMap
                em.Push(X86Register.RAX);
                em.Move(X86Register.RAX, unchecked((ulong)addrMapViewOfFile));
                em.Subtract(X86Register.RSP, 0x20);
                em.CallUsingRAX();
                em.Add(X86Register.RSP, 0x28);
                em.Move(RegisterREX.R14, X86Register.RAX);

                // Writing to shared memory the virtual address in pivot process
                // mov [r14], r14
                em.MoveToAddress(RegisterREX.R14, RegisterREX.R14);

                var assembled = em.GetAssembled();
                fixed (byte* assembledPtr = assembled)
                    Kernel32.CopyMemory(
                        (void*)addrEndOfShellCode,
                        assembledPtr,
                        (uint)assembled.Length
                    );

                addrEndOfShellCode += (uint)assembled.Length;
            }
            // (R15 has "fm")

            // CloseHandle(fm);
            using (var em = new X8664Emitter(256)) {
                em.Move(X86Register.RCX, RegisterREX.R15);
                em.Move(X86Register.RAX, (ulong)addrCloseHandle);
                em.Subtract(X86Register.RSP, 0x20);
                em.CallUsingRAX();
                em.Add(X86Register.RSP, 0x20);

                var assembled = em.GetAssembled();
                fixed (byte* assembledPtr = assembled)
                    Kernel32.CopyMemory(
                        (void*)addrEndOfShellCode,
                        assembledPtr,
                        (uint)assembled.Length
                    );

                addrEndOfShellCode += (uint)assembled.Length;
            }

            // while(true) {}
            using (var em = new X8664Emitter(4)) {
                em.WarmerHalt();

                var assembled = em.GetAssembled();
                fixed (byte* assembledPtr = assembled)
                    Kernel32.CopyMemory(
                        (void*)addrEndOfShellCode,
                        assembledPtr,
                        (uint)assembled.Length
                    );

                addrEndOfShellCode += (uint)assembled.Length;
            }

            byte[] sharedMemNameAscii = Encoding.ASCII.GetBytes(SharedMemoryName);
            byte* lpNameBuffer = stackalloc byte[SharedMemoryName.Length];
            uint lpNameBufferLen = (uint)SharedMemoryName.Length;

            fixed (byte* sharedMemName = sharedMemNameAscii) {
                Kernel32.CopyMemory(
                    lpNameBuffer,
                    sharedMemName,
                    lpNameBufferLen
                );
            }

            Kernel32.CopyMemory(
                (void*)addrEndOfShellCode,
                lpNameBuffer,
                lpNameBufferLen
            );

            addrEndOfShellCode += lpNameBufferLen;

            // Calculating full size of shellcode
            ulong fullShellcodeSize = addrEndOfShellCode - (ulong)rwMemory;

            // Placing pointer to the buffer integrated with the shellcode containing the name
            ulong lpNameInRemoteExecMemory = (ulong)remoteExecMem + fullShellcodeSize - lpNameBufferLen;
            Kernel32.CopyMemory(
                (void*)((ulong)rwMemory + (uint)lpNameOffset), // originally was +12
                &lpNameInRemoteExecMemory,
                sizeof(long)
            );

            bool pushShellcodeStatus = PushShellcode(rwMemory, fullShellcodeSize);
            VirtualFree(rwMemory, 0, MEM_RELEASE);
            if (!pushShellcodeStatus)
                return false;

            if (!ExecWithThreadHiJacking(fullShellcodeSize - sizeof(lpNameBuffer), false)) // The shellcode ends before since the end is just memory
                return false;

            CopyMemory(&ptrRemoteSharedMem, ptrLocalSharedMem, sizeof(void*));
            if (ptrRemoteSharedMem == nullptr)
                return false;
            else
                return true;

        }

        static bool SetPrivilege(string lpszPrivilege, bool bEnablePrivilege = true)
        {
            Handle hToken;
            if (!AdvAPI32.OpenProcessToken(Kernel32.GetCurrentProcess(), Kernel32.TokenAdjustPrivilidges, out hToken)) {
                hToken.TryClose();
                return false;
            }

            LUID luid;
            if (!AdvAPI32.LookupPrivilegeValue(null, lpszPrivilege, out luid)) {
                hToken.TryClose();
                return false;
            }

            TokenPrivileges priv = default;

            priv.PrivilegeCount = 1;
            priv.Privileges[0].Luid = luid;
            priv.Privileges[0].Attributes = bEnablePrivilege ? AdvAPI32.SE_PRIVILEGE_ENABLED : AdvAPI32.SE_PRIVILEGE_REMOVED;

            if (!AdvAPI32.AdjustTokenPrivileges(hToken, false, ref priv, 0, default, default)) {
                hToken.TryClose();
                return false;
            }

            hToken.TryClose();
            return true;
        }
    }
}
