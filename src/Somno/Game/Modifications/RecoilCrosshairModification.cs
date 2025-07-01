using ImGuiNET;
using Somno.Game.Serialization;
using Somno.UI;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace Somno.Game.Modifications;

internal class RecoilCrosshairModification : GameModification, IOverlayRenderable, IConfigRenderable
{
    public bool Enabled;
    public Vector4 Color = new(0.25f, 0.25f, 0.25f, 0.5f);
    public Vector4 OutlineColor = new(0, 0, 0, 0.5f);
    public int Size = 6;
    public int OutlineSize = 2;

    public bool OverlayRenderDependsOnGame => true;

    public RecoilCrosshairModification()
    {
        if (ConfigDeserializer.Exists("recoil-crosshair")) {
            ConfigDeserializer.Deserialize("recoil-crosshair")
                .ReadBool(x => Enabled = x)
                .ReadVector4(x => Color = x)
                .ReadVector4(x => OutlineColor = x)
                .ReadInt32(x => Size = x)
                .ReadInt32(x => OutlineSize = x)
                .Finish();
        }
    }

    public void RenderConfiguration(SomnoOverlay overlay)
    {
        if (ImGui.CollapsingHeader("Recoil Crosshair")) {
            ImGui.Text("Renders an additional crosshair, which shows the landing position of bullets.");
            ImGui.Indent(12);

            ImGui.Checkbox("Enabled", ref Enabled);

            ImGui.ColorEdit4("Color", ref Color);
            ImGui.ColorEdit4("Outline Color", ref OutlineColor);
            ImGui.SliderInt("Size", ref Size, 1, 32);
            ImGui.SliderInt("Outline Size", ref OutlineSize, 1, 32);

            if (ImGui.Button("Save configuration")) {
                ConfigSerializer.Serialize("recoil-crosshair")
                    .Write(Enabled)
                    .Write(Color)
                    .Write(OutlineColor)
                    .Write(Size)
                    .Write(OutlineSize)
                    .Finish();
            }

            ImGui.Unindent(12);
        }
    }

    static readonly HashSet<WeaponType> automaticWeapons = new() {
        WeaponType.AK47,
        WeaponType.AUG,
        WeaponType.M4A4,
        WeaponType.M4A1S,
        WeaponType.MP9,
        WeaponType.G3SG1,
        WeaponType.CZ75Auto,
        WeaponType.Scar20,
        WeaponType.FAMAS,
        WeaponType.GalilAR,
        WeaponType.M249,
        WeaponType.MAC10,
        WeaponType.MP5SD,
        WeaponType.MP7,
        WeaponType.Negev,
        WeaponType.P90,
        WeaponType.PPBizon,
        WeaponType.UMP45
    };

    public void RenderOnOverlay(SomnoOverlay overlay)
    {
        if (!Enabled)
            return;

        if (automaticWeapons.Contains(GameManager.CurrentPlayer.Weapon)) {
            if (GameManager.CurrentPlayer.ShotsFired <= 1)
                return;

            var overlaySize = overlay.HostWindow.Dimensions;

            var recoil = GameManager.CurrentPlayer.RecoilAngle;

            float x = overlaySize.Width / 2f;
            float y = overlaySize.Height / 2f;
            float dy = overlaySize.Height / 90f;
            float dx = overlaySize.Width / 90f;
            x -= (dx * recoil.Y);
            y += (dy * recoil.X);

            RenderCrosshair(overlaySize, new(x, y));
        }
    }

    void RenderCrosshair(Rectangle overlaySize, Vector2 middle)
    {
        ImGui.SetNextWindowContentSize(new(overlaySize.Width, overlaySize.Height));
        ImGui.SetNextWindowPos(new(0, 0));
        ImGui.Begin("(Crosshair Overlay)", ImGuiHelpers.RenderSurface);

        int sizeHalf = Size / 2;
        int outlineTotalHalf = (Size + OutlineSize) / 2;
        var bodyOffset = new Vector2(sizeHalf, sizeHalf);
        var outlineOffset = new Vector2(outlineTotalHalf, outlineTotalHalf);

        var render = ImGui.GetWindowDrawList();
        render.AddRectFilled(
            middle - outlineOffset,
            middle + outlineOffset,
            ImGui.ColorConvertFloat4ToU32(OutlineColor)
        );

        render.AddRectFilled(
            middle - bodyOffset,
            middle + bodyOffset,
            ImGui.ColorConvertFloat4ToU32(Color)
        );

        ImGui.End();
    }
}
