using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Somno.Portal.Native
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct ProcessEntry32W
    {
        public uint Size;
        public uint Usage;
        public uint ProcessID;
        public IntPtr DefaultHeapID;
        public uint ModuleID;
        public uint Threads;
        public uint ParentProcessID;
        public int PriClassBase;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string ExeFile;
    };
}
