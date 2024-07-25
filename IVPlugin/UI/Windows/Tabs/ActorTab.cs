using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using ImGuiNET;
using IVPlugin.ActorData;
using IVPlugin.Actors.Structs;
using IVPlugin.Core;
using IVPlugin.Core.Extentions;
using IVPlugin.Mods.Structs;
using IVPlugin.Resources;
using IVPlugin.Services;
using IVPlugin.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Security.AccessControl;

namespace IVPlugin.UI.Windows.Tabs
{
    public static class ActorTab
    {
        private static XIVActor selectedActor;

        private static string EmoteSearch = "";

        private static XIVActor rightclickedActor = null;

        private static bool newActor = false, forceAnimation = false;

        private static string[] EmoteType = ["Base", "Startup", "Floor", "Sitting", "Blend", "Other", "Adjusted"];
        private static string[] Lipstyles = ["None", "Whisper (Short)", "Whisper (Normal)", "Whisper (Long)", "Talk (Short)", "Talk (Normal)", "Talk (Long)", "Shout (Short)", "Shout (Normal)", "Shout (Long)"];
        private static string[] Races = ["Hyur Midlander Male", "Hyur Midlander Female", "Hyur Highlander Male", "Hyur Highlander Female", "Elezen Male", "Elezen Female", "Miqo'te Male", "Miqo'te Female", "Roegadyn Male", "Roegadyn Female", "Lalafell Male", "Lalafell Femake", "AuRa Male", "AuRa Female", "Horthgar Male", "Hrothgar Female", "Viera Male", "Viera Female"];
        private static string[] AnimationSlotNames = ["Base:", "Torso:", "Expression:", "Additive:", "Mouth:", "Misc 1:", "Misc 2:", "Misc 3:", "Misc 4:", "Overlay:"];
        private static int[] lipIDs = [0, 626, 627, 628, 629, 630, 631, 632, 633, 634];

        private static int lipSelect = 0;

        private static string newName = "";

        private static int animID = 0, blendAnimdID = 0;
        private static string animName = "";
        private static bool addCompanionSlot = false;

        private static bool lockWeaponID = false;

        private static uint weapAttachPoint;
        private static float weapScale;

        public static void Draw()
        {
            BearGUI.Text("Selected Actors List", 1.1f);

            if (selectedActor != null && !selectedActor.IsLoaded())
            {
                selectedActor = null;
            }

            if (DalamudServices.clientState.IsGPosing && !ActorManager.Instance.GPoseActors.Contains(selectedActor))
            {
                if(ActorManager.Instance.GPoseActors.Count > 0)
                    selectedActor = selectedActor = ActorManager.Instance.mainGposeActor;
            }

            if (selectedActor == null)
            {
                ApperanceWindow.Hide();
                selectedActor = ActorManager.Instance.playerActor;
            }

            List<XIVActor> actors = new();

            if (DalamudServices.clientState.IsGPosing) 
            {
                actors = ActorManager.Instance.GPoseActors;
            }
            else
            {
                actors = ActorManager.Instance.Actors;
            }

            if (ImRaii.Child("##ActorList", new Vector2(0, 125), true))
            {
                foreach (var actor in actors)
                {
                    if (actor == null || !actor.IsLoaded()) continue;

                    var startPos = ImGui.GetCursorPosX();

                    bool selected = false;

                    string actorName = actor.GetName();

                    using (ImRaii.Disabled((actor.actorObject?.ObjectKind == ObjectKind.MountType || actor.actorObject?.ObjectKind == ObjectKind.Ornament) && !IllusioVitae.InDebug()))
                    {
                        selected = ImGui.Selectable($"##{actor.GetName()}{actor.actorObject.ObjectIndex}", selectedActor.actorObject.Address == actor.actorObject.Address, ImGuiSelectableFlags.DontClosePopups);
                    }
                        
                    if (ImGui.IsItemHovered())
                    {
                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                        {
                            rightclickedActor = actor;
                            ImGui.OpenPopup("rClickActorPopup");
                        }
                    }

                    ImGui.SameLine();

                    ImGui.SetCursorPosX(startPos);

                    using (ImRaii.PushFont(UiBuilder.IconFont))
                    {
                        var icon = FontAwesomeIcon.Skull;
                        switch (actor.actorObject?.ObjectKind)
                        {
                            case ObjectKind.Player:
                                icon = FontAwesomeIcon.User;
                                break;
                            case ObjectKind.BattleNpc:
                                icon = FontAwesomeIcon.UserShield;
                                break;
                            case ObjectKind.EventNpc:
                                icon = FontAwesomeIcon.UserTie;
                                break;
                            case ObjectKind.Companion:
                                icon = FontAwesomeIcon.Dog;
                                break;
                            case ObjectKind.MountType:
                                actorName = "Mount";
                                icon = FontAwesomeIcon.Biking;
                                break;
                            case ObjectKind.Ornament:
                                actorName = "Ornament";
                                icon = FontAwesomeIcon.Umbrella;
                                break;
                        }

                        if(actor.isCustom)
                            icon = FontAwesomeIcon.UserAstronaut;

                        ImGui.Text(icon.ToIconString());
                    }

                    ImGui.SameLine();
                    if((actor.actorObject?.ObjectKind == ObjectKind.MountType || actor.actorObject?.ObjectKind == ObjectKind.Ornament) && !IllusioVitae.InDebug())
                    {
                        ImGui.TextColored(new(.5f, .5f, .5f, 1), actorName);
                    }
                    else
                    {
                        if (actor.actorObject?.ObjectKind == ObjectKind.MountType);
                        ImGui.TextColored(selectedActor.actorObject.Address == actor.actorObject.Address ? IVColors.Yellow : IVColors.White, actorName);
                    }
                    

                    if (IllusioVitae.IsDebug && IllusioVitae.InDebug())
                    {
                        ImGui.SameLine();

                        ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - ImGui.CalcTextSize(actor.actorObject.ObjectIndex.ToString()).X);
                        ImGui.TextColored(IVColors.Cyan,actor.actorObject.ObjectIndex.ToString());
                        
                    }

                    if(selected)
                    {
                        selectedActor = actor;

                        if (SkeletonOverlay.IsOpen)
                        {
                            SkeletonOverlay.Show(actor);
                        }

                        if(ApperanceWindow.IsOpen)
                        {
                            ApperanceWindow.SetActorAndShow(ref selectedActor);
                        }
                    }
                }

                using(var popup = ImRaii.Popup("rClickActorPopup"))
                {
                    if (popup.Success)
                    {
                        if (ImGui.Button("Rename Actor"))
                        {
                            ImGui.OpenPopup("RenameActorPopup");
                        }
                    }

                    using (var popup2 = ImRaii.Popup("RenameActorPopup"))
                    {
                        if (popup2.Success)
                        {
                            ImGui.Text("Input Name");

                            ImGui.SetNextItemWidth(100);
                            if (ImGui.InputText("##customNameText", ref newName, 21, ImGuiInputTextFlags.EnterReturnsTrue))
                            {
                                rightclickedActor.Rename(checValidname() ? UpdateName(newName) : "");
                                newName = "";
                                ImGui.CloseCurrentPopup();
                            }

                            if (checValidname())
                            {
                                ImGui.Text("Valid Name");
                            }
                            else
                            {
                                ImGui.Text("Invalid Name");
                            }
                        }
                    }
                }

                ImGui.EndChildFrame();
            }

            ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X/2 - 140);

            ImGui.BeginGroup();

            using (ImRaii.Disabled(DalamudServices.TargetManager.Target == null && DalamudServices.TargetManager.GPoseTarget == null))
            {
                if (BearGUI.ImageButton("Add Actor", GameResourceManager.Instance.GetResourceImage("ActorAdd.png").ImGuiHandle, new(22, 22)))
                {
                    if (DalamudServices.clientState.IsGPosing)
                    {
                        ActorManager.Instance.SetUpGposeActor(DalamudServices.TargetManager.GPoseTarget);
                    }
                    else
                    {
                        if (DalamudServices.TargetManager.Target?.SubKind != 5 || IllusioVitae.InDebug())
                            ActorManager.Instance.SetUpActor(DalamudServices.TargetManager.Target);
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Add Targeted Actor");
                }
            }

            ImGui.SameLine();

            using (ImRaii.Disabled(selectedActor == ActorManager.Instance.playerActor))
            {
                if (BearGUI.ImageButton("Remove Actor", GameResourceManager.Instance.GetResourceImage("ActorRemove.png").ImGuiHandle, new(22, 22)))
                {
                    var deletableActor = selectedActor;
                    selectedActor = ActorManager.Instance.playerActor;

                    if (deletableActor.isCustom) deletableActor.DestroyActor();
                    else ActorManager.Instance.RemoveActor(deletableActor);

                    if (ApperanceWindow.IsOpen)
                    {
                        ApperanceWindow.SetActorAndShow(ref selectedActor);
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Remove Selected Actor");
                }
            }

            ImGui.SameLine();

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 2.5f);

            ImGui.Text("|");

            ImGui.SameLine();

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 2.5f);

            if (BearGUI.ImageButton("Edit Actor", GameResourceManager.Instance.GetResourceImage("ActorEdit.png").ImGuiHandle, new(22, 22)))
            {
                ApperanceWindow.SetActorAndShow(ref selectedActor);
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Open Actor Appearance Editor");
            }

            ImGui.SameLine();

            if (!EventManager.validCheck)
            {
                ImGui.BeginDisabled();
            }

            using(ImRaii.Disabled(!DalamudServices.clientState.IsGPosing && !IllusioVitae.InDebug()))
            {
                if (SkeletonOverlay.IsOpen)
                {
                    if (BearGUI.ImageButton("Hide Skeleton", GameResourceManager.Instance.GetResourceImage("SkeletonDisable.png").ImGuiHandle, new(22, 22)))
                    {
                        SkeletonOverlay.Hide();
                    }
                }
                else
                {
                    if (BearGUI.ImageButton("Show Skeleton", GameResourceManager.Instance.GetResourceImage("SkeletonEnable.png").ImGuiHandle, new(22, 22)))
                    {
                        SkeletonOverlay.Show(selectedActor);
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    if (SkeletonOverlay.IsOpen)
                        ImGui.SetTooltip("Hide Skeleton Editor & Overlay");
                    else
                        ImGui.SetTooltip("Show Skeleton Editor & Overlay");
                }
            }

            if (!EventManager.validCheck)
            {
                ImGui.EndDisabled();
            }

            ImGui.SameLine();

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 2.5f);

            ImGui.Text("|");

            ImGui.SameLine();

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 2.5f);

            if (BearGUI.ImageButton("New Actor", GameResourceManager.Instance.GetResourceImage("ActorNew.png").ImGuiHandle, new(22, 22)))
            {
                ImGui.OpenPopup("CustomActorNamePopup");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Create New Actor");
            }

            ImGui.SameLine();

            using (ImRaii.Disabled(selectedActor.isCustom && !DalamudServices.clientState.IsGPosing))
            {
                if (BearGUI.ImageButton("Target Actor", GameResourceManager.Instance.GetResourceImage("ActorTarget.png").ImGuiHandle, new(22, 22)))
                {
                    if (DalamudServices.clientState.IsGPosing)
                        DalamudServices.TargetManager.GPoseTarget = selectedActor.actorObject;
                    else
                        DalamudServices.TargetManager.Target = selectedActor.actorObject;
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Target Selected Actor");
                }
            }
            
            using(var popup = ImRaii.Popup("CustomActorNamePopup"))
            {
                if (popup.Success)
                {
                    newActor = true;

                    ImGui.Text("Input Name");

                    ImGui.SetNextItemWidth(100);
                    if(ImGui.InputText("##customNameText", ref newName, 21, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        ActorManager.Instance.SetUpCustomActor(null, checValidname() ? UpdateName(newName) : "", addCompanionSlot);
                        newActor = false;
                        newName = "";
                        ImGui.CloseCurrentPopup();
                    }

                    if (checValidname())
                    {
                        ImGui.Text("Valid Name");
                    }
                    else
                    {
                        ImGui.Text("Invalid Name");
                    }

                    ImGui.Checkbox("Add Companion Slot", ref addCompanionSlot);
                }
                else
                {
                    if (newActor)
                    {
                        ActorManager.Instance.SetUpCustomActor(null, "");
                        newActor = false;
                    }
                }
            }


            ImGui.SameLine();

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 2.5f);

            ImGui.Text("|");

            ImGui.SameLine();

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 2.5f);


            using (ImRaii.Disabled(!DalamudServices.clientState.IsGPosing && !IllusioVitae.InDebug()))
            {
                if (BearGUI.ImageButton("Save Scene", GameResourceManager.Instance.GetResourceImage("SceneSave.png").ImGuiHandle, new(22, 22)))
                {
                    ActorScene scene = new();

                    scene.SaveScene();
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Save Actor Scene");
                }

                ImGui.SameLine();

                if (BearGUI.ImageButton("Load Scene", GameResourceManager.Instance.GetResourceImage("SceneLoad.png").ImGuiHandle, new(22, 22)))
                {
                    ASceneWarningWindow.Show();
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Load Actor Scene");
                }
            }
            ImGui.EndGroup();

            ImGui.Separator();

            ActorDraw(selectedActor);

            ImGui.EndTabItem();
        }

        private static bool checValidname()
        {
            var strings = newName.Split(" ");
            

            if (strings.Length > 2) return false;

            if(strings.Length < 2) return false;

            foreach(var s in strings)
            {
                if (HasSpecialChars(s)) return false;

                int capitalized = 0;

                if(s.Length < 2) return false ;

                for(var i = 1; i < s.Length; i++)
                {
                    if (char.IsUpper(s[i]))
                    {
                        capitalized++;
                    }
                }

                if(capitalized > 0) return false;
            }

            return true;
        }

        private static bool HasSpecialChars(string yourString)
        {
            return yourString.Any(ch => (!char.IsLetter(ch) && (ch != '-' && ch != '\'')));
        }

        private static string UpdateName(string name)
        {
            var finalName = "";

            var strings = name.Split(" ");

            for(var i = 0; i < strings.Length; i++)
            {
                if(i != 0)
                {
                    finalName += " ";
                }

                var s = strings[i].Captialize();

                finalName += s;
            }

            return finalName;

        }

        private static void ActorDraw(XIVActor _actor)
        {
            BearGUI.Text("Player Settings", 1.1f);

            ImGui.Checkbox("Lock Appearance", ref _actor.forceCustomAppearance);

            ImGui.SameLine();

            List<string> comboNames = new() { "Character & Equipment Data", "Only Character Data", "Only Equipment Data" };

            ImGui.SetNextItemWidth(210);

            using (var loadDrop = ImRaii.Combo("###loadType_combo", comboNames[(int)_actor.customLoadType]))
            {
                if (loadDrop.Success)
                {
                    foreach (var name in comboNames)
                    {
                        if (ImGui.Selectable(name, name == comboNames[(int)_actor.customLoadType]))
                        {
                            var newType = (CustomLoadType)comboNames.IndexOf(name);
                            _actor.customLoadType = newType;
                        }
                    }
                }
            }

            using (ImRaii.Disabled((_actor == null || !_actor.IsLoaded()) || (!DalamudServices.clientState.IsGPosing && !IllusioVitae.InDebug())))
            {
                ImGui.Spacing();
                ImGui.Separator();

                BearGUI.Text("Animation Controller", 1.1f);

                ImGui.Checkbox("Force & Loop Base Animation Playback", ref _actor.DoNotInterupt);

                ImGui.SetNextItemWidth(50);

                ImGui.DragInt("##AnimID", ref animID);

                ImGui.SameLine();

                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    if (ImGui.Button($"{FontAwesomeIcon.SearchLocation.ToIconString()}##EmoteSearch"))
                    {
                        ImGui.OpenPopup("emoteandactionsmenu");
                    }
                }

                ImGui.SameLine();

                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    if (ImGui.Button(FontAwesomeIcon.Play.ToIconString()))
                    {
                        if (animName.Contains("_loop")) _actor.loopCurrentAnimation = true;

                        if (_actor.loopCurrentAnimation)
                        {
                            _actor.SetLoopAnimation(animID);
                        }
                        else
                        {
                            _actor.PlayAnimation(animID);
                        }
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Play Base Animation");
                }

                ImGui.SameLine();

                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    if (ImGui.Button(FontAwesomeIcon.Pause.ToIconString() + "##pausePlayinganim"))
                    {
                        _actor.PauseAnimation();
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Pause Base Animation");
                }

                ImGui.SameLine();

                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    if (_actor.loopCurrentAnimation)
                    {
                        if (ImGui.Button(FontAwesomeIcon.Repeat.ToIconString(), new(24, 22)))
                        {
                            _actor.loopCurrentAnimation = false;
                        }
                    }
                    else
                    {
                        if (ImGui.Button(FontAwesomeIcon.ArrowRightArrowLeft.ToIconString(), new(24, 22)))
                        {
                            _actor.loopCurrentAnimation = true;
                        }
                    }
                }

                if (_actor.loopCurrentAnimation)
                {
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Unloop Base Animation");
                    }
                }
                else
                {
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Loop Base Animation");
                    }
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(208);
                ImGui.Text("Base Animation Selection");

                using (var popup = ImRaii.Popup("emoteandactionsmenu"))
                {
                    if (popup.Success)
                    {
                        ImGui.Text("Search:");
                        ImGui.SameLine();

                        ImGui.SetNextItemWidth(150);
                        ImGui.InputText("##EmoteSearch", ref EmoteSearch, 1000);

                        var emoteList = GameResourceManager.Instance.Emotes.Select(x => x.Value).Where(x => (x.Name.RawString.Contains(EmoteSearch, StringComparison.OrdinalIgnoreCase)) && x.Name != "").ToList();
                        var actionList = GameResourceManager.Instance.Actions.Select(x => x.Value).Where(x => (x.Name.RawString.Contains(EmoteSearch, StringComparison.OrdinalIgnoreCase)) && x.Name != "").ToList();
                        var timelineList = GameResourceManager.Instance.ActionTimelines.Select(x => x.Value).Where(x => x.Key.RawString.Contains(EmoteSearch, StringComparison.OrdinalIgnoreCase)).ToList();

                        if (ImGui.BeginTabBar("##EmoteInformationTabbar"))
                        {

                            if (ImGui.BeginTabItem("Emotes##EmoteTabItem"))
                            {
                                using (var listbox = ImRaii.ListBox($"###listbox", new(250, 200)))
                                {
                                    if (listbox.Success)
                                    {
                                        var i = 0;
                                        foreach (var emote in emoteList)
                                        {
                                            for(var x = 0; x < 7; x++)
                                            {
                                                var currentEmote = emote.ActionTimeline[x];

                                                if (currentEmote.Value == null) continue;

                                                if (currentEmote.Value.RowId == 0) continue;

                                                i++;
                                                using (ImRaii.PushId(i))
                                                {
                                                    var startPos = ImGui.GetCursorPos();

                                                    var selected = ImGui.Selectable("###Selector", false, ImGuiSelectableFlags.None, new(0, 50));

                                                    var endPos = ImGui.GetCursorPos();

                                                    if (ImGui.IsItemVisible())
                                                    {
                                                        var icon = DalamudServices.textureProvider.GetFromGameIcon(new(emote.Icon)).GetWrapOrDefault();

                                                        if (icon == null) continue;

                                                        ImGui.SetCursorPos(new(startPos.X, startPos.Y + 5));

                                                        ImGui.Image(icon.ImGuiHandle, new(40, 40));
                                                        ImGui.SameLine();

                                                        ImGui.BeginGroup();

                                                        ImGui.Text(emote.Name.RawString + $" ({EmoteType[x]})");

                                                        ImGui.Text(currentEmote.Value.Key.ToString());

                                                        ImGui.EndGroup();

                                                        ImGui.SetCursorPos(endPos);

                                                        if (selected)
                                                        {
                                                            animID = (int)currentEmote.Value.RowId;
                                                            animName = currentEmote.Value.Key;
                                                            ImGui.CloseCurrentPopup();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                ImGui.EndTabItem();
                            }
                            if (ImGui.BeginTabItem("Actions##ActionTabItem"))
                            {
                                using (var listbox = ImRaii.ListBox($"###listbox", new(200, 200)))
                                {
                                    if (listbox.Success)
                                    {
                                        var i = 0;
                                        foreach (var action in actionList)
                                        {
                                            i++;
                                            using (ImRaii.PushId(i))
                                            {
                                                var startPos = ImGui.GetCursorPos();

                                                var selected = ImGui.Selectable("###Selector", false, ImGuiSelectableFlags.None, new(0, 50));

                                                var endPos = ImGui.GetCursorPos();

                                                if (ImGui.IsItemVisible())
                                                {
                                                    var icon = DalamudServices.textureProvider.GetFromGameIcon(new(action.Icon)).GetWrapOrDefault();

                                                    if (icon == null) continue;

                                                    ImGui.SetCursorPos(new(startPos.X, startPos.Y + 5));

                                                    ImGui.Image(icon.ImGuiHandle, new(40, 40));
                                                    ImGui.SameLine();

                                                    ImGui.BeginGroup();

                                                    ImGui.Text(action.Name.RawString);
                                                    ImGui.Text(action.ActionTimelineHit.Value.Key);

                                                    ImGui.EndGroup();

                                                    ImGui.SetCursorPos(endPos);

                                                    if (selected)
                                                    {
                                                        animID = (int)action.AnimationEnd.Value.RowId;
                                                        animName = action.ActionTimelineHit.Value.Key;
                                                        ImGui.CloseCurrentPopup();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                ImGui.EndTabItem();
                            }
                            if (ImGui.BeginTabItem("Other##TimelinesTabTiem"))
                            {
                                using (var listbox = ImRaii.ListBox($"###listbox", new(350, 200)))
                                {
                                    if (listbox.Success)
                                    {
                                        var i = 0;
                                        foreach (var timeline in timelineList)
                                        {
                                            i++;
                                            using (ImRaii.PushId(i))
                                            {
                                                var startPos = ImGui.GetCursorPos();

                                                var selected = ImGui.Selectable("###Selector", false, ImGuiSelectableFlags.None, new(0, 50));

                                                var endPos = ImGui.GetCursorPos();

                                                if (ImGui.IsItemVisible())
                                                {
                                                    var icon = GameResourceManager.Instance.GetResourceImage("UnknownSlot.png");

                                                    if (icon == null) continue;

                                                    ImGui.SetCursorPos(new(startPos.X, startPos.Y + 5));

                                                    ImGui.Image(icon.ImGuiHandle, new(40, 40));
                                                    ImGui.SameLine();

                                                    ImGui.Text(timeline.Key.RawString);

                                                    ImGui.SetCursorPos(endPos);

                                                    if (selected)
                                                    {
                                                        animID = (int)timeline.RowId;
                                                        animName = timeline.Key;
                                                        ImGui.CloseCurrentPopup();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                ImGui.EndTabItem();
                            }

                            ImGui.EndTabBar();
                        }
                    }
                }

                ImGui.SetNextItemWidth(50);

                ImGui.DragInt("##BlendAnimID", ref blendAnimdID);

                ImGui.SameLine();

                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    if (ImGui.Button($"{FontAwesomeIcon.SearchLocation.ToIconString()}##Blendsearch"))
                    {
                        ImGui.OpenPopup("blendmenu");
                    }
                }

                using (var popup = ImRaii.Popup("blendmenu"))
                {
                    if (popup.Success)
                    {
                        ImGui.Text("Search:");
                        ImGui.SameLine();

                        ImGui.SetNextItemWidth(150);
                        ImGui.InputText("##EmoteSearch", ref EmoteSearch, 1000);

                        var emoteList = GameResourceManager.Instance.Emotes.Select(x => x.Value).Where(x => (x.Name.RawString.Contains(EmoteSearch, StringComparison.OrdinalIgnoreCase)) && x.Name != "").ToList();

                        using (var listbox = ImRaii.ListBox($"###listbox", new(250, 200)))
                        {
                            if (listbox.Success)
                            {
                                var i = 0;
                                foreach (var emote in emoteList)
                                {
                                    var currentEmote = emote.ActionTimeline[4];

                                    if (currentEmote.Value == null) continue;

                                    if (currentEmote.Value.RowId == 0) continue;

                                    i++;
                                    using (ImRaii.PushId(i))
                                    {
                                        var startPos = ImGui.GetCursorPos();

                                        var selected = ImGui.Selectable("###Selector", false, ImGuiSelectableFlags.None, new(0, 50));

                                        var endPos = ImGui.GetCursorPos();

                                        if (ImGui.IsItemVisible())
                                        {
                                            var icon = DalamudServices.textureProvider.GetFromGameIcon(new(emote.Icon)).GetWrapOrDefault();

                                            if (icon == null) continue;

                                            ImGui.SetCursorPos(new(startPos.X, startPos.Y + 5));

                                            ImGui.Image(icon.ImGuiHandle, new(40, 40));
                                            ImGui.SameLine();

                                            ImGui.BeginGroup();
                                            ImGui.Text(emote.Name.RawString + $" ({EmoteType[4]})");
                                            ImGui.Text(currentEmote.Value.Key.ToString());
                                            ImGui.EndGroup();

                                            ImGui.SetCursorPos(endPos);

                                            if (selected)
                                            {
                                                blendAnimdID = (int)currentEmote.Value.RowId;
                                                ImGui.CloseCurrentPopup();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                ImGui.SameLine();

                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    if (ImGui.Button($"{FontAwesomeIcon.Play.ToIconString()}##blend"))
                    {

                        _actor.PlayAnimation(blendAnimdID);
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Play Blend Animation");
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(208);
                ImGui.Text("Blend Animation Selection");

                var previewValue = Lipstyles[lipSelect];

                ImGui.SetNextItemWidth(147);
                using (var comboBox = ImRaii.Combo($"##mouthanimation", previewValue))
                {
                    if (comboBox.Success)
                    {
                        foreach (var lipstyle in Lipstyles)
                        {
                            if (ImGui.Selectable(lipstyle, lipstyle == previewValue))
                            {
                                lipSelect = Lipstyles.ToList().IndexOf(lipstyle);                                
                            }
                        }
                    }
                }

                ImGui.SameLine();

                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    if (ImGui.Button($"{FontAwesomeIcon.Play.ToIconString()}##lips"))
                    {
                        _actor.PlayFacialAniamtion(lipIDs[lipSelect]);
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Play Mouth Animation");
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(208);
                ImGui.Text("Mouth Animation Selection");

                var raceCode = Enum.GetName(_actor.GetRaceAnimCode()) ?? "N/A";

                var selectedRace = Enum.GetNames<AnimCodes>().ToList().IndexOf(raceCode);

                if(_actor.GetModelType() == 0)
                {
                    var fixedName = Races[selectedRace];

                    ImGui.SetNextItemWidth(179);
                    using (var raceDrop = ImRaii.Combo("###race_combo", fixedName))
                    {
                        if (raceDrop.Success)
                        {
                            foreach (var raceName in Races)
                            {
                                if (ImGui.Selectable(raceName, raceName == fixedName))
                                {

                                    var SelectedAnimCode = Enum.GetNames<AnimCodes>().ToList()[Races.ToList().IndexOf(raceName)];
                                    var newRace = Enum.Parse<AnimCodes>(SelectedAnimCode);
                                    _actor.SetRaceAnimCode(newRace);

                                }
                            }
                        }
                    }
                }
                

                ImGui.SameLine();
                ImGui.SetCursorPosX(208);
                ImGui.Text("Racial Animation Override");

                ImGui.SetNextItemWidth(147);
                ImGui.DragFloat($"##animationspeed", ref _actor.animSpeed, 0.1f, 0.1f, 5);

                ImGui.SameLine();

                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
 
                    if (ImGui.Button(FontAwesomeIcon.Undo.ToIconString()))
                    {
                        _actor.animSpeed = 1;
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Reset Animation Speed");
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(208);
                ImGui.Text("Animation Speed");

                if (_actor.animPaused)
                {
                    ImGui.SetNextItemWidth(147);
                    ImGui.DragFloat($"##scrubbing", ref _actor.animScrub, .1f, 0, _actor.duration);

                    ImGui.SameLine();
                    ImGui.SetCursorPosX(208);
                    ImGui.Text("Animation Scrubbing");
                }

                if (ImGui.CollapsingHeader("Animation Slots"))
                {
                    var slotStartPos = ImGui.GetCursorPos();

                    var i = 0;
                    foreach (var slot in AnimationSlotNames)
                    {
                        var currentSpeed = _actor.GetSlotSpeed(AnimationSlotNames.ToList().IndexOf(slot));

                        if (i % 2 != 0 && i != 0)
                        {
                            ImGui.SetCursorPos(new(slotStartPos.X + 200, slotStartPos.Y));
                        }
                        else
                        {
                            slotStartPos = ImGui.GetCursorPos();
                        }

                        ImGui.BeginGroup();

                        ImGui.Text(AnimationSlotNames[i]);

                        BearGUI.Text($"{_actor.GetSlotAnimation(AnimationSlotNames.ToList().IndexOf(slot))}", .85f);

                        ImGui.SetNextItemWidth(110);
                        if (ImGui.DragFloat($"##{slot.ToString()}", ref currentSpeed, .1f, .1f, 5))
                        {
                            _actor.SetSlotSpeed(AnimationSlotNames.ToList().IndexOf(slot), currentSpeed);
                        }

                        ImGui.SameLine();

                        using (ImRaii.PushFont(UiBuilder.IconFont))
                        {
                            var icon = currentSpeed == 0 ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause;

                            if (ImGui.Button($"{icon.ToIconString()}##{AnimationSlotNames.ToList().IndexOf(slot)}"))
                            {
                                _actor.SetSlotSpeed(AnimationSlotNames.ToList().IndexOf(slot), currentSpeed == 0 ? 1 : 0);
                            }
                        }

                        ImGui.SameLine();

                        using (ImRaii.PushFont(UiBuilder.IconFont))
                        {

                            if (ImGui.Button($"{FontAwesomeIcon.Undo.ToIconString()}##{slot}slotspeeds"))
                            {
                                _actor.SetSlotSpeed(AnimationSlotNames.ToList().IndexOf(slot), 1);
                            }
                        }


                        ImGui.EndGroup();

                        i++;
                    }
                }

                if (!DalamudServices.clientState.IsGPosing && !IllusioVitae.InDebug())
                {
                    ImGui.TextColored(IVColors.Red, "Animation Control is Unavailable Outside of Group Pose");
                }

                if (IllusioVitae.InDebug())
                {

                    if(ImGui.CollapsingHeader("Debug"))
                    {
                        unsafe
                        {
                            var charAdd = _actor.actorObject.Address.ToString("X");
                            var drawObjAdd = ((nint)_actor.GetCharacterBase()).ToString("X");
                            var actorID = _actor.actorObject.EntityId.ToString("X");
                            var skeletonAdd = ((nint)_actor.GetCharacterBase()->CharacterBase.Skeleton).ToString("X");
                            var timelineAdd = _actor.GetCharacter()->Timeline;

                            ImGui.Text($"Currently Playing Animation: {_actor.currentBaseAnimation}");

                            ImGui.SetNextItemWidth(100);
                            ImGui.InputText("Actor ID", ref actorID, 100);

                            ImGui.SetNextItemWidth(100);
                            ImGui.InputText("Actor Address", ref charAdd, 100);

                            ImGui.SetNextItemWidth(100);
                            ImGui.InputText("Actor DrawObject Address", ref drawObjAdd, 100);

                            if (_actor.TryGetWeapon(DrawDataContainer.WeaponSlot.MainHand, out var mh))
                            {
                                var mhAdd = ((nint)mh).ToString("X");
                                ImGui.SetNextItemWidth(100);
                                ImGui.InputText("MainHand DrawObject Address", ref mhAdd, 100);
                            }

                            if (_actor.TryGetWeapon(DrawDataContainer.WeaponSlot.OffHand, out var oh))
                            {
                                var ohAdd = ((nint)oh).ToString("X");
                                ImGui.SetNextItemWidth(100);
                                ImGui.InputText("OffHand DrawObject Address", ref ohAdd, 100);
                            }

                            ImGui.SetNextItemWidth(100);
                            ImGui.InputText("Skeleton Address", ref skeletonAdd, 100);

                            /*
                            unsafe
                            {
                                var drawData = (ExtendedDrawDataContainer*)&_actor.GetCharacter()->DrawData;

                                var weaponData = (ExtendedWeaponData*)&drawData->weapon1;


                                var buttoNname = lockWeaponID ? "unlock" : "lock";

                                if (ImGui.Button($"{buttoNname}##weaponlock"))
                                {
                                    weapScale = weaponData->WeaponScale;
                                    weapAttachPoint = weaponData->WeaponAttachment;
                                    lockWeaponID = !lockWeaponID;
                                }

                                if (lockWeaponID)
                                {
                                    weaponData->WeaponScale = weapScale;
                                    weaponData->WeaponAttachment = weapAttachPoint;
                                }
                                

                                int x = weaponData->WeaponAnimationID;

                                if(ImGui.DragInt("animation id", ref x)){
                                    weaponData->WeaponAnimationID = (ushort)x;
                                }
                            }
                            */
                        }
                    }
                }
            }
        } 
    }
}
