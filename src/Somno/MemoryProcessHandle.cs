using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno
{
    /// <summary>
    /// Represents a handle that can modify the memory of a process.
    /// </summary>
    internal struct MemoryProcessHandle
    {
        /// <summary>
        /// The ID of the owning process.
        /// </summary>
        public int OwnerPID;

        /// <summary>
        /// The handle itself.
        /// </summary>
        public int Handle;

        /// <summary>
        /// Whether the handle is valid.
        /// </summary>
        public bool IsValid => OwnerPID != 0 && Handle != 0;
    }
}
