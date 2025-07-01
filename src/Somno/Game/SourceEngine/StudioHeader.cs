using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Game.SourceEngine
{
    /// <summary>
    /// The <c>studiohdr_t</c> structure. Represents an MDL model.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct StudioHeader
    {
        public int Id; // 0x00
        public int Version; // 0x04
        public int Checksum; // 0x08
        public fixed byte Name[64]; // 0xC
        public int Length; // 4C
        public Vector3 EyePosition; // 0x50
        public Vector3 IllumPosition; // 0x5C
        public Vector3 HullMin; // 0x68
        public Vector3 HullMax; // 0x74
        public Vector3 ViewBBMin; // 0x80
        public Vector3 ViewBBMax; // 0x8C
        public int Flags; // 0x98
        public int NumBones; // 0x9C
        public int BoneIndex; // 0xA0
        public int NumBoneControllers; // 0xA4
        public int BoneControllerIndex; // 0xA8
        public int NumHitboxSets; // 0xAC
        public uint HitboxSetIndex; // 0xB0
    }
}
