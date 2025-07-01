using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Game.SourceEngine
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct MStudioBBox
    {
        public const int MaxStudioBones = 128;

        public int Bone;
        public int Group; 
        public Vector3 BBMin;
        public Vector3 BBMax;
        public int SZHitboxNameIndex;
        private fixed int unused[3];
        public float Radius;
        private fixed int pad[4];
    }
}
