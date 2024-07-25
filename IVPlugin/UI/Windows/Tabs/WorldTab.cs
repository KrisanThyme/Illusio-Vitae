using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using ImGuiNET;
using IVPlugin.Camera;
using IVPlugin.Core.Extentions;
using IVPlugin.Core.Files;
using IVPlugin.Cutscene;
using IVPlugin.Gpose;
using IVPlugin.Log;
using IVPlugin.Resources;
using IVPlugin.Services;
using IVPlugin.UI.Helpers;
using IVPlugin.Windows;
using Lumina.Excel.GeneratedSheets;
using Lumina.Excel.GeneratedSheets2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace IVPlugin.UI.Windows.Tabs
{
    public static class WorldTab
    {
        private static string camPath = "";

        private static bool hideUI = false, useScale = true;
        public static void Draw()
        {
            bool timeLocked = WorldManager.Instance.IsTimeFrozen;
            int minuteOfDay = WorldManager.Instance.MinuteOfDay;
            int dayOfMonth = WorldManager.Instance.DayOfMonth;

            BearGUI.Text("Time and Weather Controller", 1.1f);
            ImGui.SetNextItemWidth(250);
            if (ImGui.SliderInt("Time of Day", ref minuteOfDay, 0, 1439, $"{TimeSpan.FromMinutes(minuteOfDay).Hours:D2}:{TimeSpan.FromMinutes(minuteOfDay).Minutes:D2}"))
            {
                WorldManager.Instance.IsTimeFrozen = true;
                WorldManager.Instance.MinuteOfDay = minuteOfDay;
            };

            ImGui.SameLine();
            ImGui.SetCursorPosX(366);

            var currentIcon = timeLocked ? GameResourceManager.Instance.GetResourceImage("Locked.png").ImGuiHandle : GameResourceManager.Instance.GetResourceImage("Unlocked.png").ImGuiHandle;

            if (BearGUI.ImageButton($"Lock Time##TimeLock", currentIcon, new(22, 22)))
            {
                WorldManager.Instance.IsTimeFrozen = !timeLocked;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Lock Time");
            }

            ImGui.SetNextItemWidth(250);
            if (ImGui.SliderInt("Day of Month", ref dayOfMonth, 1, 31))
            {
                WorldManager.Instance.IsTimeFrozen = true;
                WorldManager.Instance.DayOfMonth = dayOfMonth;
            };

            //ImGui.Separator();

            bool weatherLocked = WorldManager.Instance.WeatherOverrideEnabled;

            var mainWeather = GameResourceManager.Instance.Weathers[(uint)WorldManager.Instance.CurrentWeather];

            nint icon = 0;

            var wrapper = DalamudServices.textureProvider.GetFromGameIcon(new((uint)mainWeather.Icon)).GetWrapOrDefault();

            if (wrapper != null) icon = wrapper.ImGuiHandle;


            if (ImGui.Button($"##{mainWeather}weatherButton", new(256, 22)))
            {
                ImGui.OpenPopup("WeatherPopup");
            }

            ImGui.SameLine();

            ImGui.SetCursorPosX(16);

            ImGui.Image(icon, new(22, 22));
            ImGui.SameLine();

            ImGui.Text(mainWeather.Name);

            ImGui.SameLine();

            ImGui.SetCursorPosX(269);

            ImGui.Text("Weather");
            ImGui.SameLine();
            ImGui.SetCursorPosX(366);

            currentIcon = weatherLocked ? GameResourceManager.Instance.GetResourceImage("Locked.png").ImGuiHandle : GameResourceManager.Instance.GetResourceImage("Unlocked.png").ImGuiHandle;

            if (BearGUI.ImageButton($"Lock Weather##WeatherLock", currentIcon, new(22, 22)))
            {
                WorldManager.Instance.WeatherOverrideEnabled = !weatherLocked;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Lock Weather");
            }

            using (var popup = ImRaii.Popup("WeatherPopup"))
            {
                if (popup.Success)
                {
                    foreach (var weather in WorldManager.Instance.TerritoryWeatherTable)
                    {

                        var weatherWrapper = DalamudServices.textureProvider.GetFromGameIcon(new((uint)weather.Icon)).GetWrapOrDefault();

                        nint ic = 0;
                            
                        if(weatherWrapper != null) ic = weatherWrapper.ImGuiHandle;

                        if (ImGui.Button($"##{weather}weatherButton", new(256, 22)))
                        {
                            WorldManager.Instance.WeatherOverrideEnabled = true;
                            WorldManager.Instance.CurrentWeather = (int)weather.RowId;
                            ImGui.CloseCurrentPopup();
                        }

                        ImGui.SameLine();

                        ImGui.SetCursorPosX(16);

                        ImGui.Image(ic, new(22, 22));

                        ImGui.SameLine();

                        ImGui.Text(weather.Name);
                    }
                }
            }

            int skyid = (int)WorldManager.Instance.CurrentSky;

            ImGui.SetNextItemWidth(256);
            if(ImGui.InputInt("Skybox ID", ref skyid))
            {
                if(skyid < 0) skyid = 0;
                WorldManager.Instance.CurrentSky = (uint)skyid;
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(366);

            using (ImRaii.PushFont(UiBuilder.IconFont))
            {

                if (ImGui.Button($"{FontAwesomeIcon.Undo.ToIconString()}##undosky", new(22, 22)))
                {
                    WorldManager.Instance.resetSky();
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Reset Skybox");
            }

            ImGui.Separator();

            if (ImGui.CollapsingHeader("Camera Controls"))
            {
                using (ImRaii.Disabled(!DalamudServices.clientState.IsGPosing && !IllusioVitae.InDebug()))
                {
                    CameraDraw();
                }
            }

            if (ImGui.CollapsingHeader("Custom Emote Camera Controls"))
            {
                using (ImRaii.Disabled(!DalamudServices.clientState.IsGPosing))
                {
                    CCameraDraw();
                }
            }

            unsafe
            {
                if (IllusioVitae.InDebug())
                {
                    if (ImGui.CollapsingHeader("Debug"))
                    {
                        ImGui.Text($"Current Zone ID: {GameResourceManager.Instance.territories[DalamudServices.clientState.TerritoryType].Name.RawString}");
                        ImGui.Text("Currently Enabled Camera Type: " + XIVCamera.instance.GetCurrentCamera()->type.ToString());

                        var cameraID = ((nint)XIVCamera.instance.GetCurrentCamera()).ToString("X");

                        ImGui.SetNextItemWidth(100);
                        ImGui.InputText("Camera Address", ref cameraID, 100);

                        string envPtr = ((nint)EnvManager.Instance()).ToString("X");

                        ImGui.SetNextItemWidth(100);
                        ImGui.InputText("Environment Address", ref envPtr, 100);

                        var layerPtr = ((nint)LayoutWorld.Instance()).ToString("X");

                        ImGui.SetNextItemWidth(100);
                        ImGui.InputText("Layout Address", ref layerPtr, 100);
                    }
                }
            }
            


            ImGui.EndTabItem();
        }

        private unsafe static void CameraDraw()
        {
            var currentCam = XIVCamera.instance.GetCurrentCamera();

            float fov = currentCam->FoV * ExtentionMethods.RadiansToDegrees;
            Vector2 angle = currentCam->Angle;
            Vector2 pan = currentCam->Pan;
            float rotation = currentCam->Rotation * ExtentionMethods.RadiansToDegrees;
            float zoom = currentCam->Camera.Distance;
            float interp = currentCam->Camera.InterpDistance;

            BearGUI.Text("Camera Controller", 1.1f);

            ImGui.DragFloat3("Camera Offset", ref XIVCamera.instance.posOffset, .05f);

            if (ImGui.DragFloat("FOV", ref fov, .1f, -44, 120))
            {
                currentCam->FoV = fov * ExtentionMethods.DegreesToRadians;
            }

            if (ImGui.DragFloat("Rotation", ref rotation, 1, -180, 180))
            {
                currentCam->Rotation = rotation * ExtentionMethods.DegreesToRadians;
            }

            if (ImGui.DragFloat("Zoom", ref zoom, 1, currentCam->Camera.MinDistance, currentCam->Camera.MaxDistance))
            {
                currentCam->Camera.Distance = zoom;
            }

            if (ImGui.DragFloat2("Angle", ref angle, .01f))
            {
                if (currentCam->type == Camera.Struct.CameraType.Legacy)
                {
                    if (angle.X - currentCam->Angle.X > 0)
                    {
                        Vector2 tempAngle = new(angle.X + .079f, angle.Y);
                        angle = tempAngle;
                    }
                    else
                    {
                        Vector2 tempAngle = new(angle.X - .079f, angle.Y);
                        angle = tempAngle;
                    }

                }

                currentCam->Angle = angle;
            }

            if (ImGui.DragFloat2("Pan", ref pan, .01f))
            {
                currentCam->Pan = pan;
            }

            ImGui.Checkbox("Disable Collision", ref XIVCamera.instance.disableCollision);

            ImGui.SameLine();

            if(ImGui.Checkbox("Remove Zoom Limits", ref XIVCamera.instance.removeZoomLimits))
            {
                if (XIVCamera.instance.removeZoomLimits)
                {
                    XIVCamera.instance.RemoveZoomLimits();
                }
                else
                {
                    XIVCamera.instance.ReinstateZoomLimits();
                }
            }

            if (ImGui.Button("Reset Camera Controller"))
            {
                ResetCameraControls();
            }

            if (!DalamudServices.clientState.IsGPosing && !IllusioVitae.InDebug())
            {
                ImGui.TextColored(IVColors.Red, "Camera Control is Unavailable Outside of Group Pose");
            }
        }

        public unsafe static void ResetCameraControls()
        {
            var currentCam = XIVCamera.instance.GetCurrentCamera();

            currentCam->FoV = 0;
            XIVCamera.instance.posOffset = Vector3.Zero;
            currentCam->Rotation = 0;
            XIVCamera.instance.disableCollision = false;
            if (XIVCamera.instance.removeZoomLimits)
            {
                XIVCamera.instance.ReinstateZoomLimits();
            }
            XIVCamera.instance.forceCameraPositon = false;
        }

        private static void CCameraDraw()
        {
            BearGUI.Text("Emote Camera Controller", 1.1f);

            ImGui.InputText("Camera Path", ref camPath, 1000);

            if (ImGui.Button("Browse"))
            {
                WindowsManager.Instance.fileDialogManager.OpenFileDialog("Camera File", ".xcp", (Confirm, FilePath) =>
                {
                    if (!Confirm) return;

                    camPath = FilePath;
                });
            }

            ImGui.SameLine();

            if (ImGui.Button("Force Play"))
            {
                XATCameraPathFile camera;

                using (FileStream fs = File.Open(camPath, FileMode.Open))
                {
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        camera = new XATCameraPathFile(br);
                    }
                }

                IllusioCutsceneManager.Instance.CameraPath = camera;

                IllusioCutsceneManager.Instance.StartPlayback(useScale);

                if (hideUI) MainWindow.Toggle();
            }


            ImGui.Checkbox("Hide UI On Play", ref hideUI);

            ImGui.Checkbox("Enable Race Scaling Offsets For Camera", ref useScale);

            ImGui.InputFloat3("Scale", ref IllusioCutsceneManager.Instance.CameraSettings.Scale);
            ImGui.InputFloat3("Offset", ref IllusioCutsceneManager.Instance.CameraSettings.Offset);
            ImGui.Checkbox("Loop Emote Camera", ref IllusioCutsceneManager.Instance.CameraSettings.Loop);

            using (ImRaii.Disabled(!IllusioCutsceneManager.Instance.IsRunning))
            {
                if (ImGui.Button("Force Stop Emote Camera"))
                {
                    IllusioCutsceneManager.Instance.StopPlayback();
                }
            }

            

            if (!DalamudServices.clientState.IsGPosing)
            {
                ImGui.TextColored(IVColors.Red, "Emote Camera Control is Unavailable Outside of Group Pose");
            }
        }
    }
}
