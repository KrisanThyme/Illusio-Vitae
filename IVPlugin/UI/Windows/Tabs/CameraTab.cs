using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using IVPlugin.Cutscene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.UI.Windows.Tabs
{
    public static class CameraTab
    {
        public static void Draw()
        {
            ImGui.Text("Camera Settings");

            ImGui.InputFloat3("Scale", ref IllusioCutsceneManager.Instance.CameraSettings.Scale);
            ImGui.InputFloat3("Offset", ref IllusioCutsceneManager.Instance.CameraSettings.Offset);
            ImGui.Checkbox("Loop", ref IllusioCutsceneManager.Instance.CameraSettings.Loop);

            using (ImRaii.Disabled(!IllusioCutsceneManager.Instance.IsRunning))
            {
                if (ImGui.Button("Force Stop"))
                {
                    IllusioCutsceneManager.Instance.StopPlayback();
                }
            }

            ImGui.EndTabItem();
        }
    }
}
