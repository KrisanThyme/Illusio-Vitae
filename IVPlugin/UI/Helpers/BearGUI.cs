using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.UI.Helpers
{
    public static class BearGUI
    {
        public static bool ImageButton(string idx, nint image, Vector2 size)
        {
            ImGui.BeginGroup();
            var pos = ImGui.GetCursorPos();

            var result = ImGui.Button($"##{idx}", size);

            ImGui.SetCursorPos(new(pos.X +2.5f, pos.Y+2.5f));

            ImGui.Image(image, new(size.X - 5, size.Y - 5));

            ImGui.EndGroup();

            return result;
        }

        public static bool FontButton(string idx, string icon, Vector2 size = new())
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(size == Vector2.Zero)
                    return ImGui.Button($"{icon}##idx");
                else
                    return ImGui.Button($"{icon}##idx", size);
            }
        }

        public static void FontText(string text, float fontScale = 1, uint textColor = 0xFFFFFFFF)
        {
            ImGui.GetFont().Scale = fontScale;

            ImGui.PushFont(ImGui.GetFont());

            ImGui.PushStyleColor(ImGuiCol.Text, textColor);

            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                ImGui.Text(text);
            }

            ImGui.GetFont().Scale = 1;

            ImGui.PopFont();

            ImGui.PopStyleColor(1);
        }

        public static void Text(string text, float fontScale = 1, uint textColor = 0xFFFFFFFF)
        {
            ImGui.GetFont().Scale = fontScale;

            ImGui.PushFont(ImGui.GetFont());

            ImGui.PushStyleColor(ImGuiCol.Text, textColor);

            ImGui.Text(text);

            ImGui.GetFont().Scale = 1;

            ImGui.PopFont();

            ImGui.PopStyleColor(1);
        }

        public static bool ColoredLableButton(string id, uint color, string label, bool HideToolTip = true, Vector2? size = null, float fontScale = 1)
        {

            Vector2 minButtonSize = new(ImGui.CalcTextSize("XXX").X + ImGui.GetStyle().FramePadding.X * 2, ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2);
            Vector2 desiredButtonSize = new(ImGui.CalcTextSize(label).X + ImGui.GetStyle().FramePadding.X * 2, ImGui.CalcTextSize(label).Y + ImGui.GetStyle().FramePadding.Y * 2);
            Vector2 buttonSize = size.HasValue ? size.Value : Vector2.Max(minButtonSize, desiredButtonSize);

            Vector2 textSize = ImGui.CalcTextSize(label);
            Vector2 initialPos = ImGui.GetCursorScreenPos();
            Vector2 textPos = initialPos + (buttonSize - textSize) * 0.5f;

            var flags = ImGuiColorEditFlags.None;

            if (HideToolTip) flags |= ImGuiColorEditFlags.NoTooltip;

            var result = ImGui.ColorButton(id, ImGui.ColorConvertU32ToFloat4(color), flags, buttonSize);

            var textColor = GUIMethods.CalculateLuminance(color) > 0.5 ? 0xff000000 : 0xffffffff;

            ImGui.GetFont().Scale = fontScale;

            ImGui.PushFont(ImGui.GetFont());

            ImGui.GetWindowDrawList().AddText(textPos, textColor, label);

            ImGui.GetFont().Scale = 1;

            ImGui.PopFont();

            return result;
        }
    }
}
