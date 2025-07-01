using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Native.NT
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SystemProcessInformation
    {
        internal uint NextEntryOffset;
        internal uint NumberOfThreads;
        private fixed byte Reserved1[48];
        internal UnicodeString ImageName;
        internal int BasePriority;
        internal IntPtr UniqueProcessId;
        private readonly UIntPtr Reserved2;
        internal uint HandleCount;
        internal uint SessionId;
        private readonly UIntPtr Reserved3;
        internal UIntPtr PeakVirtualSize;  // SIZE_T
        internal UIntPtr VirtualSize;
        private readonly uint Reserved4;
        internal UIntPtr PeakWorkingSetSize;  // SIZE_T
        internal UIntPtr WorkingSetSize;  // SIZE_T
        private readonly UIntPtr Reserved5;
        internal UIntPtr QuotaPagedPoolUsage;  // SIZE_T
        private readonly UIntPtr Reserved6;
        internal UIntPtr QuotaNonPagedPoolUsage;  // SIZE_T
        internal UIntPtr PagefileUsage;  // SIZE_T
        internal UIntPtr PeakPagefileUsage;  // SIZE_T
        internal UIntPtr PrivatePageCount;  // SIZE_T
        private fixed long Reserved7[6];
    }
}
