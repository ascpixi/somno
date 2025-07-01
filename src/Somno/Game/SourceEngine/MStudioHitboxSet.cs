using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Game.SourceEngine
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MStudioHitboxSet
    {
        public uint SZNameIndex;
        public uint NumHitboxes;
        public uint HitboxIndex;
    }
}
