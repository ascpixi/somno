using Somno.UI.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Somno.Native.WinUSER;

namespace Somno.UI
{
    internal class ConfigurationGUI : ImGuiOverlay
    {
        public bool EnemyESP;

        protected override Task PostInitialized()
        {
            // Hide from any window/screen capture
            User32.SetWindowDisplayAffinity(Window.Handle, 0x00000011);

            this.VSync = false;
            return Task.CompletedTask;
        }

        protected override void Render()
        {
            ImGui.Checkbox("Enemy ESP", ref EnemyESP);
        }
    }
}
