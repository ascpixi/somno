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

namespace Somno.UI
{
    [SupportedOSPlatform("windows")]
    internal class ConfigurationGUI : IOverlayRenderable
    {
        nint kbHookId;
        bool hidden = false;
        bool hideFromScreenCapture;
        bool firstFrame = true;
        bool showStyleEditor = false;

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
            SomnoOverlay.Instance.Modules.Add(Instance);
        }

        static IntPtr KeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == 0x0100) {
                int vkCode = Marshal.ReadInt32(lParam);

                if (vkCode == 0x24) { // HOME
                    Instance!.hidden = !Instance.hidden;
                    if (!Instance.hidden) {
                        User32.SwitchToThisWindow(
                            SomnoOverlay.Instance!.Host.WindowHandle,
                            false
                        );
                    }
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

            bool cantExit = SomnoMain.MainPortal != null &&
                !SomnoMain.MainPortal.TargetProcess.HasExited;

            if (cantExit) {
                ImGui.BeginDisabled(true);
            }

            if (ImGui.Button("Eject")) {
                SomnoMain.Exit();
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

            if (cantExit) {
                if(ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("You cannot eject Somno because the game is running. Close it, alongside any launchers, in order to safely eject.");
                }

                ImGui.EndDisabled();
            }

            ImGui.SameLine();
            if(ImGui.Button("Theme Editor")) {
                showStyleEditor = true;
            }

            if(showStyleEditor) {
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
            File.WriteAllBytes("theme.dat", ImGuiStyleSerialization.Save());
        }

        static void LoadStyle()
        {
            if (File.Exists("theme.dat")) {
                var bytes = File.ReadAllBytes("theme.dat");
                ImGuiStyleSerialization.Load(bytes);
            }
        }

        public void OnOverlayDestroy()
        {
            Keyboard.Unhook(kbHookId);
        }
    }
}
