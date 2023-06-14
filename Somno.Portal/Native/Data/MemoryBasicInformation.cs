using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Somno.Portal.Native.Data
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct MemoryBasicInformation
    {
        /// <summary>
        /// A pointer to the base address of the region of pages.
        /// </summary>
        public void* BaseAddress;

        /// <summary>
        /// A pointer to the base address of a range of pages allocated by the VirtualAlloc function. The page pointed to by the BaseAddress member is contained within this allocation range.
        /// </summary>
        public void* AllocationBase;

        /// <summary>
        /// The memory protection option when the region was initially allocated. This member can be one of the memory protection constants or 0 if the caller does not have access.
        /// </summary>
        public uint AllocationProtect;

        public ushort PartitionId;

        /// <summary>
        /// The size of the region beginning at the base address in which all pages have identical attributes, in bytes.
        /// </summary>
        public nuint RegionSize;

        /// <summary>
        /// The state of the pages in the region.
        /// </summary>
        public MemoryRegionState State;

        /// <summary>
        /// The access protection of the pages in the region. This member is one of the values listed for the AllocationProtect member.
        /// </summary>
        public MemoryProtection Protect;

        /// <summary>
        /// The type of pages in the region.
        /// </summary>
        public MemoryRegionType Type;
    }
}
