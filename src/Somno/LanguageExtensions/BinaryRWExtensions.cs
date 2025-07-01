using System;
using System.IO;
using System.Numerics;

namespace Somno.LanguageExtensions;

internal static class BinaryRWExtensions
{
    public static unsafe T Read<T>(this Stream self) where T : unmanaged
    {
        byte* buffer = stackalloc byte[sizeof(T)];
        self.Read(new Span<byte>(buffer, sizeof(T)));
        return *(T*)buffer;
    }

    public static void Write(this BinaryWriter self, Vector2 vec2)
    {
        self.Write(vec2.X);
        self.Write(vec2.Y);
    }

    public static Vector2 ReadVector2(this BinaryReader self)
        => new(self.ReadSingle(), self.ReadSingle());

    public static void Write(this BinaryWriter self, Vector3 vec3)
    {
        self.Write(vec3.X);
        self.Write(vec3.Y);
        self.Write(vec3.Z);
    }
    public static Vector3 ReadVector3(this BinaryReader self)
        => new(self.ReadSingle(), self.ReadSingle(), self.ReadSingle());

    public static void Write(this BinaryWriter self, Vector4 vec4)
    {
        self.Write(vec4.X);
        self.Write(vec4.Y);
        self.Write(vec4.Z);
        self.Write(vec4.W);
    }

    public static Vector4 ReadVector4(this BinaryReader self)
        => new(self.ReadSingle(), self.ReadSingle(), self.ReadSingle(), self.ReadSingle());
}
