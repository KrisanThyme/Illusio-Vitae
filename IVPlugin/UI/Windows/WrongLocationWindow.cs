using ImGuiNET;
using IVPlugin.Actors.Structs;
using IVPlugin.Json;
using IVPlugin.Log;
using IVPlugin.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.UI.Windows
{
    public static class WrongLocationWindow
    {
        public static bool IsOpen = false;
        public static void Show(ActorScene scene)
        {
            cScene = scene;
            IsOpen = true;
        }
        public static void Toggle() => IsOpen = !IsOpen;
        public static bool showWarning = true;

        private static ActorScene cScene;

        private static bool doOnce;

        public static void Draw()
        {
            if (!IsOpen)
            {
                doOnce = true;
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(395, 0), ImGuiCond.FirstUseEver);

            ImGui.SetNextWindowSizeConstraints(new Vector2(395, 230), new Vector2(500, 300));

            if (doOnce)
            {
                ImGui.SetNextWindowPos(new Vector2((ImGui.GetIO().DisplaySize.X / 2) - (395 / 2), (ImGui.GetIO().DisplaySize.Y / 2) - (150 / 2)));
                doOnce = false;
            }
            

            showWarning = !IllusioVitae.configuration.ActorSceneWarningShow;

            if (ImGui.Begin($"Warning", ref IsOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Spacing();

                ImGui.SetCursorPosX(28);
                ImGui.Text("You are currently loading this scene using World Space,");
                ImGui.SetCursorPosX(25);
                ImGui.Text("but are in an area different from the one in which it was");
                ImGui.SetCursorPosX(28);
                ImGui.Text("created. Your position may not be desirable should you");
                ImGui.SetCursorPosX(11);
                ImGui.Text("continue. For more predictable results when loading scenes,");
                ImGui.SetCursorPosX(35);
                ImGui.Text("please choose \"Local Space\" from the Settings Menu.");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.SetCursorPosX(88);
                if (ImGui.Checkbox("Don't Show this Message Again##aActorwarning", ref showWarning))
                {
                    IllusioVitae.configuration.ActorSceneWarningShow = !showWarning;
                }

                ImGui.Spacing();

                ImGui.SetCursorPosX(83);
                if (ImGui.Button("Continue Loading with World Space"))
                {
                    if (DalamudServices.clientState.IsGPosing || IllusioVitae.InDebug())
                    {
                        cScene.LoadScene();
                    }
                    Toggle();
                }

                ImGui.SetCursorPosX(88);
                if (ImGui.Button("Cancel Loading and Close Window"))
                {
                    Toggle();
                }
            }
        }
    }
}
