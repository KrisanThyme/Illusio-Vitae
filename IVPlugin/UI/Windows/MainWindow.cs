using System.Numerics;
using ImGuiNET;
using IVPlugin.UI.Windows.Tabs;
using IVPlugin.Services;
using System.Drawing;
using IVPlugin.UI.Helpers;
using System.Runtime.CompilerServices;
using IVPlugin.Core;
using IVPlugin.UI;
using Dalamud.Interface.Utility.Raii;
using IVPlugin.UI.Windows;
using IVPlugin.ActorData;

namespace IVPlugin.Windows;

public static class MainWindow
{
    public static bool IsOpen = false;
    public static void Show() => IsOpen = true;
    public static void Toggle() => IsOpen = !IsOpen;
    
    private static bool LogLock = false;

    public static void Draw()
    {
        if (!IsOpen) return;

        if(IllusioVitae.configuration.firstTimeCheck)
        {
            FirstTimeWindow.Show();
            return;
        }

        ChangeLogWindow.TryShow();

        var size = new Vector2(-1, -1);
        ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

        ImGui.SetNextWindowSizeConstraints(new Vector2(100, 100), new Vector2(600, 1200));

        if (ImGui.Begin($"Illusio Vitae", ref IsOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if (ImGui.Button("Open Configuration Settings"))
            {
                ConfigWindow.Show();
            }

            if (!DalamudServices.penumbraServices.CheckAvailablity())
            {
                ImGui.TextColored(IVColors.Red, "Penumbra Unavailable/Version Mismatch");
            }

            ImGui.Spacing();

            if (!EventManager.validCheck)
            {
                ImGui.TextColored(IVColors.Red, "Limited Mode (Player is Busy or not in a Safe Area)");

                SkeletonOverlay.Hide();

                ImGui.Spacing();
            }

            if (IllusioVitae.configuration.ModLocation != string.Empty)
            {
                if (ImGui.BeginTabBar("Menu"))
                {
                    using (ImRaii.Disabled((!ActorManager.Instance.playerActor.IsLoaded(true) && !IllusioVitae.InDebug()) && !DalamudServices.clientState.IsGPosing))
                    {
                        using (ImRaii.Disabled(!DalamudServices.penumbraServices.CheckAvailablity()))
                        {
                            if (ImGui.BeginTabItem("Custom Emotes"))
                            CustomdataTab.Draw();
                        }

                        if (ImGui.BeginTabItem("Concept Matrix"))
                            ActorTab.Draw();

                        if (!EventManager.validCheck)
                        {
                            ImGui.BeginDisabled();
                        }
                        if (ImGui.BeginTabItem("World Control"))
                            WorldTab.Draw();

                        if (!EventManager.validCheck)
                        {
                            ImGui.EndDisabled();
                        }

                    }

                    if (IllusioVitae.IsDebug)
                    {
                        if (ImGui.BeginTabItem("Debug"))
                            DebugTab.Draw();
                    }


                    ImGui.EndTabBar();
                }
            }
            else
            {
                ImGui.Text("Please Input an Installation Directory.");
                ConfigWindow.Show();
            }
        }
    }
}
