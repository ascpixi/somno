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
    internal class ESPModification : IOverlayRenderable, IConfigRenderable
    {
        public bool Enabled;
        public bool HeadBox = true;

        public bool RenderTeam;
        public Vector3 TeamColor = new(0, 1, 0);

        public bool RenderEnemy = true;
        public Vector3 EnemyColor = new(1, 0, 0);

        public void RenderConfiguration(SomnoOverlay overlay)
        {
            if (ImGui.CollapsingHeader("ESP")) {
                ImGui.Text("Renders the positions of players through walls.");
                ImGui.Indent(12);

                ImGui.Checkbox("Enabled", ref Enabled);
                ImGui.Checkbox("Head Box", ref HeadBox);

                ImGui.Checkbox("Render Enemies", ref RenderEnemy);
                if (RenderEnemy) {
                    ImGui.Indent();
                    ImGui.ColorEdit3("Enemy Color", ref EnemyColor);
                    ImGui.Unindent();
                }

                ImGui.Checkbox("Render Teammates", ref RenderTeam);
                if (RenderTeam) {
                    ImGui.Indent();
                    ImGui.ColorEdit3("Team Color", ref TeamColor);
                    ImGui.Unindent();
                }

                ImGui.Unindent(12);
            }
        }

        public void RenderOnOverlay(SomnoOverlay overlay)
        {
            // ...
        }
    }
}
