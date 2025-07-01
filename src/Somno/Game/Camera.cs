using Somno.UI;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vortice.Mathematics;

namespace Somno.Game;

/// <summary>
/// Represents the main in-game camera.
/// </summary>
internal static class Camera
{
    /// <summary>
    /// The vertical FOV.
    /// </summary>
    public const float VerticalFOV = 74;

    /// <summary>
    /// The view-matrix, used to transform from world space to screen space.
    /// </summary>
    public static readonly float[] ViewMatrix = new float[16];

    /// <summary>
    /// The rotation of the local player camera.
    /// </summary>
    public static Vector3 ViewAngles { get; internal set; }

    /// <summary>
    /// The position of the camera, in world space. This is equivalent to
    /// <see cref="GameManager.CurrentPlayer.EyePosition"/>, as the camera
    /// is always attached to the player.
    /// </summary>
    public static Vector3 Position => GameManager.CurrentPlayer.EyePosition;

    /// <summary>
    /// Transforms a position in world-space to a position in screen-space.
    /// </summary>
    /// <param name="position">The position to transform, in world space.</param>
    /// <param name="screenPos">The resulting position in screen-space.</param>
    /// <returns>Whether the transformed vector is visible.</returns>
    public static bool WorldToScreen(Vector3 position, out Vector2 screenPos)
    {
        float sx, sy; // screen X/Y
        float wx = position.X, wy = position.Y, wz = position.Z; // world X/Y/Z

        sx = (ViewMatrix[0] * wx) + (ViewMatrix[1] * wy) + (ViewMatrix[2] * wz) + ViewMatrix[3];
        sy = (ViewMatrix[4] * wx) + (ViewMatrix[5] * wy) + (ViewMatrix[6] * wz) + ViewMatrix[7];

        var flTemp = (ViewMatrix[12] * wx) + (ViewMatrix[13] * wy) + (ViewMatrix[14] * wz) + ViewMatrix[15];

        if (flTemp < 0.01f) {
            screenPos = default;
            return false;
        }

        var invFlTemp = 1 / flTemp;
        sx *= invFlTemp;
        sy *= invFlTemp;

        float scrWidth  = SomnoOverlay.Instance!.HostWindow.Dimensions.Width;
        float scrHeight = SomnoOverlay.Instance!.HostWindow.Dimensions.Height;

        var x = (float)scrWidth / 2f;
        var y = (float)scrHeight / 2f;

        x += (0.5f * sx * (float)scrWidth) + 0.5f;
        y -= (0.5f * sy * (float)scrHeight) + 0.5f;

        screenPos = new(x, y);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 WorldToScreen(Vector3 position)
    {
        WorldToScreen(position, out var result);
        return result;
    }

    public static bool IsPointInFOV(Vector3 point)
    {
        var forward = MathS.GetForward(ViewAngles);

        var camToPoint = point - Position;

        var camForwardNorm = Vector3.Normalize(forward);
        var camToPointNorm = Vector3.Normalize(camToPoint);
        float dot = Vector3.Dot(camForwardNorm, camToPointNorm);

        float vertFovCos = (float)Math.Cos(MathHelper.ToRadians(74.0f / 2));

        return dot >= vertFovCos;
    }
}
