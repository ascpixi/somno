using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Somno.Portal.Native.Data.Kernel32
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct ProcessEntry32W
    {
        public uint Size;
        [Obsolete] public uint Usage;
        public uint ProcessID;
        [Obsolete] public IntPtr DefaultHeapID;
        [Obsolete] public uint ModuleID;
        [Obsolete] public uint Threads;
        public uint ParentProcessID;
        public int PriClassBase;
        [Obsolete] public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string ExeFile;
    };
}
