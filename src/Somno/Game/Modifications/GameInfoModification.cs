using ImGuiNET;
using Somno.Game.Serialization;
using Somno.UI;
using System.Numerics;
using System.Text;

namespace Somno.Game.Modifications;

internal class GameInfoModification : GameModification, IOverlayRenderable, IConfigRenderable
{
    public bool Enabled;
    public bool DisplayPlantBombsite;

    public bool OverlayRenderDependsOnGame => true;

    public GameInfoModification()
    {
        if (ConfigDeserializer.Exists("game-info")) {
            ConfigDeserializer.Deserialize("game-info")
                .ReadBool(x => Enabled = x)
                .ReadBool(x => DisplayPlantBombsite = x)
                .Finish();
        }
    }

    public void RenderConfiguration(SomnoOverlay overlay)
    {
        if (ImGui.CollapsingHeader("Game Information")) {
            ImGui.Text("Displays additional information.");
            ImGui.Indent(12);

            ImGui.Checkbox("Enabled", ref Enabled);

            ImGui.Checkbox("Display plant bomb-site", ref DisplayPlantBombsite);

            if (ImGui.Button("Save configuration")) {
                ConfigSerializer.Serialize("game-info")
                    .Write(Enabled)
                    .Write(DisplayPlantBombsite)
                    .Finish();
            }

            ImGui.Unindent(12);
        }
    }

    public void RenderOnOverlay(SomnoOverlay overlay)
    {
        if (!Enabled)
            return;

        var overlaySize = overlay.HostWindow.Dimensions;

        ImGui.SetNextWindowContentSize(new(overlaySize.Width, overlaySize.Height));
        ImGui.SetNextWindowPos(new(0, 0));
        ImGui.Begin("(Game Info Overlay)", ImGuiHelpers.RenderSurface);

        var render = ImGui.GetWindowDrawList();

        var display = new StringBuilder();

        if (DisplayPlantBombsite && GameManager.BombPlanted) {
            display.AppendLine($"Bomb-site: {(GameManager.Bomb.BombSite == 0 ? 'A' : 'B')}");
            display.Append($"Explodes in: {GameManager.Bomb.TimeToDetonation:0.00}s ");

            if(GameManager.Bomb.TimeToDetonation > 10) {
                display.AppendLine("(defusable)");
            } else if (GameManager.Bomb.TimeToDetonation > 5) {
                display.AppendLine("(defusable with kit)");
            } else {
                display.AppendLine("(cannot defuse)");
            }
        }

        if(display.Length != 0) {
            var displayBuilt = display.ToString();
            var textSize = ImGui.CalcTextSize(displayBuilt);

            render.AddRectFilled(
                new Vector2(0, 0),
                textSize + new Vector2(16, 16),
                ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0.5f))
            );

            render.AddText(new Vector2(8, 8), uint.MaxValue, display.ToString());
        }

        ImGui.End();
    }
}
