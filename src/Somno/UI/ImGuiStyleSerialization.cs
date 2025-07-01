using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Somno.LanguageExtensions;

namespace Somno.UI
{
    internal static class ImGuiStyleSerialization
    {
        public static byte[] Save()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            var s = ImGui.GetStyle();

            bw.Write("Somno Theme File\n\n");
            WriteAttributes(bw, s);
            WriteColors(bw, s);

            return ms.ToArray();
        }

        public static void Load(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            using var br = new BinaryReader(ms);
            var s = ImGui.GetStyle();

            var signature = br.ReadString();
            if(signature != "Somno Theme File\n\n") {
                throw new InvalidOperationException($"Invalid signature '{signature}'.");
            }

            ReadAttributes(br, s);
            ReadColors(br, s);
        }

        static void WriteAttributes(BinaryWriter bw, ImGuiStylePtr s)
        {
            bw.Write(s.Alpha);
            bw.Write(s.AntiAliasedFill);
            bw.Write(s.AntiAliasedLines);
            bw.Write(s.AntiAliasedLinesUseTex);
            bw.Write(s.ButtonTextAlign);
            bw.Write(s.CellPadding);
            bw.Write(s.ChildBorderSize);
            bw.Write(s.ChildRounding);
            bw.Write(s.CircleTessellationMaxError);
            bw.Write(s.ColumnsMinSpacing);
            bw.Write(s.CurveTessellationTol);
            bw.Write(s.DisabledAlpha);
            bw.Write(s.DisplaySafeAreaPadding);
            bw.Write(s.DisplayWindowPadding);
            bw.Write(s.FrameBorderSize);
            bw.Write(s.FramePadding);
            bw.Write(s.FrameRounding);
            bw.Write(s.GrabMinSize);
            bw.Write(s.GrabRounding);
            bw.Write(s.TabRounding);
            bw.Write(s.IndentSpacing);
            bw.Write(s.ItemInnerSpacing);
            bw.Write(s.ItemSpacing);
            bw.Write(s.LogSliderDeadzone);
            bw.Write(s.MouseCursorScale);
            bw.Write(s.PopupBorderSize);
            bw.Write(s.PopupRounding);
            bw.Write(s.ScrollbarRounding);
            bw.Write(s.ScrollbarSize);
            bw.Write(s.SelectableTextAlign);
            bw.Write(s.SeparatorTextAlign);
            bw.Write(s.SeparatorTextBorderSize);
            bw.Write(s.SeparatorTextPadding);
            bw.Write(s.TabBorderSize);
            bw.Write(s.TabMinWidthForCloseButton);
            bw.Write(s.TabRounding);
            bw.Write(s.TouchExtraPadding);
            bw.Write(s.WindowBorderSize);
            bw.Write(s.WindowMinSize);
            bw.Write(s.WindowPadding);
            bw.Write(s.WindowRounding);
            bw.Write(s.WindowTitleAlign);
        }

        static void ReadAttributes(BinaryReader br, ImGuiStylePtr s)
        {
            s.Alpha = br.ReadSingle();
            s.AntiAliasedFill = br.ReadBoolean();
            s.AntiAliasedLines = br.ReadBoolean();
            s.AntiAliasedLinesUseTex = br.ReadBoolean();
            s.ButtonTextAlign = br.ReadVector2();
            s.CellPadding = br.ReadVector2();
            s.ChildBorderSize = br.ReadSingle();
            s.ChildRounding = br.ReadSingle();
            s.CircleTessellationMaxError = br.ReadSingle();
            s.ColumnsMinSpacing = br.ReadSingle();
            s.CurveTessellationTol = br.ReadSingle();
            s.DisabledAlpha = br.ReadSingle();
            s.DisplaySafeAreaPadding = br.ReadVector2();
            s.DisplayWindowPadding = br.ReadVector2();
            s.FrameBorderSize = br.ReadSingle();
            s.FramePadding = br.ReadVector2();
            s.FrameRounding = br.ReadSingle();
            s.GrabMinSize = br.ReadSingle();
            s.GrabRounding = br.ReadSingle();
            s.TabRounding = br.ReadSingle();
            s.IndentSpacing = br.ReadSingle();
            s.ItemInnerSpacing = br.ReadVector2();
            s.ItemSpacing = br.ReadVector2();
            s.LogSliderDeadzone = br.ReadSingle();
            s.MouseCursorScale = br.ReadSingle();
            s.PopupBorderSize = br.ReadSingle();
            s.PopupRounding = br.ReadSingle();
            s.ScrollbarRounding = br.ReadSingle();
            s.ScrollbarSize = br.ReadSingle();
            s.SelectableTextAlign = br.ReadVector2();
            s.SeparatorTextAlign = br.ReadVector2();
            s.SeparatorTextBorderSize = br.ReadSingle();
            s.SeparatorTextPadding = br.ReadVector2();
            s.TabBorderSize = br.ReadSingle();
            s.TabMinWidthForCloseButton = br.ReadSingle();
            s.TabRounding = br.ReadSingle();
            s.TouchExtraPadding = br.ReadVector2();
            s.WindowBorderSize = br.ReadSingle();
            s.WindowMinSize = br.ReadVector2();
            s.WindowPadding = br.ReadVector2();
            s.WindowRounding = br.ReadSingle();
            s.WindowTitleAlign = br.ReadVector2();
        }

        static void WriteColors(BinaryWriter bw, ImGuiStylePtr s)
        {
            bw.Write(s.Colors.Count);

            for (int i = 0; i < s.Colors.Count; i++) {
                bw.Write(s.Colors[i]);
            }
        }

        static void ReadColors(BinaryReader br, ImGuiStylePtr s)
        {
            int colors = br.ReadInt32();

            for (int i = 0; i < Math.Min(colors, s.Colors.Count); i++) {
                s.Colors[i] = br.ReadVector4();
            }
        }
    }
}
