using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno.UI
{
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
}
