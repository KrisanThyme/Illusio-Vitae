using ImGuiNET;
using IVPlugin.Mods;
using IVPlugin.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentPartyMember.Delegates;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.GroupPoseModule;

namespace IVPlugin.UI.Windows
{
    public static class FirstTimeWindow
    {
        public static bool IsOpen = false;
        public static void Show() => IsOpen = true;
        public static void Toggle() => IsOpen = !IsOpen;

        public static bool doOnce = false;

        public static void Draw()
        {
            if (!IsOpen)
            {
                doOnce = true;
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(500, 0), ImGuiCond.FirstUseEver);

            ImGui.SetNextWindowSizeConstraints(new Vector2(500, 100), new Vector2(700, 150));

            if (doOnce)
            {
                ImGui.SetNextWindowPos(new Vector2((ImGui.GetIO().DisplaySize.X / 2) - (500 / 2), (ImGui.GetIO().DisplaySize.Y / 2) - (125 / 2)));
            }
            

            if (ImGui.Begin($"Full Transparency", ref IsOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.SetCursorPosX((500 / 2) - (ImGui.CalcTextSize("Readme!").X / 2 + 22));

                BearGUI.Text("Readme!", 1.5f, 0xFF0000FF);

                ImGui.Spacing();

                ImGui.Text("A lot of animation mods requires IVCS to be installed to function properly");
                ImGui.SetCursorPosX((500 / 2) - (ImGui.CalcTextSize("Would you like us to install it in your Penumbra for you?").X / 2));
                ImGui.Text("Would you like us to install it in your Penumbra for you?");

                ImGui.SetCursorPos(new((500 / 2) - ((ImGui.CalcTextSize("Yes").X/2) + (ImGui.CalcTextSize("No").X / 2)) - 10, 95));

                if (ImGui.Button("Yes"))
                {
                    ModManager.Instance.InstallIVCSMod();
                    IllusioVitae.configuration.firstTimeCheck = false;
                    IsOpen = false;
                }

                ImGui.SameLine();

                if(ImGui.Button("No"))
                {
                    IllusioVitae.configuration.firstTimeCheck = false;
                    IsOpen = false;
                }
            }
        }
    }
}
