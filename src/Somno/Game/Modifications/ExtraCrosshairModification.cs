using ImGuiNET;
using Somno.Game.Serialization;
using Somno.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Game.Modifications
{
    internal class ExtraCrosshairModification : GameModification, IOverlayRenderable, IConfigRenderable
    {
        public bool Enabled;
        public bool EnableOnSnipers = true;
        public bool EnableOnAutoSnipers = true;
        public Vector3 Color = new(0, 1, 0);
        public Vector3 OutlineColor = new(0, 0, 0);
        public int Size = 6;
        public int OutlineSize = 2;

        public bool OverlayRenderDependsOnGame => true;

        public ExtraCrosshairModification()
        {
            if (ConfigDeserializer.Exists("extra-crosshair")) {
                ConfigDeserializer.Deserialize("extra-crosshair")
                    .ReadBool(x => Enabled = x)
                    .ReadBool(x => EnableOnAutoSnipers = x)
                    .ReadBool(x => EnableOnSnipers = x)
                    .ReadVector3(x => Color = x)
                    .ReadVector3(x => OutlineColor = x)
                    .ReadInt32(x => Size = x)
                    .ReadInt32(x => OutlineSize = x)
                    .Finish();
            }
        }

        public void RenderConfiguration(SomnoOverlay overlay)
        {
            if (ImGui.CollapsingHeader("Extra Crosshair")) {
                ImGui.Text("Renders additional crosshairs.");
                ImGui.Indent(12);

                ImGui.Checkbox("Enabled", ref Enabled);

                ImGui.Checkbox("Enabled for auto-snipers", ref EnableOnAutoSnipers);
                ImGui.Checkbox("Enabled for snipers", ref EnableOnSnipers);
                ImGui.ColorEdit3("Color", ref Color);
                ImGui.ColorEdit3("Outline Color", ref OutlineColor);
                ImGui.SliderInt("Size", ref Size, 1, 32);
                ImGui.SliderInt("Outline Size", ref OutlineSize, 1, 32);

                if (ImGui.Button("Save configuration")) {
                    ConfigSerializer.Serialize("extra-crosshair")
                        .Write(Enabled)
                        .Write(EnableOnAutoSnipers)
                        .Write(EnableOnSnipers)
                        .Write(Color)
                        .Write(OutlineColor)
                        .Write(Size)
                        .Write(OutlineSize)
                        .Finish();
                }

                ImGui.Unindent(12);
            }
        }

        public void RenderOnOverlay(SomnoOverlay overlay)
        {
            if (!Enabled)
                return;

            switch (GameManager.CurrentPlayer.Weapon) {
                case WeaponType.Scar20:
                case WeaponType.G3SG1:
                    if (EnableOnAutoSnipers && !GameManager.CurrentPlayer.IsScoped)
                        RenderCrosshair(overlay);
                    break;
                case WeaponType.AWP:
                case WeaponType.SSG08:
                    if (EnableOnSnipers && !GameManager.CurrentPlayer.IsScoped)
                        RenderCrosshair(overlay);
                    break;
            }
        }

        void RenderCrosshair(SomnoOverlay overlay)
        {
            var overlaySize = overlay.HostWindow.Dimensions;

            ImGui.SetNextWindowContentSize(new(overlaySize.Width, overlaySize.Height));
            ImGui.SetNextWindowPos(new(0, 0));
            ImGui.Begin("(Crosshair Overlay)", ImGuiHelpers.RenderSurface);

            var middle = new Vector2(overlaySize.Width / 2, overlaySize.Height / 2);

            int sizeHalf = Size / 2;
            int outlineTotalHalf = (Size + OutlineSize) / 2;
            var bodyOffset = new Vector2(sizeHalf, sizeHalf);
            var outlineOffset = new Vector2(outlineTotalHalf, outlineTotalHalf);

            var render = ImGui.GetWindowDrawList();
            render.AddRectFilled(
                middle - outlineOffset,
                middle + outlineOffset,
                ImGui.ColorConvertFloat4ToU32(new(OutlineColor, 1))
            );

            render.AddRectFilled(
                middle - bodyOffset,
                middle + bodyOffset,
                ImGui.ColorConvertFloat4ToU32(new(Color, 1))
            );

            ImGui.End();
        }
    }
}
