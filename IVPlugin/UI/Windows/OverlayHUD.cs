using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using IVPlugin.ActorData;
using IVPlugin.Resources;
using IVPlugin.UI.Helpers;
using IVPlugin.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.UI.Windows
{
    internal class OverlayHUD
    {
        public static bool IsOpen = false;
        public static void Show() => IsOpen = true;
        public static void Hide() => IsOpen = false;
        public static void Draw()
        {
            if (!IsOpen) return;

            ImGuiHelpers.ForceNextWindowMainViewport();

            var size = new Vector2(-1, -1);
            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

            ImGui.SetNextWindowSizeConstraints(new Vector2(0, 0), new Vector2(100, 25));

            if (ImGui.Begin("IllusioHUD Overlay", ImGuiWindowFlags.NoDecoration))
            {

                ImGui.Image(GameResourceManager.Instance.GetResourceImage("draggable.png").ImGuiHandle, new(20));

                ImGui.SameLine(30);

                if (BearGUI.FontButton("guiOpen", FontAwesomeIcon.Display.ToIconString()))
                {
                    MainWindow.Show();
                }
            }
        }
            
    }
}
