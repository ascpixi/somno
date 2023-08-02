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
    internal class SomnoOverlay : ImGuiOverlay
    {
        /// <summary>
        /// The modules that render to the overlay.
        /// </summary>
        public readonly List<IOverlayRenderable> Modules = new();

        /// <summary>
        /// The main instance of the overlay.
        /// </summary>
        public static SomnoOverlay? Instance { get; set; }

        protected override Task PostInitialized()
        {
            Instance = this;

            this.VSync = false;
            return Task.CompletedTask;
        }

        protected override void Render()
        {
            foreach (var module in Modules) {
                module.RenderOnOverlay(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing) {
                foreach (var module in Modules) {
                    module.OnOverlayDestroy();
                }
            }

            base.Dispose(disposing);
        }
    }
}
