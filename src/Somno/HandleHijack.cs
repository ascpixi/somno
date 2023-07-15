using Somno.Native.NT;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno
{
    internal static class HandleHijack
    {
        const uint ProcessVMRead = 0x10;
        const uint ProcessVMWrite = 0x20;

        public static unsafe MemoryProcessHandle FindMemoryHandle(string processName)
        {
            NTStatus status;

            int bufferLength = 0x1000;
            byte* buffer = (byte*)Marshal.AllocHGlobal(bufferLength);

            do {
                status = NTDll.NtQuerySystemInformation(
                    SystemInformationClass.SystemHandleInformation,
                    buffer,
                    unchecked((uint)bufferLength),
                    out uint returnLength
                );

                if (status == NTStatus.InfoLengthMismatch) {
                    bufferLength = unchecked((int)returnLength);
                    buffer = (byte*)Marshal.AllocHGlobal(bufferLength );
                }
            } while (status == NTStatus.InfoLengthMismatch);

            if(status != NTStatus.Success) {
                Terminal.LogError($"Could not retrieve system handles: 0x{((uint)status):X8}");
                return default;
            }

            byte* objTypeInfoBuffer = stackalloc byte[0x1000];

            var handleInfo = (SystemHandleInformation*)buffer;
            for (int i = 0; i < handleInfo->NumberOfHandles; i++) {
                var handle = handleInfo->Handles[i];

                if(handle.ObjectTypeIndex != 7) {
                    continue;
                }

                if(
                    (handle.GrantedAccess & ProcessVMRead) == 0 ||
                    (handle.GrantedAccess & ProcessVMWrite) == 0
                ) {
                    continue;
                }

                Console.WriteLine($"handle {handle.HandleValue:X2} at 0x{(ulong)handle.Object:X8} of PID {handle.UniqueProcessId}");
            }

            return default;
        }
    }
}
