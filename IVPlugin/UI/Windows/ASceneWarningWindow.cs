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
    public static class ASceneWarningWindow
    {
        public static bool IsOpen = false;
        public static void Show() => IsOpen = true;
        public static void Toggle() => IsOpen = !IsOpen;
        public static bool worldSpace, applyPlayerAppearance = false, useBaseOnly = false;

        public static bool doOnce = false;

        public static void Draw()
        {
            if (!IsOpen)
            {
                doOnce = true;
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(395, 0), ImGuiCond.FirstUseEver);

            ImGui.SetNextWindowSizeConstraints(new Vector2(395, 180), new Vector2(500, 350));

            if(doOnce)
            {
                ImGui.SetNextWindowPos(new Vector2((ImGui.GetIO().DisplaySize.X / 2) - (395 / 2), (ImGui.GetIO().DisplaySize.Y / 2) - (250 / 2)));
                doOnce = false;
            }

            worldSpace = !IllusioVitae.configuration.ActorSceneLocalSpace;

            if (ImGui.Begin($"Actor Scene Settings", ref IsOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Spacing();

                ImGui.SetCursorPosX(38);
                ImGui.Text("All Actors will be posed when loading Actor Scenes.");
                ImGui.SetCursorPosX(24);
                ImGui.Text("In order to restore Character Animation you will need to");
                ImGui.SetCursorPosX(22);
                ImGui.Text("either Reset Pose and Orientation in the Skeleton Editor,");
                ImGui.SetCursorPosX(21);
                ImGui.Text("or select Reload Character Data in the Appearance Editor.");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.SetCursorPosX(73);
                ImGui.Checkbox("Load Player Appearance from Scene", ref applyPlayerAppearance);
                ImGui.SetCursorPosX(73);
                ImGui.Checkbox("Ignore Player Head Pose from Scene", ref useBaseOnly);
                ImGui.Spacing();

                ImGui.SetCursorPosX(85);
                ImGui.Text("Load Scene Space: ");

                var buttonText = worldSpace ? "World Space" : "Local Space";

                ImGui.SameLine();

                if (ImGui.Button(buttonText, new(100,22)))
                {
                    IllusioVitae.configuration.ActorSceneLocalSpace = !IllusioVitae.configuration.ActorSceneLocalSpace;
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X/2 - 100);
                ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X / 2 - 50);

                if (ImGui.Button("Confirm Settings"))
                {
                    if (DalamudServices.clientState.IsGPosing || IllusioVitae.InDebug())
                    {
                        WindowsManager.Instance.fileDialogManager.OpenFileDialog("Open Actor Scene", ".ivscene", (Confirm, Path) =>
                        {
                            if (!Confirm) return;

                            if (DalamudServices.clientState.IsGPosing || IllusioVitae.InDebug())
                            {
                                var data = File.ReadAllText(Path);

                                try
                                {
                                    ActorScene scene = JsonHandler.Deserialize<ActorScene>(data);

                                    if (scene.location != DalamudServices.clientState.TerritoryType && worldSpace && IllusioVitae.configuration.ActorSceneWarningShow)
                                    {
                                        WrongLocationWindow.Show(scene);
                                    }
                                    else
                                    {
                                        scene.changePlayerAppearance = applyPlayerAppearance;
                                        scene.onlyUseBase = useBaseOnly;
                                        scene.LoadScene();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    IllusioDebug.Log("Failed to Read Actor Scene File", LogType.Error, false);
                                    return;
                                }
                            }
                        });
                    }

                    Toggle();
                }
            }
        }
    }
}
