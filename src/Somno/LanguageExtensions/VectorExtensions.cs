using System.Numerics;
using System.Runtime.CompilerServices;

namespace Somno.LanguageExtensions;

internal static class VectorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Add(this Vector3 self, float x, float y, float z)
    {
        self.X += x;
        self.Y += y;
        self.Z += z;
        return self;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Add(this Vector2 self, float x, float y)
    {
        self.X += x;
        self.Y += y;
        return self;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 AddX(this Vector3 self, float x)
    {
        self.X += x;
        return self;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 AddY(this Vector3 self, float y)
    {
        self.Y += y;
        return self;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 AddZ(this Vector3 self, float z)
    {
        self.Z += z;
        return self;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 AsVec3(this Vector2 self) => new(self.X, self.Y, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 AsVec2(this Vector3 self) => new(self.X, self.Y);
}
