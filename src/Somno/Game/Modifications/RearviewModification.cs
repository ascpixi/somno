using ImGuiNET;
using Somno.Game.Serialization;
using Somno.UI;
using System;
using System.Numerics;

namespace Somno.Game.Modifications;

internal class RearviewModification : GameModification, IOverlayRenderable, IConfigRenderable
{
    public bool Enabled;
    public float MaxDistance = 1500;
    public float RingSize = 16;
    public float MarkerSize = 2;

    public bool OverlayRenderDependsOnGame => true;

    public RearviewModification()
    {
        if (ConfigDeserializer.Exists("rearview")) {
            ConfigDeserializer.Deserialize("rearview")
                .ReadBool(x => Enabled = x)
                .ReadFloat(x => MaxDistance = x)
                .ReadFloat(x => RingSize = x)
                .ReadFloat(x => MarkerSize = x)
                .Finish();
        }
    }

    public void RenderConfiguration(SomnoOverlay overlay)
    {
        if (ImGui.CollapsingHeader("Rearview")) {
            ImGui.Text("Renders the relative positions of nearby enemies out of your field of vision around your crosshair.");
            ImGui.Indent(12);

            ImGui.Checkbox("Enabled", ref Enabled);
            ImGui.InputFloat("Maximum Distance", ref MaxDistance);
            ImGui.InputFloat("Ring Size", ref RingSize);
            ImGui.InputFloat("Marker Size", ref MarkerSize);

            if (ImGui.Button("Save configuration")) {
                ConfigSerializer.Serialize("rearview")
                    .Write(Enabled)
                    .Write(MaxDistance)
                    .Write(RingSize)
                    .Write(MarkerSize)
                    .Finish();
            }

            ImGui.Unindent(12);
        }
    }

    public void RenderOnOverlay(SomnoOverlay overlay)
    {
        if (!Enabled)
            return;

        var player = GameManager.CurrentPlayer;
        var ppos = player.Position;
        var maxDistSqr = MaxDistance * MaxDistance;

        var screenCenter = ImGui.GetIO().DisplaySize / 2;
        var render = ImGui.GetBackgroundDrawList();

        var playerYaw = MathS.Deg2Rad(-Camera.ViewAngles.Y);

        int i = 0;
        foreach (var enemy in GameManager.Players) {
            if (!enemy.Dormant || enemy.Team == player.Team || enemy.Health == 0f)
                continue;

            var epos = enemy.Position;

            float distSqr = Vector3.DistanceSquared(ppos, epos);
            if (distSqr > maxDistSqr)
                continue; // too far away

            // Calculate enemy position on the ring
            var direction = new Vector2(epos.X - ppos.X, epos.Y - ppos.Y); // 2D direction on the XY plane

            float rotatedX = (direction.X * MathF.Cos(playerYaw)) - direction.Y * MathF.Sin(playerYaw);
            float rotatedY = (direction.X * MathF.Sin(playerYaw)) + direction.Y * MathF.Cos(playerYaw);

            float angle = MathF.Atan2(rotatedY, rotatedX);
            //angle %= 2 * MathF.PI;

            if (angle is > -(MathF.PI / 2) and < (MathF.PI / 2)) {
                continue;
            }

            float displayAngle = angle + ((3f / 2f) * MathF.PI);
            displayAngle %= 2 * MathF.PI;

            float normalizedDistance = MathF.Sqrt(distSqr) / MaxDistance;
            float opacity = 1.0f - normalizedDistance; // Opacity decreases with distance

            // Calculate position on the ring based on the angle
            var ringPosition = screenCenter + (new Vector2(MathF.Cos(displayAngle) * -1, MathF.Sin(displayAngle))) * RingSize;
        
            render.AddCircleFilled(
                ringPosition,
                MarkerSize,
                ImGui.ColorConvertFloat4ToU32(new(1f, 0f, 0f, opacity))
            );
        }
    }
}
