using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using IVPlugin.UI.Helpers;
using IVPlugin.Commands;
using IVPlugin.Mods;
using IVPlugin.Mods.Structs;
using System.Runtime.CompilerServices;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using IVPlugin.Services;
using IVPlugin.Core.Extentions;
using IVPlugin.Json;
using System.IO.Compression;
using System.IO;
using IVPlugin.ActorData;
using System.Security.AccessControl;
using Dalamud.Interface;
using IVPlugin.Log;
using IVPlugin.Core;
using IVPlugin.Resources;
using IVPlugin.Actors;

namespace IVPlugin.UI.Windows.Tabs
{
    public static class CustomdataTab
    {

        private static string selectedMod = string.Empty;
        private static IVMod selectedModData;

        private static bool Enabled, CameraEnabled, BGMEnabled, VFXEnabled;

        public static void Draw()
        {
            BearGUI.Text("Installed Emotes List", 1.1f);

            DrawEmoteTab();
            
            ImGui.EndTabItem();
        }

        private static void DrawEmoteTab()
        {
            if (ImRaii.Child("##EmoteModList", new Vector2(0, 150), true))
            {
                foreach (var mod in ModManager.Instance.mods.Values)
                {
                    SetUpChild(mod);
                }

                ImGui.EndChildFrame();
            }

            ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X / 2 - 60);

            ImGui.BeginGroup();

            if (BearGUI.FontButton("createmod", FontAwesomeIcon.FileCirclePlus.ToIconString()))
            {
                ModCreationWindow.Show();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Create Custom Emote");
            }

            ImGui.SameLine();

            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button(FontAwesomeIcon.FileImport.ToIconString()))
                {
                    WindowsManager.Instance.fileDialogManager.OpenFileDialog("Import Illusio Vitae Modpack", ".ivmp", (Confirm, FilePath) =>
                    {
                        if (!Confirm) return;

                        string folderPath = Path.Combine(IllusioVitae.configuration.ModLocation, Path.GetFileNameWithoutExtension(FilePath));

                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);

                        try
                        {
                            ZipFile.ExtractToDirectory(FilePath, folderPath);

                            ModManager.Instance.Refresh();
                        }
                        catch (Exception ex)
                        {
                            IllusioDebug.Log("Unable to process new mod" + ex, LogType.Error, false);

                            Directory.Delete(folderPath, true);
                        }

                    });
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Import Custom Emote");
            }

            ImGui.SameLine();

            using (ImRaii.Disabled(selectedMod == ""))
            {
                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString()))
                    {
                        ModManager.Instance.DeleteMod(selectedModData.emote.Name);

                        selectedMod = "";
                        selectedModData = null;
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Remove Custom Emote");
                }
            }

            ImGui.SameLine();

            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button(FontAwesomeIcon.ArrowsSpin.ToIconString()))
                {
                    ModManager.Instance.Refresh();
                    selectedMod = "";
                    selectedModData = null;
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Refresh Mod List");
            }

            ImGui.EndGroup();

            // TODO: Remake or Dispose of Mod Creator
            /*
            ImGui.SameLine();

            ImGui.SetCursorPosX(360);

            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button(FontAwesomeIcon.FilePen.ToIconString()))
                {
                    CreateModWindow.Show();
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Create Custom Emote");
            }
            */

            ImGui.Separator();

            if (selectedMod != string.Empty && selectedModData.emote.emoteData.Count != 0)
            {
                ImGui.Text($"Author: {selectedModData.emote.Author}");

                ImGui.Text("Command List:");

                using(ImRaii.Child("##EmoteCommandsChild", new(0, 87.5f), true))
                {
                    XIVActor playerActor = ActorManager.Instance.playerActor;

                    if (DalamudServices.clientState.IsGPosing) playerActor = ActorManager.Instance.mainGposeActor;

                    for (var i = 0; i < selectedModData.emote.emoteData.Count; i++)
                    {
                        ImGui.BeginGroup();

                        bool isForCurrentRace = false;

                        bool hasPap = false;

                        foreach(var path in selectedModData.emote.emoteData[i].dataPaths)
                        {
                            if (Path.GetExtension(path.GamePath) != ".pap") continue;
                            else hasPap = true;

                            if (playerActor == null || !playerActor.IsLoaded(true))
                            {
                                break;
                            }

                            if (path.validRaces.HasFlag(playerActor.GetCustomizeData().GetRaceCode())) 
                            {
                                isForCurrentRace = true;
                                break;
                            }
                        }

                        if (!hasPap) isForCurrentRace = true;

                        var activeVFX = ModManager.Instance.ActiveModVFX.ContainsKey(selectedModData.emote.emoteData[i].GetCommand() + "VFX");

                        var alwaysActive = selectedModData.emote.emoteData[i].emoteType == EmoteType.Additive;

                        using (ImRaii.PushFont(UiBuilder.IconFont))
                        {
                            var startPos = ImGui.GetCursorPos();

                            using(ImRaii.Disabled((!alwaysActive && !EventManager.validCheck) || !isForCurrentRace))
                            {
                                if (ImGui.Button($"##{i}play", new(20, 22)))
                                {
                                    ModManager.Instance.PlayMod(selectedModData, i, false);
                                }
                            }

                            ImGui.SameLine();

                            ImGui.SetCursorPos(new(startPos.X + 2, startPos.Y - 1));

                            if (!EventManager.validCheck || !isForCurrentRace)
                            {
                                if (alwaysActive)
                                {
                                    ImGui.TextColored(activeVFX ? IVColors.lRed : IVColors.Green, activeVFX ? FontAwesomeIcon.Stop.ToIconString() : FontAwesomeIcon.Play.ToIconString());
                                }
                                else
                                {
                                    ImGui.TextColored(new(.5f, .5f, .5f, 1), FontAwesomeIcon.Ban.ToIconString());
                                }
                                
                            }
                            else
                            {
                                ImGui.TextColored(activeVFX ? IVColors.lRed : IVColors.Green, activeVFX ? FontAwesomeIcon.Stop.ToIconString() : FontAwesomeIcon.Play.ToIconString());


                            }
                        }

                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(activeVFX ? "Stop VFX" : "Play Emote");
                        }

                        using (ImRaii.Disabled((!EventManager.validCheck) || !isForCurrentRace))
                        {
                            if (selectedModData.emote.allowNPC)
                            {
                                ImGui.SameLine();

                                using (ImRaii.PushFont(UiBuilder.IconFont))
                                {
                                    if (ImGui.Button($"{FontAwesomeIcon.Users.ToIconString()}##{i}MultiPlay"))
                                    {
                                        ModManager.Instance.PlayMod(selectedModData, i, true);
                                    }
                                }

                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.SetTooltip("Play Multi Emote");
                                }

                            }
                        }

                        ImGui.SameLine();

                        ImGui.Text(selectedModData.emote.emoteData[i].GetCommand());
                        ImGui.EndGroup();

                        if(i % 2 == 0)
                        {
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(200);
                        }
                    }
                }

                if (ImGui.CollapsingHeader($"{selectedModData.emote.Name} Settings"))
                {
                    if (ImGui.Checkbox("Enable Emote", ref Enabled))
                    {
                        ModManager.Instance.ToggleModStatus(selectedMod);
                    }

                    ImGui.Spacing();

                    if (selectedModData.emote.bgmData.scdPath != "" && selectedModData.emote.bgmData.scdPath != null)
                    {
                        if (ImGui.Checkbox("Enable Music", ref BGMEnabled))
                        {
                            ModManager.Instance.ToggleBGMStatus(selectedMod);
                        }
                    }
                    else
                    {
                        using (ImRaii.Disabled(true))
                        {
                            ImGui.Button("Import BGM");
                        }
                    }

                    ImGui.Spacing();

                    if (selectedModData.emote.emoteData[0].vfxData.Count > 0 && selectedModData.emote.emoteData[0].vfxData[0].vfxDatapaths[0].GamePath != "")
                    {
                        if (ImGui.Checkbox("Enable VFX", ref VFXEnabled))
                        {
                            ModManager.Instance.ToggleVFXStatus(selectedMod);
                        }

                        ImGui.Spacing();
                    }

                    if (selectedModData.emote.cameraPath != "" && selectedModData.emote.cameraPath != null)
                    {
                        if (ImGui.Checkbox("Enable Camera (Gpose Only)", ref CameraEnabled))
                        {
                            ModManager.Instance.ToggleCameraStatus(selectedMod);
                        }
                    }
                    else
                    {
                        if (ImGui.Button("Import Cameara"))
                        {
                            ModManager.Instance.importCamera(selectedMod);
                        }
                    }
                }
            }
        }

        private static void SetUpChild(IVMod mod)
        {
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 100);

            var startPos = ImGui.GetCursorPosX();

            bool selectedModItem = false;

            selectedModItem = ImGui.Selectable($"##{mod.emote.Name}", selectedModData == mod);

            ImGui.SameLine();

            ImGui.SetCursorPosX(startPos);

            bool isValidForCharacter = false;

            XIVActor playerActor = ActorManager.Instance.playerActor;

            if (DalamudServices.clientState.IsGPosing) playerActor = ActorManager.Instance.mainGposeActor;

            if(playerActor != null)
            {
                switch (mod.emote.category)
                {
                    case ModCatagory.Global:
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2);
                        ImGui.Image(GameResourceManager.Instance.GetResourceImage("CategoryGlobal.png").ImGuiHandle, new(20, 20));
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Global Animation");
                        }
                        isValidForCharacter = true;
                        break;
                    case ModCatagory.Male:
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2);
                        ImGui.Image(GameResourceManager.Instance.GetResourceImage("CategoryMale.png").ImGuiHandle, new(20, 20));
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Male Exclusive Animation");
                        }
                        isValidForCharacter = playerActor.GetCustomizeData().Gender == Genders.Masculine;
                        break;
                    case ModCatagory.Female:
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2);
                        ImGui.Image(GameResourceManager.Instance.GetResourceImage("CategoryFemale.png").ImGuiHandle, new(20, 20));
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Female Exclusive Animation");
                        }
                        isValidForCharacter = playerActor.GetCustomizeData().Gender == Genders.Feminine;
                        break;
                    case ModCatagory.Straight:
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2);
                        ImGui.Image(GameResourceManager.Instance.GetResourceImage("CategoryStraight.png").ImGuiHandle, new(20, 20));
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Straight Animation");
                        }
                        isValidForCharacter = true;
                        break;
                    case ModCatagory.Bisexual:
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2);
                        ImGui.Image(GameResourceManager.Instance.GetResourceImage("CategoryBisexual.png").ImGuiHandle, new(20, 20));
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Bisexual Animation");
                        }
                        isValidForCharacter = true;
                        break;
                    case ModCatagory.Gay:
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2);
                        ImGui.Image(GameResourceManager.Instance.GetResourceImage("CategoryGay.png").ImGuiHandle, new(20, 20));
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Gay Animation");
                        }
                        isValidForCharacter = playerActor.GetCustomizeData().Gender == Genders.Masculine;
                        break;
                    case ModCatagory.Lesbian:
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2);
                        ImGui.Image(GameResourceManager.Instance.GetResourceImage("CategoryLesbian.png").ImGuiHandle, new(20, 20));
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Lesbian Animation");
                        }
                        isValidForCharacter = playerActor.GetCustomizeData().Gender == Genders.Feminine;
                        break;

                    default:
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2);
                        ImGui.Image(GameResourceManager.Instance.GetResourceImage("CategoryGlobal.png").ImGuiHandle, new(20, 20));
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Global Animation");
                        }
                        isValidForCharacter = true;
                        break;
                }
            }

            

            ImGui.SameLine();

            ImGui.SetNextItemWidth(145);
            //ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1);

            var displayName = mod.emote.Name;

            var width = ImGui.CalcTextSize(displayName).X;

            if(width > ImGui.CalcTextSize("XXXXXXXXXXXXXXXXXXXXXXXXX").X)
            {
                displayName = "";
                for(var i =0; ImGui.CalcTextSize(displayName).X < ImGui.CalcTextSize("XXXXXXXXXXXXXXXXXXXXXXXX").X; i++)
                {
                    if (i > mod.emote.Name.Length) break;

                    displayName += mod.emote.Name[i];
                }

                displayName += "...";
            }

            ImGui.TextColored(selectedModData == mod ? IVColors.Yellow : IVColors.White, displayName);

            ImGui.SameLine();

            ImGui.BeginGroup();

            if(ModManager.Instance.mods.Count < 7)
            {
                var x = ImGui.GetWindowContentRegionMax().X;
                if(x > ImGui.CalcTextSize("XXXXXXXXXXXXXXXXXXXXXXXXX").X + 80)
                    ImGui.SetCursorPosX((x - 80));
                else
                    ImGui.SetCursorPosX(ImGui.CalcTextSize("XXXXXXXXXXXXXXXXXXXXXXXXX").X);
            }
            else
            {
                var x = ImGui.GetWindowContentRegionMax().X;

                if (x > ImGui.CalcTextSize("XXXXXXXXXXXXXXXXXXXXXXXXX").X + 90)
                    ImGui.SetCursorPosX((x-90));
                else
                    ImGui.SetCursorPosX(ImGui.CalcTextSize("XXXXXXXXXXXXXXXXXXXXXXXXX").X);
            }

            if (mod.error != ErrorType.none)
            {
                ImGui.TextColored(IVColors.Red, "ERROR");
                return;
            }

            bool ValidAnimType = false;

            foreach(var x in mod.emote.emoteData)
            {
                if(x.emoteType == EmoteType.Additive)
                {
                    ValidAnimType = true;
                }
            }

            if (isValidForCharacter)
            {
                if (!EventManager.validCheck && !ValidAnimType)
                {

                    using (ImRaii.PushFont(UiBuilder.IconFont))
                    {
                        ImGui.TextColored(new(.5f, .5f, .5f, 1),
                        mod.config.enabled ? FontAwesomeIcon.Check.ToIconString() : FontAwesomeIcon.Ban.ToIconString());
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Mod Unavailable");
                    }
                }
                else
                {
                    using (ImRaii.PushFont(UiBuilder.IconFont))
                    {
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1);
                        ImGui.TextColored(
                        mod.config.enabled ? IVColors.Green : IVColors.Red,
                        mod.config.enabled ? FontAwesomeIcon.Check.ToIconString() : FontAwesomeIcon.Ban.ToIconString());
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(mod.config.enabled ? "Mod Enabled" : "Mod Disabled");
                    }
                }
            }
            else
            {
                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1);
                    ImGui.TextColored(IVColors.Cyan, FontAwesomeIcon.Exclamation.ToIconString());
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Mod Unavailable for current Gender");
                }
            }
            

            ImGui.SameLine();
            
            if (!string.IsNullOrEmpty(mod.emote.bgmData.scdPath))
            {
                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1);
                    ImGui.TextColored(
                    mod.config.BGMEnabled ? IVColors.Green : IVColors.Red,
                    FontAwesomeIcon.Music.ToIconString());
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(mod.config.BGMEnabled ? "Music Enabled" : "Music Disabled");
                }
            }
            else
            {
                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1);
                    ImGui.TextColored(new(.5f, .5f, .5f, 1), FontAwesomeIcon.Music.ToIconString());
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Music Unavailable");
                }
            }

            ImGui.SameLine();
            
            if (mod.emote.emoteData[0].vfxData.Count > 0 && mod.emote.emoteData[0].vfxData[0].vfxDatapaths != null)
            {
                var activeVFX = false;
                
                foreach(var cMod in mod.emote.emoteData)
                {
                    if (ModManager.Instance.ActiveModVFX.ContainsKey(cMod.GetCommand() + "VFX"))
                    {
                        activeVFX = true;
                        break;
                    }
                }
                    

                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1);
                    if (activeVFX)
                        ImGui.TextColored(IVColors.Magenta, FontAwesomeIcon.WandMagicSparkles.ToIconString());
                    else
                        ImGui.TextColored(
                            mod.config.VFXEnabled ? IVColors.Green : IVColors.Red,
                            mod.config.VFXEnabled ? FontAwesomeIcon.WandMagicSparkles.ToIconString() : FontAwesomeIcon.Magic.ToIconString());


                }

                if (ImGui.IsItemHovered())
                {
                    if (activeVFX)
                        ImGui.SetTooltip("Active VFX");
                    else
                        ImGui.SetTooltip(mod.config.VFXEnabled ? "VFX Enabled" : "VFX Disabled");
                }
            }
            else
            {
                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1);
                    ImGui.TextColored(new(.5f, .5f, .5f, 1), FontAwesomeIcon.Magic.ToIconString());
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("VFX Unavailable");
                }
            }

            ImGui.SameLine();
            
            if (!string.IsNullOrEmpty(mod.emote.cameraPath))
            {
                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1);
                    ImGui.TextColored(
                    mod.config.cameraEnabled ? IVColors.Green : IVColors.Red,
                    FontAwesomeIcon.Camera.ToIconString());
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(mod.config.cameraEnabled ? "Camera Enabled" : "Camera Disabled");
                }
            }
            else
            {
                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1);
                    ImGui.TextColored(new(.5f,.5f,.5f, 1),FontAwesomeIcon.Camera.ToIconString());
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("No Camera Available");
                }
            }

            ImGui.EndGroup();

            if (selectedModItem)
            {
                selectedMod = mod.emote.Name;

                selectedModData = mod;

                Enabled = mod.config.enabled;

                CameraEnabled = mod.config.cameraEnabled;

                BGMEnabled = mod.config.BGMEnabled;

                VFXEnabled = mod.config.VFXEnabled;
            }
        }
    }
}
