using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Game.Rendering
{
    internal static class Math3D
    {
        public static Vector3 Normalize(this Vector3 vector) => Vector3.Normalize(vector);
        public static Vector3 Cross(this Vector3 vector, Vector3 other) => Vector3.Cross(vector, other);
        public static float Dot(this Vector3 vector, Vector3 other) => Vector3.Dot(vector, other);

        /// <summary>
        /// Angle between 3d vectors.
        /// </summary>
        public static float AngleTo(this Vector3 vector, Vector3 other)
        {
            return AngleBetweenUnitVectors(vector.Normalize(), other.Normalize());
        }

        /// <summary>
        /// Angle between 3d unit vectors (normalized vectors).
        /// </summary>
        public static float AngleBetweenUnitVectors(Vector3 leftNormalized, Vector3 rightNormalized)
        {
            return AcosClamped(leftNormalized.Dot(rightNormalized));
        }

        /// <summary>
        /// <see cref="Math.Acos"/> with clamped [-1..1] value.
        /// </summary>
        public static float AcosClamped(float value, float tolerance = 1E-6f)
        {
            if (value > 1 - tolerance)
                return 0;

            if (value < tolerance - 1)
                return MathF.PI;
 
            return MathF.Acos(value);
        }

        /// <summary>
        /// Get orthogonal axis from given normal.
        /// </summary>
        public static void GetOrthogonalAxis(Vector3 normal, out Vector3 xAxis, out Vector3 yAxis, out Vector3 zAxis)
        {
            zAxis = Vector3.Normalize(normal);
            var zAxisWorld = new Vector3(0, 0, 1);
            var angleToAxisZ = zAxis.AngleTo(zAxisWorld);
            if (angleToAxisZ < System.Math.PI * 0.25 || angleToAxisZ > System.Math.PI * 0.75) {
                // too close to z-axis, use y-axis
                xAxis = new Vector3(0, 1, 0).Cross(zAxis).Normalize();
            }
            else {
                // use z-axis
                xAxis = zAxis.Cross(zAxisWorld).Normalize();
            }
            yAxis = zAxis.Cross(xAxis).Normalize();
        }

        /// <summary>
        /// Get orthogonal matrix from given normal and origin.
        /// </summary>
        public static Matrix4x4 GetOrthogonalMatrix(Vector3 normal, Vector3 origin)
        {
            GetOrthogonalAxis(normal, out var xAxis, out var yAxis, out var zAxis);
            return GetMatrix(xAxis, yAxis, zAxis, origin);
        }

        /// <summary>
        /// Get matrix from given axis and origin.
        /// </summary>
        public static Matrix4x4 GetMatrix(Vector3 xAxis, Vector3 yAxis, Vector3 zAxis, Vector3 origin)
        {
            return new Matrix4x4 {
                M11 = xAxis.X,
                M12 = xAxis.Y,
                M13 = xAxis.Z,

                M21 = yAxis.X,
                M22 = yAxis.Y,
                M23 = yAxis.Z,

                M31 = zAxis.X,
                M32 = zAxis.Y,
                M33 = zAxis.Z,

                M41 = origin.X,
                M42 = origin.Y,
                M43 = origin.Z,
                M44 = 1,
            };
        }

        /// <summary>
        /// Get 3d circle vertices.
        /// </summary>
        public static Vector3[] GetCircleVertices(Vector3 origin, Vector3 normal, double radius, int segments)
        {
            var matrixLocalToWorld = GetOrthogonalMatrix(normal, origin);
            var vertices = GetCircleVertices2D(new Vector3(), radius, segments);
            for (var i = 0; i < vertices.Length; i++) {
                vertices[i] = Vector3.Transform(vertices[i], matrixLocalToWorld);
            }

            return vertices;
        }

        /// <summary>
        /// Get 2d (flat) circle vertices (x,y,0).
        /// </summary>
        public static Vector3[] GetCircleVertices2D(Vector3 origin, double radius, int segments)
        {
            var vertices = new Vector3[segments + 1];
            var step = System.Math.PI * 2 / segments;
            for (var i = 0; i < segments; i++) {
                var theta = step * i;
                vertices[i] = new Vector3
                (
                    (float)(origin.X + radius * System.Math.Cos(theta)),
                    (float)(origin.Y + radius * System.Math.Sin(theta)),
                    0
                );
            }
            vertices[segments] = vertices[0];
            return vertices;
        }

        /// <summary>
        /// Get half-sphere vertices.
        /// </summary>
        /// <returns>
        /// Returns array of 3d circles. Each circle is array of vertices.
        /// </returns>
        public static Vector3[][] GetHalfSphere(Vector3 origin, Vector3 normal, float radius, int segments, int layers)
        {
            normal.Normalize();
            var verticesByLayer = new Vector3[layers][];
            for (var layerId = 0; layerId < layers; layerId++) {
                var radiusLayer = radius - layerId * (radius / layers);
                var originLayer = origin + normal * ((float)System.Math.Cos(System.Math.Asin(radiusLayer / radius)) * radius);
                verticesByLayer[layerId] = GetCircleVertices(originLayer, normal, radiusLayer, segments);
            }
            return verticesByLayer;
        }
    }
}
