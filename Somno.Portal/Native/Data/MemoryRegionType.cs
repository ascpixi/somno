using System;
using System.Collections.Generic;
using System.Text;

namespace Somno.Portal.Native.Data
{
    internal enum MemoryRegionType : uint
    {
        /// <summary>
        /// Indicates that the memory pages within the region are mapped into the view of an image section.
        /// </summary>
        Image = 0x1000000,

        /// <summary>
        /// Indicates that the memory pages within the region are mapped into the view of a section.
        /// </summary>
        Mapped = 0x40000,

        /// <summary>
        /// Indicates that the memory pages within the region are private (that is, not shared by other processes).
        /// </summary>
        Private = 0x20000
    }
}
