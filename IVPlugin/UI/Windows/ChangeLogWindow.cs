using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using IVPlugin.Services;
using IVPlugin.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.UI.Windows
{
    public static class ChangeLogWindow
    {
        public static bool IsOpen = false;

        public static void TryShow()
        {
            if(IllusioVitae.configuration.LastSeenVersion == -1)
            {
                Show();
                IllusioVitae.configuration.LastSeenVersion = version;
                return;
            }

            if(IllusioVitae.configuration.LastSeenVersion < version)
            {
                Show();
                IllusioVitae.configuration.LastSeenVersion = version;
                return;
            }
        }
        public static void Show() => IsOpen = true;
        public static void Toggle() => IsOpen = !IsOpen;

        public const int version = 1; //change to show an update to changelog

        public static void Draw()
        {
            if (!IsOpen) return;

            var size = new Vector2(-1, -1);
            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

            ImGui.SetNextWindowSizeConstraints(new Vector2(700, 700), new Vector2(1000, 1000));

            if (ImGui.Begin($"Illusio Vitae ChangeLog", ref IsOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                using (ImRaii.Child("##changelogCHILD", new(0, 620)))
                {
                    Ver1();

                    ImGui.Spacing();
                    ImGui.Spacing();

                    ver0();
                }
                

                ImGui.SetCursorPos(new(ImGui.GetWindowContentRegionMax().X/2 - ImGui.CalcTextSize("Close").X/2, ImGui.GetWindowContentRegionMax().Y - 25));
                if (ImGui.Button("Close"))
                {
                    Toggle();
                }
            }
        }

        #region Logs

        private static void Ver1()
        {
            BearGUI.Text("[08/09/24] - Version 1.0.5:", 2, 0xFFFF00FF);
            BearGUI.Text("- Added support to create IVMP Files");
            BearGUI.Text("- Small Bug Fixes");
        }

        private static void ver0()
        {
            BearGUI.Text("[07/24/24] - Version 1.0:", 2, 0xFFFF00FF);
            BearGUI.Text("- Illusio Vitae Plugin has officially been released.");
            BearGUI.Text("- Custom Emote tab and Mod Manager have been added.");
            BearGUI.Text("  Emotes can now have unique slash commands, and no longer replace existing files.");
            BearGUI.Text("- Added support for VFX specific emotes,");
            BearGUI.Text("  allowing for special persistent effects around the player character at all times.");
            BearGUI.Text("- Added various Quality of Life features such as toggling Music, VFX, or Cameras from Custom Emotes.");
            BearGUI.Text("- Concept Matrix tab, Appearance Editor, Skeleton Editor, and Animation Controller have been added.");
            BearGUI.Text("- Added full actor customization, gear swapping, and partial shader manipulation to the Appearance Editor.");
            BearGUI.Text("- Added the ability to move an actor along with its camera in GPose.");
            BearGUI.Text("- Added the ability to Save and Load poses made using the Skeleton Editor. (Brio Pose Importing Supported)");
            BearGUI.Text("- Added various filters to the Skeleton Editor for the sake of Quality of Life.");
            BearGUI.Text("- Added Actor Scenes which can save and load multiple actors, their current poses,");
            BearGUI.Text("  and their current world placement.");
            BearGUI.Text("- Added Custom Actor spawning. These actors can be edited in the Appearance Editor,");
            BearGUI.Text("  and can be used in tandem with the Actor Scene feature mentioned above.");
            BearGUI.Text("- Added a Lock Appearance toggle to Concept Matrix.");
            BearGUI.Text("  This will attempt to freeze your current customize data between loading areas.");
            BearGUI.Text("- Added Animation switching, pausing, speed adjustment, and scrubbing to the Animation Controller.");
            BearGUI.Text("- Added a Racial Animation Override dropdown to the Animation Controller.");
            BearGUI.Text("  This functions the same as \"Data Path\" did in the original Concept Matrix.");
            BearGUI.Text("- Added the ability to view and adjust the speed of each individual animation slot");
            BearGUI.Text("  on the currently selected actor.");
            BearGUI.Text("- World Control tab, Time and Weather Controller, Camera Controller,");
            BearGUI.Text("  and Custom Emote Camera Controller have been added.");
            BearGUI.Text("- Added the ability to adjust the current time, weather, and skybox texture.");
            BearGUI.Text("- Added the ability to manipulate the camera freely while in GPose,");
            BearGUI.Text("  and fine tune settings normally inaccessible to the player.");
            BearGUI.Text("- Added a means to load and play XCP files freely,");
            BearGUI.Text("  regardless of whether an existing emote has a Camera file or not.");
            BearGUI.Text("- Illusio Vitae Custom Skeletons (IVCS) has been updated to 2.0 and is now fully Dawntrail compatible.");
            BearGUI.Text("- IVCS now has a variety of physics enabled bones that can be used should any body mod support it.");
            BearGUI.Text("- IVCS now utilizes deforming. You no longer require extra mods or overhead to use IVCS.");
            BearGUI.Text("  All Body Mods can now be weighted to IVCS the same way as the base skeleton.");
        }

        #endregion
    }
}
