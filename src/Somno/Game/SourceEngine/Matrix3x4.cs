using System.Numerics;
using System.Runtime.InteropServices;

namespace Somno.Game.SourceEngine;

[StructLayout(LayoutKind.Sequential)]
internal struct Matrix3x4
{
    public float M00; // xAxis.x
    public float M10; // yAxis.x
    public float M20; // zAxis.x
    public float M30; // vecOrigin.x

    public float M01; // xAxis.y
    public float M11; // yAxis.y
    public float M21; // zAxis.y
    public float M31; // vecOrigin.y

    public float M02; // xAxis.z
    public float M12; // yAxis.z
    public float M22; // zAxis.z
    public float M32; // vecOrigin.z

    public Matrix4x4 To4x4()
        => new() {
            M11 = this.M00,
            M12 = this.M01,
            M13 = this.M02,

            M21 = this.M10,
            M22 = this.M11,
            M23 = this.M12,

            M31 = this.M20,
            M32 = this.M21,
            M33 = this.M22,

            M41 = this.M30,
            M42 = this.M31,
            M43 = this.M32,
            M44 = 1,
        };
}
