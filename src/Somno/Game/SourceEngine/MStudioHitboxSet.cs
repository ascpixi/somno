using System.Runtime.InteropServices;

namespace Somno.Game.SourceEngine;

[StructLayout(LayoutKind.Sequential)]
internal struct MStudioHitboxSet
{
    public uint SZNameIndex;
    public uint NumHitboxes;
    public uint HitboxIndex;
}
