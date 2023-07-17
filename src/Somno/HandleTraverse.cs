using Somno.Native;
using Somno.Native.NT;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Somno
{
    internal static class HandleTraverse
    {
        const uint ProcessVMRead = 0x10;
        const uint ProcessVMWrite = 0x20;

        static unsafe NTStatus QuerySystemInfo(SystemInformationClass infoClass, out byte* buffer, out int bufferLength)
        {
            NTStatus status;

            bufferLength = 0x100000;
            buffer = (byte*)Marshal.AllocHGlobal(bufferLength);

            do {
                status = NTDll.NtQuerySystemInformation(
                    infoClass,
                    buffer,
                    unchecked((uint)bufferLength),
                    out uint returnLength
                );

                if (status == NTStatus.InfoLengthMismatch) {
                    bufferLength = unchecked((int)returnLength) * 2;
                    buffer = (byte*)Marshal.ReAllocHGlobal((nint)buffer, bufferLength);
                }
            } while (status == NTStatus.InfoLengthMismatch);

            return status;
        }

        public static unsafe MemoryProcessHandle FindMemoryHandle(int pid)
        {
            var locatorDummyHandle = Kernel32.OpenProcess(
                ProcessAccessFlags.QueryLimitedInformation,
                false,
                (uint)pid
            );

            NTStatus status;
            status = QuerySystemInfo(
                SystemInformationClass.SystemHandleInformation,
                out var buffer, out var bufferLength
            );

            // After the query is complete, we can close the handle.
            Kernel32.CloseHandle(locatorDummyHandle);

            if (status != NTStatus.Success) {
                Console.WriteLine($"Could not retrieve system handles: 0x{((uint)status):X8}");
                return default;
            }

            var handleInfo = (SystemHandleInformation*)buffer;

            // First, locate the kernel object address of the target process.
            void* internalPtr = null;

            for (int i = 0; i < handleInfo->NumberOfHandles; i++) {
                var handle = handleInfo->Handles[i];

                if (handle.ObjectTypeIndex != 7) {
                    continue;
                }

                if (
                    handle.UniqueProcessId == Environment.ProcessId &&
                    handle.HandleValue == locatorDummyHandle
                ) {
                    internalPtr = handle.Object;
                    break;
                }
            }

            var possibleVectors = new List<MemoryProcessHandle>();

            // Now, locate every handle that has the required permissions
            // for the object pointer (the process) we've identified.
            for (int i = 0; i < handleInfo->NumberOfHandles; i++) {
                var handle = handleInfo->Handles[i];

                if (
                    (handle.Object != internalPtr) ||
                    (handle.GrantedAccess & ProcessVMRead) == 0
                ) {
                    continue;
                }

                possibleVectors.Add(new MemoryProcessHandle() {
                    Handle = handle.HandleValue,
                    OwnerPID = handle.UniqueProcessId
                });

                Terminal.LogInfo($"Possible vector found: PID {handle.UniqueProcessId} (0x{handle.UniqueProcessId:X2})");
            }

            Marshal.FreeHGlobal((nint)buffer);

            if (possibleVectors.Count == 0) {
                return default;
            }

            return possibleVectors.MaxBy(x => x.OwnerPID);
        }
    }
}
