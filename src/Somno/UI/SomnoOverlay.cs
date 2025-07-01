using Somno.UI.Engine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Somno.Game;
using System.Runtime.Versioning;

namespace Somno.UI;

[SupportedOSPlatform("windows")]
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
        try {
            GameManager.Update();
        } catch (GameClosedException) {
            Orchestrator.ChangeState(EngineState.WaitingForProcess);
        }

        lock (Modules) {
            foreach (var module in Modules) {
                if(module.OverlayRenderDependsOnGame) {
                    if (!GameManager.Playing)
                        continue;

                    try {
                        module.RenderOnOverlay(this);
                    }
                    catch (GameClosedException) {
                        Orchestrator.ChangeState(EngineState.WaitingForProcess);
                    }
                } else {
                    module.RenderOnOverlay(this);
                }
            }
        }
    }

    public void AddModule(IOverlayRenderable module)
    {
        lock (Modules) {
            Modules.Add(module);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) {
            foreach (var module in Modules) {
                module.OnOverlayDestroy();
            }
        }

        base.Dispose(disposing);
    }
}
