using ImGuiNET;
using Somno.Native.WinUSER;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Somno.LanguageExtensions;
using Somno.Game;

namespace Somno.UI
{
    internal class ConfigurationGUI : IOverlayRenderable
    {
        readonly nint kbHookId;

        bool hidden = false;
        bool hideFromScreenCapture;
        bool firstFrame = true;
        bool showStyleEditor = false;

        public bool OverlayRenderDependsOnGame => false;

        /// <summary>
        /// The configuration modules that render to the overlay.
        /// </summary>
        public readonly List<IConfigRenderable> Modules = new();

        /// <summary>
        /// The main instance of the configuration GUI module.
        /// </summary>
        public static ConfigurationGUI? Instance { get; set; }

        private ConfigurationGUI() {
            kbHookId = Keyboard.SetHook(KeyboardHook);
        }

        /// <summary>
        /// Creates the main configuration GUI.
        /// </summary>
        public static void Create()
        {
            if (Instance != null)
                throw new InvalidOperationException("Cannot create more than one configuration GUI instance.");

            if (SomnoOverlay.Instance == null)
                throw new InvalidOperationException("The main overlay hasn't been initialized yet.");

            Instance = new();
            SomnoOverlay.Instance.AddModule(Instance);
        }

        public void AddConfigurable(IConfigRenderable module)
        {
            lock (Modules) {
                Modules.Add(module);
            }
        }

        static IntPtr KeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == 0x0100) {
                int vkCode = Marshal.ReadInt32(lParam);

                if (vkCode == 0x24) { // HOME
                    Instance!.hidden = !Instance.hidden;
                }
            }

            return User32.CallNextHookEx(Instance!.kbHookId, nCode, wParam, lParam);
        }

        public void RenderOnOverlay(SomnoOverlay overlay)
        {
            if(firstFrame) {
                ImGui.SetNextWindowSize(new(600, 300));
                LoadStyle();
                firstFrame = false;
            }

            if (hidden)
                return;

            ImGui.Begin("Somno", ImGuiWindowFlags.NoNavInputs);

            var prevPos = ImGui.GetCursorPosX();
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 30);

            if (ImGui.Button("X")) {
                hidden = true;
            }

            ImGui.SetCursorPosX(prevPos);

            ImGui.SameLine(8);
            ImGui.Checkbox("Hide from screen capture", ref hideFromScreenCapture);

            if (hideFromScreenCapture)
                overlay.Host.HideFromScreenCapture();
            else
                overlay.Host.ShowOnScreenCapture();

            if (GameManager.Running) ImGui.BeginDisabled(true);

            if (ImGui.Button("Eject")) {
                SomnoMain.Exit();
            }

            if (GameManager.Running) {
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("You cannot eject Somno because the game is running. Close it, alongside any launchers, in order to safely eject.");

                ImGui.EndDisabled();

                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1));
                if (ImGui.Button("Force Eject"))
                    SomnoMain.Exit();
                ImGui.PopStyleColor();
            }

            ImGui.SameLine();
            if (SomnoMain.ConsoleVisible) {
                if (ImGui.Button("Hide Console")) {
                    SomnoMain.HideConsole();
                }
            } else {
                if (ImGui.Button("Show Console")) {
                    SomnoMain.ShowConsole();
                }
            }

            ImGui.SameLine();
            if(ImGui.Button("Theme Editor")) {
                showStyleEditor = true;
            }

            if (!GameManager.Running) {
                ImGui.Text("Waiting for the game process to start...");
            } else if (!GameManager.ConnectedToServer) {
                ImGui.Text("Waiting for a match to start...");
            } else if (!GameManager.PlayerSpawned) {
                ImGui.Text("Awaiting player spawn...");
            }

            if (showStyleEditor) {
                ImGui.Begin("Theme Editor", ImGuiWindowFlags.NoMove);
                ImGui.SetWindowSize(new(500, overlay.HostWindow.Dimensions.Height));
                ImGui.SetWindowPos(new(overlay.HostWindow.Dimensions.Width - 500, 0));
                
                if(ImGui.Button("Close")) showStyleEditor = false;

                ImGui.SameLine();
                if (ImGui.Button("Save")) SaveStyle();

                ImGui.Separator();
                ImGui.ShowStyleEditor();
                ImGui.End();
            }

            ImGui.SeparatorText("Modifications");
            foreach (var module in Modules) {
                module.RenderConfiguration(overlay);
            }

            ImGui.End();
        }

        static void SaveStyle()
        {
            File.WriteAllBytes("./config/theme.dat", ImGuiStyleSerialization.Save());
        }

        static void LoadStyle()
        {
            if (File.Exists("./config/theme.dat")) {
                var bytes = File.ReadAllBytes("./config/theme.dat");
                ImGuiStyleSerialization.Load(bytes);
            }
        }

        public void OnOverlayDestroy()
        {
            Keyboard.Unhook(kbHookId);
        }
    }
}
