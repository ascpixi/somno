using ImGuiNET;
using Somno.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Game.Modifications
{
    internal class TrajectoriesModification : IOverlayRenderable, IConfigRenderable
    {
        public bool Enabled;

        public void RenderConfiguration(SomnoOverlay overlay)
        {
            if (ImGui.CollapsingHeader("Trajectories")) {
                ImGui.Text("Renders the trajectories of grenades.");
                ImGui.Indent(12);

                ImGui.Checkbox("Enabled", ref Enabled);

                ImGui.Unindent(12);
            }
        }

        public void RenderOnOverlay(SomnoOverlay overlay)
        {
            // ...
        }
    }
}
