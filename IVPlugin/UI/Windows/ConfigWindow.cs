using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using ImGuiNET;
using IVPlugin.ActorData;
using IVPlugin.Camera;
using IVPlugin.Core;
using IVPlugin.Core.Extentions;
using IVPlugin.Mods;
using IVPlugin.Services;
using IVPlugin.UI;
using IVPlugin.UI.Helpers;
using IVPlugin.UI.Windows;
using Lumina.Excel.GeneratedSheets;
using Penumbra.Api.IpcSubscribers;

namespace IVPlugin.Windows;

public static class ConfigWindow
{
    public static bool IsOpen = false;
    public static void Show() => IsOpen = true;

    public static void Hide() => IsOpen = false;
    public static void Toggle() => IsOpen = !IsOpen;

    private static bool NPCHack = true;
    private static bool SkeleColors = true;
    private static bool installIVCS = true;
    private static bool aSceneLocalSpace = true;
    private static bool aSceneWarningShow = true;
    private static bool FadeInOnAnimation = true;

    private static bool IVCSRequiresUpdate = false;
    public static void Draw()
    {
        if (!IsOpen) return;

        var size = new Vector2(-1, -1);
        ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

        ImGui.SetNextWindowSizeConstraints(new Vector2(395, 0), new Vector2(500, 1080));

        NPCHack = IllusioVitae.configuration.UseNPCHack;
        SkeleColors = IllusioVitae.configuration.UseSkeletonColors;
        installIVCS = IllusioVitae.configuration.installIVCS;
        aSceneLocalSpace = IllusioVitae.configuration.ActorSceneLocalSpace;
        aSceneWarningShow = IllusioVitae.configuration.ActorSceneWarningShow;
        FadeInOnAnimation = IllusioVitae.configuration.FadeInOnAnimation;

        if (ImGui.Begin($"Illusio Vitae: Configuration Settings", ref IsOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {

            if (ImGui.BeginTabBar("ConfigTabBar"))
            {
                if (ImGui.BeginTabItem("General"))
                {
                    GeneralTabDraw();

                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("NPC Presets"))
                {
                    AppearancesDraw();

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }
    }

    private static void GeneralTabDraw()
    {
        BearGUI.Text("Custom Emote Installation Directory", 1.1f);

        BearGUI.Text(IllusioVitae.configuration.ModLocation, .85f);

        ImGui.Spacing();

        if (ImGui.Button("Change Location"))
        {
            WindowsManager.Instance.fileDialogManager.OpenFolderDialog("Select a Custom Emote Installation Directory", (confirm, path) =>
            {
                if (confirm)
                {
                    IllusioVitae.configuration.ModLocation = path;
                }
            });
        }

        ImGui.Spacing();

        ImGui.Separator();

        if (ImGui.Checkbox("Show Actor Scene World Space Warning", ref aSceneWarningShow))
        {
            IllusioVitae.configuration.ActorSceneWarningShow = aSceneWarningShow;
        }

        ImGui.Spacing();

        if (ImGui.Checkbox("Enable NPC Appearance Data for Concept Matrix", ref NPCHack))
        {
            IllusioVitae.configuration.UseNPCHack = NPCHack;
        }

        ImGui.Spacing();

        if (ImGui.Checkbox("Enable Skeleton Editor Color Coding", ref SkeleColors))
        {
            IllusioVitae.configuration.UseSkeletonColors = SkeleColors;
        }

        ImGui.Spacing();

        using (ImRaii.Disabled(!DalamudServices.penumbraServices.CheckAvailablity()))
        {

            if (ImGui.Button("Install IVCS"))
            {
                ModManager.Instance.InstallIVCSMod();
            }
        }

        ImGui.Spacing();

        if (ImGui.Button("Show Changelog"))
        {
            ChangeLogWindow.Show();
        }
    }

    public static int id = -1;

    private static void AppearancesDraw()
    {
        BearGUI.Text("NPC Appearance Presets for Emotes", 1.1f);

        ImGui.Spacing();

        using(var listbox = ImRaii.ListBox("##Custom Preset", new(279, 100)))
        {
            if (listbox.Success)
            {
                for(var i = 0; i < IllusioVitae.configuration.PresetActors.Length; i++)
                {
                    var currentActor = IllusioVitae.configuration.PresetActors[i];

                    var selected = ImGui.Selectable($"{currentActor.Name}##{i}", id == i);

                    if(selected)
                    {
                        id = i;
                    }
                }  
            }
        }

        ImGui.SameLine();

        ImGui.BeginGroup();

        if (ImGui.Button("Create Preset"))
        {
            IllusioVitae.configuration.PresetActors = IllusioVitae.configuration.PresetActors.Append(new($"Illusio Actor", null)).ToArray();
        }

        ImGui.Spacing();
        ImGui.Spacing();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            if (id != -1)
            {
                var selectedActor = IllusioVitae.configuration.PresetActors[id];

                using (ImRaii.Disabled(id == 0))
                {
                    if (ImGui.Button($"{FontAwesomeIcon.AngleUp.ToIconString()}##MoveUp"))
                    {
                        var prevActor = IllusioVitae.configuration.PresetActors[id - 1];

                        IllusioVitae.configuration.PresetActors[id - 1] = selectedActor;

                        IllusioVitae.configuration.PresetActors[id] = prevActor;

                        id = id - 1;
                    }
                }

                using (ImRaii.Disabled(IllusioVitae.configuration.PresetActors.Length - 1 == id))
                {
                    if (ImGui.Button($"{FontAwesomeIcon.AngleDown.ToIconString()}##MoveDown"))
                    {
                        var nextActor = IllusioVitae.configuration.PresetActors[id + 1];

                        IllusioVitae.configuration.PresetActors[id + 1] = selectedActor;

                        IllusioVitae.configuration.PresetActors[id] = nextActor;

                        id = id + 1;
                    }
                }
            }
            else
            {
                using (ImRaii.Disabled(id == -1))
                {
                    if (ImGui.Button($"{FontAwesomeIcon.AngleUp.ToIconString()}##MoveUpFake")) { }
                }

                using (ImRaii.Disabled(id == -1))
                {
                    if (ImGui.Button($"{FontAwesomeIcon.AngleDown.ToIconString()}##MoveDownFake")) { }
                }
            }
        }

        ImGui.EndGroup();

        if (id == -1) return;
        ImGui.Separator();
        ImGui.Spacing();

        string actorName = IllusioVitae.configuration.PresetActors[id].Name;

        ImGui.Text("Rename Character Preset:");

        ImGui.SetNextItemWidth(279);
        if(ImGui.InputText("##nameinput", ref actorName, 50))
        {
            if (CheckValidName(actorName))
            {
                IllusioVitae.configuration.PresetActors[id].Name = actorName.Captialize();
            }
        }

        ImGui.SameLine();

        ImGui.TextColored(CheckValidName(actorName) ? IVColors.Green : IVColors.Red,
           CheckValidName(actorName) ? "Valid Name" : "Invalid Name");

        ImGui.Spacing();

        var currentCharaLocation = IllusioVitae.configuration.PresetActors[id].charaPath;
            
        ImGui.Text("Selected File: " + (string.IsNullOrEmpty(currentCharaLocation) ? "Not Set" : Path.GetFileName(currentCharaLocation)));

        if (ImGui.Button("Select Character Data"))
        {
            WindowsManager.Instance.fileDialogManager.OpenFileDialog("Import a Character Data File", ".chara", (confirm, path) =>
            {
                if (confirm)
                {
                    IllusioVitae.configuration.PresetActors[id].charaPath = path;
                }
            });
        }

        ImGui.SameLine();

        if(ImGui.Button("Delete Selected Preset"))
        {
            var list = IllusioVitae.configuration.PresetActors.ToList();

            list.Remove(IllusioVitae.configuration.PresetActors[id]);

            IllusioVitae.configuration.PresetActors = list.ToArray();

            id = -1;
        }
    }

    private static bool CheckValidName(string newName)
    {
        var strings = newName.Split(" ");


        if (strings.Length > 2) return false;

        if (strings.Length < 2) return false;

        foreach (var s in strings)
        {
            if (HasSpecialChars(s)) return false;

            int capitalized = 0;

            if (s.Length < 2) return false;

            for (var i = 1; i < s.Length; i++)
            {
                if (char.IsUpper(s[i]))
                {
                    capitalized++;
                }
            }

            if (capitalized > 0) return false;
        }

        return true;
    }

    private static bool HasSpecialChars(string yourString)
    {
        return yourString.Any(ch => (!char.IsLetter(ch) && (ch != '-' && ch != '\'')));
    }
}
