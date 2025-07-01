using ImGuiNET;

namespace Somno.UI;

internal static class ImGuiHelpers
{
    public const ImGuiWindowFlags RenderSurface =
        ImGuiWindowFlags.NoTitleBar |
        ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoInputs |
        ImGuiWindowFlags.NoBackground;
}
