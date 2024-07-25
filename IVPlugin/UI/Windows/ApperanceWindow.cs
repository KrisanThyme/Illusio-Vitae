using IVPlugin.Resources.Sheets;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Utility.Table;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;
using IVPlugin.ActorData;
using IVPlugin.ActorData.Structs;
using IVPlugin.Actors;
using IVPlugin.Core.Files;
using IVPlugin.Json;
using IVPlugin.Resources;
using IVPlugin.Services;
using IVPlugin.UI.Helpers;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using static IVPlugin.Resources.Sheets.CharaMakeTypeData;
using static Dalamud.Interface.Utility.Raii.ImRaii;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer;
using IVPlugin.Actors.Structs;
using IVPlugin.Core.Extentions;
using IVPlugin.Core;
using System.Xml.Linq;
using static IVPlugin.Core.Files.CharaFile;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;
using IVPlugin.Log;
using FFXIVClientStructs.Interop;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Dalamud.Interface.Textures;
using System.Reflection.Metadata;
using FFXIVClientStructs.FFXIV.Common.Lua;

namespace IVPlugin.UI.Windows
{
    public static class ApperanceWindow
    {
        public static bool IsOpen = false;
        public static void Show() => IsOpen = true;
        public static void Hide() => IsOpen = false;
        public static void Toggle() => IsOpen = !IsOpen;

        static XIVActor CurrentActor;

        private static CustomizeStruct customize;
        private static ShaderParams shader;

        private static MenuCollection menus;

        private static string search = "", dyeSearch = "", npcSearch = "", companionSearch = "", facewearSearch = "";

        private static bool eyelock = false, eyeShaderlock = false;

        private static string[] tribesNames = {"N/A", "Midlander", "Highlander", "Wildwood", "Duskwight", "Plainsfolk", "Dunesfolk", "Seeker of the Sun", "Keeper of the Moon", "Sea Wolf", "Hellsguard", "Raen", "Xaela", "Helions", "The Lost", "Rava", "Veena" };

        private static List<FFXIVChara> CharaFiles = new();
        public static void SetActorAndShow(ref XIVActor actor)
        {
            CurrentActor = actor;

            UpdateData(false);

            Show();
        }

        private static void UpdateData(bool updateChar = true, bool reloadChar = false, bool updateShader = true)
        {
            if (updateChar)
            {

                var scale = CurrentActor.GetActorScale();
                
                CurrentActor.ApplyCustomize(customize, reloadChar, updateShader);

                //TODO add lock
                //CurrentActor.SetActorScale(scale);
            }

            customize = CurrentActor.GetCustomizeData();

            if(CurrentActor.GetShaderParams(out ShaderParams outShader))
            {
                shader = outShader;
            }

            CharaMakeTypeData? charadata = null; 
            
            if (CurrentActor.GetModelType() == 0)
               charadata = GameResourceManager.Instance.CharaMakeTypes.Select(x => x.Value).FirstOrDefault(x => x.Race.Row == (uint)customize.Race && x.Tribe.Row == (uint)customize.Tribe && x.Gender == (Genders)customize.Gender);
            else
              charadata = GameResourceManager.Instance.CharaMakeTypes.Select(x => x.Value).FirstOrDefault(x => x.Race.Row == (uint)Races.Hyur && x.Tribe.Row == (uint)Tribes.Midlander && x.Gender == Genders.Masculine);

            if (charadata != null)
                menus = charadata.BuildMenus();

        }

        public static void Draw()
        {
            if (!IsOpen) return;

            customize = CurrentActor.GetCustomizeData();

            if (CurrentActor.GetShaderParams(out ShaderParams outShader))
            {
                shader = outShader;
            }

            var size = new Vector2(475, 900);
            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

            ImGui.SetNextWindowSizeConstraints(new (100,100), new Vector2(800, 1200));

            if (ImGui.Begin($"Concept Matrix: Appearance Editor", ref IsOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if (CurrentActor.GetModelType() != 0 && !IllusioVitae.InDebug())
                {
                    ImGui.TextColored(IVColors.Red, "Customization Limited on Monsters and Demi-Humans!");

                    ImGui.Spacing();
                    ImGui.Separator();
                }


                if (ImGui.BeginTabBar("##ApperanceTabs"))
                {
                    if (ImGui.BeginTabItem("Character Data"))
                    {
                        DrawCharacterUI();

                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Equipment Data"))
                    {
                        DrawEquipmentUI();

                        ImGui.EndTabItem();
                    }
                }

                DrawCharaUi();
            }
        }

        private static void DrawCharacterUI()
        {
            using(ImRaii.Disabled(CurrentActor.GetModelType() != 0 && !(IllusioVitae.IsDebug && IllusioVitae.InDebug())))
            {
                DrawRaceUI();
                ImGui.Separator();

                DrawDataMenu();

                ImGui.Separator();

                DrawSelectMenu();

                ImGui.Separator();

                DrawTypeMenu();

                ImGui.Separator();

                DrawColorMenu();

                ImGui.Separator();
            }

            DrawAdvancedMenu();
        }

        private static void DrawEquipmentUI()
        {
            ImGui.SetCursorPosX(15);
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button(FontAwesomeIcon.Eraser.ToIconString()))
                {
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Head, new EquipmentModelId());
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Body, new EquipmentModelId());
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Hands, new EquipmentModelId());
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Legs, new EquipmentModelId());
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Feet, new EquipmentModelId());
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Ears, new EquipmentModelId());
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Neck, new EquipmentModelId());
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Wrists, new EquipmentModelId());
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.LFinger, new EquipmentModelId());
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.RFinger, new EquipmentModelId());
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Clear Gear");
            }

            ImGui.SameLine();

            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button(FontAwesomeIcon.HatWizard.ToIconString()))
                {
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Head, new EquipmentModelId() { Id = 9903, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Body, new EquipmentModelId() { Id = 9903, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Hands, new EquipmentModelId() { Id = 9903, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Legs, new EquipmentModelId() { Id = 9903, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Feet, new EquipmentModelId() { Id = 9903, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Ears, new EquipmentModelId() { Id = 0, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Neck, new EquipmentModelId() { Id = 0, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Wrists, new EquipmentModelId() { Id = 0, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.LFinger, new EquipmentModelId() { Id = 0, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.RFinger, new EquipmentModelId() { Id = 0, Variant = 1 });
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Load NPC SmallClothes Set");
            }

            ImGui.SameLine();

            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button(FontAwesomeIcon.Socks.ToIconString()))
                {
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Head, new EquipmentModelId() { Id = 279, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Body, new EquipmentModelId() { Id = 279, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Hands, new EquipmentModelId() { Id = 279, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Legs, new EquipmentModelId() { Id = 279, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Feet, new EquipmentModelId() { Id = 279, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Ears, new EquipmentModelId() { Id = 53, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Neck, new EquipmentModelId() { Id = 53, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.Wrists, new EquipmentModelId() { Id = 53, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.LFinger, new EquipmentModelId() { Id = 53, Variant = 1 });
                    CurrentActor.SetEquipmentSlot(EquipmentSlot.RFinger, new EquipmentModelId() { Id = 53, Variant = 1 });
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Load Emperor Set");
            }

            ImGui.Separator();

            ImGui.BeginGroup();
            if (!EventManager.validCheck)
            {
                ImGui.BeginDisabled();
            }
            DrawGearSelector("Main Hand", CurrentActor.GetWeaponSlot(WeaponSlot.MainHand), ActorEquipSlot.MainHand);
            if (!EventManager.validCheck)
            {
                ImGui.EndDisabled();
            }

            DrawGearSelector("Head",CurrentActor.GetEquipmentSlot(EquipmentSlot.Head), ActorEquipSlot.Head);
            DrawGearSelector("Body",CurrentActor.GetEquipmentSlot(EquipmentSlot.Body), ActorEquipSlot.Body);
            DrawGearSelector("Hands",CurrentActor.GetEquipmentSlot(EquipmentSlot.Hands), ActorEquipSlot.Hands);
            DrawGearSelector("Legs",CurrentActor.GetEquipmentSlot(EquipmentSlot.Legs), ActorEquipSlot.Legs);
            DrawGearSelector("Feet", CurrentActor.GetEquipmentSlot(EquipmentSlot.Feet), ActorEquipSlot.Feet);

            ImGui.EndGroup();

            ImGui.SameLine();

            ImGui.SetCursorPos(new(250, ImGui.GetCursorPosY() - 3));

            ImGui.BeginGroup();
            if (!EventManager.validCheck)
            {
                ImGui.BeginDisabled();
            }

            DrawGearSelector("Off Hand",CurrentActor.GetWeaponSlot(WeaponSlot.OffHand), ActorEquipSlot.OffHand);

            if (!EventManager.validCheck)
            {
                ImGui.EndDisabled();
            }

            DrawGearSelector("Ears",CurrentActor.GetEquipmentSlot(EquipmentSlot.Ears), ActorEquipSlot.Ears);
            DrawGearSelector("Neck",CurrentActor.GetEquipmentSlot(EquipmentSlot.Neck), ActorEquipSlot.Neck);
            DrawGearSelector("Wrists",CurrentActor.GetEquipmentSlot(EquipmentSlot.Wrists), ActorEquipSlot.Wrists);
            DrawGearSelector("Right Ring", CurrentActor.GetEquipmentSlot(EquipmentSlot.RFinger), ActorEquipSlot.RightRing);
            DrawGearSelector("Left Ring",CurrentActor.GetEquipmentSlot(EquipmentSlot.LFinger), ActorEquipSlot.LeftRing);
            ImGui.EndGroup();

            ImGui.Separator();

            ImGui.BeginGroup();

            DrawFacewearSelector();

            ImGui.EndGroup();

            ImGui.SameLine();
            ImGui.SetCursorPos(new(250, ImGui.GetCursorPosY() -3));
            ImGui.BeginGroup();
            DrawCompanionSelector();
            ImGui.EndGroup();
        }

        private static void DrawRaceUI()
        {
            var racePreview = Enum.GetName(customize.Race) ?? "N/A";
            ImGui.SetNextItemWidth(94);
            using (var raceDrop = ImRaii.Combo("###race_combo", racePreview))
            {
                if (raceDrop.Success)
                {
                    var races = Enum.GetNames<Races>();
                    foreach (var raceName in races)
                    {
                        if (ImGui.Selectable(raceName, raceName == racePreview))
                        {
                            var newRace = Enum.Parse<Races>(raceName);
                            customize.Race = newRace;
                            UpdateData(true, true);

                        }
                    }
                }
            }

            ImGui.SameLine();

            using(ImRaii.Disabled(racePreview == "N/A"))
            {
                var existingTribe = customize.Tribe;
                var tribePreview = tribesNames[(int)existingTribe];
                ImGui.SetNextItemWidth(154);
                using (var tribeDrop = ImRaii.Combo("###tribe_combo", tribePreview))
                {
                    if (tribeDrop.Success)
                    {
                        var tribes = customize.GetValidTribes();
                        foreach (var tribe in tribes)
                        {
                            if (ImGui.Selectable(tribesNames[(int)tribe], tribesNames[(int)tribe] == tribePreview))
                            {
                                customize.Tribe = tribe;
                                UpdateData(true, true);
                            }
                        }
                    }
                }

                ImGui.SameLine();

                var currentGender = customize.Gender;
                var genderPreview = Enum.GetName(currentGender) ?? "N/A";
                ImGui.SetNextItemWidth(94);
                using (var genderDrop = ImRaii.Combo("###gender_combo", genderPreview))
                {
                    if (genderDrop.Success)
                    {
                        var genders = customize.GetValidGenders();
                        foreach (var gender in genders)
                        {
                            if (ImGui.Selectable(gender.ToString(), gender == currentGender))
                            {
                                customize.Gender = gender;
                                UpdateData(true, true);
                            }
                        }
                    }
                }

                ImGui.SameLine();

                var currentAge = customize.Age;
                var typePreview = Enum.GetName(currentAge) ?? "N/A";
                ImGui.SetNextItemWidth(77);
                using (var typeDrop = Combo("###type_combo", typePreview))
                {
                    if (typeDrop.Success)
                    {
                        var types = customize.GetValidAges();
                        foreach (var age in types)
                        {
                            if (ImGui.Selectable(age.ToString(), age == currentAge))
                            {
                                customize.Age = age;
                                UpdateData(true, true);
                            }
                        }
                    }
                }
            }
        }

        private static void DrawDataMenu()
        {
            bool SmallIrisCheck = customize.HasSmallIris;
            bool flippedFacePaint = customize.FacepaintFlipped;
            bool hasLipColor = customize.LipColorEnabled;
            bool hasHairHighlights = customize.HighlightsEnabled;

            int heightValue = customize.Height;
            int bustValue = customize.BustSize;

            if (ImGui.Checkbox("Small Iris##chara_irisSize_id", ref SmallIrisCheck))
            {
                customize.HasSmallIris = SmallIrisCheck;
                UpdateData(true);

            }

            ImGui.SameLine();

            if (ImGui.Checkbox("Mirror Face Paint##chara_flipFacePaint_Id", ref flippedFacePaint))
            {
                customize.FacepaintFlipped = flippedFacePaint;
                UpdateData(true);

            }


            if (customize.Race != Races.Hrothgar)
            {
                ImGui.SameLine();

                if (ImGui.Checkbox("Lip Color##chara_lipColor_id", ref hasLipColor))
                {
                    customize.LipColorEnabled = hasLipColor;
                    UpdateData(true);

                }

                ImGui.SameLine();

                if (ImGui.Checkbox("Hair Highlights##chara_Highlights_id", ref hasHairHighlights))
                {
                    customize.HighlightsEnabled = hasHairHighlights;
                    UpdateData(true);

                }
            }
            else
            {
                ImGui.SameLine();

                if (ImGui.Checkbox("Fur Pattern##chara_furPattern_checkbox", ref hasHairHighlights))
                {
                    customize.HighlightsEnabled = hasHairHighlights;
                    UpdateData(true);

                }
            }

            ImGui.SetNextItemWidth(100);
            if (ImGui.SliderInt("Height##height_int", ref heightValue, 1, 100))
            {
                CurrentActor.scaleLock = false;
                customize.Height = (byte)heightValue;
                UpdateData(true);
            }

            if (customize.Gender == Genders.Feminine)
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetItemRectSize().X + 95);
                ImGui.SetNextItemWidth(100);
                if (ImGui.SliderInt("Bust Size##bust_int", ref bustValue, 1, 100))
                {
                    customize.BustSize = (byte)bustValue;
                    UpdateData(true);
                }
            }
        }

        private static void DrawSelectMenu()
        {
            var multiItem = false;

            var faceFeaturemenu = menus.GetMenuForCustomize(CustomizeIndex.FaceFeatures);
            var faceTypesMenu = menus.GetMenuForCustomize(CustomizeIndex.FaceType);
            var facePaintMenu = menus.GetMenuForCustomize(CustomizeIndex.Facepaint);
            var hairStyleMenu = menus.GetMenuForCustomize(CustomizeIndex.HairStyle);

            List<CharaMakeCustomize> hairStyles = GameResourceManager.Instance.HairMakeTypes[hairStyleMenu.CharaMakeRow].HairStyles.Where(x => x.Row != 0).Select(x => x.Value!)!.ToList();
            var facePaints = GameResourceManager.Instance.HairMakeTypes[facePaintMenu.CharaMakeRow].FacePaints.Where(x => x.Row != 0).Select(x => x.Value!)!;
            List<GenericItem> faces = faceTypesMenu.SubParams.Select((x, i) => new GenericItem() { id = (byte)(i + 1), icon = (uint)x }).ToList();

            if(customize.Race == Races.Hrothgar && customize.Gender == Genders.Masculine)
            {
                List<GenericItem> tempFaceList = new();

                for(var i = 0; i < faces.Count; i++)
                {
                    GenericItem temp = new()
                    {
                        icon = faces[i].icon,
                        id = (byte)(faces[i].id + 4),
                    };

                    tempFaceList.Add(temp);
                }

                faces.AddRange(tempFaceList);

                List<CharaMakeCustomize> tempHairList = new();

                for(var i = 1; i < 9; i++)
                {
                    CharaMakeCustomize tempCustomize = new();

                    tempCustomize.FeatureID = (byte)i;

                    tempCustomize.Icon = hairStyles.FirstOrDefault(f => f?.FeatureID == (byte)(i + 8), null).Icon;

                    tempHairList.Add(tempCustomize);
                }

                hairStyles.InsertRange(0, tempHairList);
            }

            int faceIDX = customize.FaceType;
            int facePaintIDX = customize.RealFacepaint;
            int hairStyleIDX = customize.HairStyle;
            uint highestValidhairstyle = hairStyles.Last().FeatureID;

            int facialFeatureIDX = 0;
            unsafe
            {
                facialFeatureIDX = customize.Data[(int)CustomizeIndex.FaceFeatures];
            }

            if (faceIDX < 1)
            {
                faceIDX = 1;
            }

            int hrothFaceIdX = faceIDX;

            if (customize.Race == Races.Hrothgar&& hrothFaceIdX > 4)
            {
                hrothFaceIdX -= 4;
            }

            if (facePaintIDX < 0)
            {
                facePaintIDX = 0;
            }

            if (facePaintIDX > facePaints.ToList().Count - 1)
            {
                facePaintIDX = 0;
            }

            if (hairStyleIDX < 1)
            {
                hairStyleIDX = 1;
            }

            multiItem = faceTypesMenu.Type == MenuType.MultiItemSelect ? true : false;

            uint facePaintIcon = facePaints.FirstOrDefault(f => f?.FeatureID == facePaintIDX, null)?.Icon ?? 0;
            uint faceTypeIcon = faces.FirstOrDefault(x => !multiItem && x.id == (customize.Race == Races.Hrothgar ? hrothFaceIdX : faceIDX) || multiItem && (x.id & (customize.Race == Races.Hrothgar ? hrothFaceIdX : faceIDX)) != 0).icon;

            uint hairStyleIcon = hairStyles.FirstOrDefault(f => f?.FeatureID == hairStyleIDX, null)?.Icon ?? 0;

            List<ValidFeature> validFeatures = new();

            nint featureIcon = nint.Zero;

            if ((customize.Race == Races.Hrothgar ? hrothFaceIdX : faceIDX) <= faces.Count)
            {
                for (int i = 0; i < 7; ++i)
                {
                    uint featureType = (uint)faceFeaturemenu.FacialFeatures[(customize.Race == Races.Hrothgar ? hrothFaceIdX : faceIDX) - 1, i];

                    if (i == 4 && (customize.Race == Races.Hrothgar && customize.Gender == Genders.Masculine))
                    {
                        if(hairStyleIDX < 9)
                        {
                            featureType = hairStyles.FirstOrDefault(f => f?.FeatureID == hairStyleIDX + 16, null)?.Icon ?? 0;
                        }
                        else
                        {
                            featureType = 0;
                        }
                    }

                    validFeatures.Add(new() { featureID = i, icon = featureType });
                }

                featureIcon = DalamudServices.textureProvider.GetFromGameIcon(new GameIconLookup(validFeatures[0].icon)).GetWrapOrEmpty().ImGuiHandle;
            }
            else
            {
                featureIcon = GameResourceManager.Instance.GetResourceImage("EmptySlot.png").ImGuiHandle;
            }

            ImGui.BeginGroup();

            DrawSelector(faceTypeIcon, "faceType", "Face Type", faces, ref faceIDX, ref customize.FaceType);

            ImGui.SameLine();

            ImGui.SetCursorPos(new(ImGui.GetCursorPosX() + 70,ImGui.GetCursorPosY() - 3));

            DrawSelector(hairStyleIcon, "hairstyle", "Hair Style", hairStyles.ToList(), ref hairStyleIDX, ref customize.HairStyle);

            DrawFacePaintSelector(facePaintIcon, "facepaint", "Face Paint", facePaints.ToList(), ref facePaintIDX);

            ImGui.SameLine();

            ImGui.SetCursorPos(new(ImGui.GetCursorPosX() + 70, ImGui.GetCursorPosY() - 3));

            ImGui.BeginGroup();

            if(facialFeatureIDX < 0)
            {
                facialFeatureIDX = 0;
            }

            ImGui.Text("Features");

            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt($"##facefeature_id", ref facialFeatureIDX, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsScientific))
            {
                unsafe
                {
                    customize.Data[(int)CustomizeIndex.FaceFeatures] = (byte)facialFeatureIDX;
                }

                UpdateData();
            }

            ImGui.SameLine();

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 20);

            var pos = ImGui.GetCursorPos();

            if (BearGUI.ImageButton($"facialFeature", featureIcon, new Vector2(48, 46)))
            {
                ImGui.OpenPopup($"FaceFeaturePopup");
            }

            ImGui.EndGroup();

            using (var popup = Popup("FaceFeaturePopup"))
            {
                if (popup.Success)
                {
                    for (var i = 0; i < validFeatures.Count; i++)
                    {
                        DrawFeatureButton(validFeatures[i]);

                        if (i != 3)
                            ImGui.SameLine();
                    }

                    DrawFeatureButton(new() { featureID = 7 });
                }
            }

            ImGui.EndGroup();

            int raceFeatureTypeIDX = customize.RaceFeatureType;
            int raceFeatureSizeIDX = customize.RaceFeatureSize;

            string featureTypeName = string.Empty;

            if (customize.Race == Races.Hrothgar || customize.Race == Races.Miqote || customize.Race == Races.AuRa)
            {
                featureTypeName = "Tail Type";
            }

            if (customize.Race == Races.Hrothgar || customize.Race == Races.Miqote || customize.Race == Races.AuRa)
            {
                multiItem = false;

                var RacialFeatureMenu = menus.GetMenuForCustomize(CustomizeIndex.RaceFeatureType);

                if (RacialFeatureMenu == null) return;

                var tails = RacialFeatureMenu.SubParams.Select((x, i) => new GenericItem() { id = (byte)(i + 1), icon = (uint)x }).ToList();

                if (raceFeatureTypeIDX < 1)
                {
                    raceFeatureTypeIDX = 1;
                }

                if (raceFeatureTypeIDX > tails.ToList().Count)
                {
                    raceFeatureTypeIDX = tails.ToList().Count;
                }

                int currentTailIDX = raceFeatureTypeIDX;

                multiItem = RacialFeatureMenu.Type == MenuType.MultiItemSelect ? true : false;

                uint tailIcon = tails.FirstOrDefault(x => !multiItem && x.id == currentTailIDX || multiItem && (x.id & currentTailIDX) != 0).icon;

                DrawSelector(tailIcon, "raceFeatureType", featureTypeName, tails, ref raceFeatureTypeIDX, ref customize.RaceFeatureType);

                if (customize.Race == Races.Hrothgar)
                {

                    ImGui.SameLine();
                    ImGui.SetCursorPos(new(ImGui.GetCursorPosX() + 70, ImGui.GetCursorPosY() - 3));

                    int furPatternIDX = customize.LipColor;

                    var isMultiItem = false;

                    var furPatternsMenu = menus.GetMenuForCustomize(CustomizeIndex.LipColor);

                    var furPatterns = furPatternsMenu.SubParams.Select((x, i) => new GenericItem() { id = (byte)(i + 1), icon = (uint)x }).ToList();

                    if (furPatternIDX > furPatterns.Count)
                    {
                        furPatternIDX = furPatterns.Count;
                    }

                    if (furPatternIDX < 1)
                    {
                        furPatternIDX = 1;
                    }

                    isMultiItem = furPatternsMenu.Type == MenuType.MultiItemSelect ? true : false;

                    uint furIcon = furPatterns.FirstOrDefault(x => !isMultiItem && x.id == furPatternIDX || isMultiItem && (x.id & furPatternIDX) != 0).icon;

                    DrawSelector(furIcon, "furpattern", "Fur Pattern", furPatterns, ref furPatternIDX, ref customize.LipColor);
                }
            }
        }
        
        private static void DrawTypeMenu()
        {
            var jawMenu = menus.GetMenuForCustomize(CustomizeIndex.JawShape);
            var eyeMenu = menus.GetMenuForCustomize(CustomizeIndex.EyeShape);
            var eyebrowMenu = menus.GetMenuForCustomize(CustomizeIndex.Eyebrows);
            var noseMenu = menus.GetMenuForCustomize(CustomizeIndex.NoseShape);
            var lipsMenu = menus.GetMenuForCustomize(CustomizeIndex.LipStyle);
            var voiceData = GameResourceManager.Instance.VoiceData;

            int jawIDX = customize.JawShape;
            int eyeIDX = customize.RealEyeShape;
            int eyeBrowIDX = customize.Eyebrows;
            int noseIDX = customize.NoseShape;
            int lipsIDX = customize.LipStyle;
            int raceFeatureTypeIDX = customize.RaceFeatureType;
            int raceFeatureSizeIDX = customize.RaceFeatureSize;
            int voiceIDX = CurrentActor.GetVoice();

            if (customize.LipColorEnabled) lipsIDX -= 128;

            if (jawIDX > jawMenu.SubParams.Length - 1) jawIDX = jawMenu.SubParams.Length - 1;
            if (eyeIDX > eyeMenu.SubParams.Length - 1) eyeIDX = eyeMenu.SubParams.Length - 1;
            if (eyeBrowIDX > eyebrowMenu.SubParams.Length - 1) eyeBrowIDX = eyebrowMenu.SubParams.Length - 1;
            if (noseIDX > noseMenu.SubParams.Length - 1) noseIDX = noseMenu.SubParams.Length - 1;
            if (lipsIDX > lipsMenu.SubParams.Length - 1) lipsIDX = lipsMenu.SubParams.Length - 1;

            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt("Jaw Shape##chara_jaw_id", ref jawIDX, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsScientific))
            {
                if (jawIDX > jawMenu.SubParams.Length - 1) jawIDX = jawMenu.SubParams.Length - 1;
                if (jawIDX == -1) jawIDX = 0;
                customize.JawShape = (byte)jawIDX;
                UpdateData(true);
            }

            ImGui.SameLine();
            var initScale = ImGui.GetItemRectSize().X;
            ImGui.SetCursorPosX(initScale + 75);

            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt("Eye Shape##chara_eyeshape_id", ref eyeIDX, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsScientific))
            {
                if (eyeIDX > eyeMenu.SubParams.Length - 1) eyeIDX = eyeMenu.SubParams.Length - 1;
                if (eyeIDX == -1) eyeIDX = 0;
                customize.RealEyeShape = (byte)eyeIDX;
                UpdateData(true);
            }

            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt("Eyebrow Shape##chara_eyebrow_id", ref eyeBrowIDX, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsScientific))
            {
                if (eyeBrowIDX > eyebrowMenu.SubParams.Length - 1) eyeBrowIDX = eyebrowMenu.SubParams.Length - 1;
                if (eyeBrowIDX == -1) eyeBrowIDX = 0;
                customize.Eyebrows = (byte)eyeBrowIDX;
                UpdateData(true);
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(initScale + 75);

            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt("Nose Shape##chara_nose_id", ref noseIDX, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsScientific))
            {
                if (noseIDX > noseMenu.SubParams.Length - 1) noseIDX = noseMenu.SubParams.Length - 1;
                if (noseIDX == -1) noseIDX = 0;
                customize.NoseShape = (byte)noseIDX;
                UpdateData(true);
            }

            string mouthText = string.Empty;

            if (customize.Race == Races.Hrothgar)
            {
                mouthText = "Fang Length";
            }
            else
            {
                mouthText = "Lip Shape";
            }

            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt($"{mouthText}##chara_mouth_id", ref lipsIDX, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsScientific))
            {
                if (lipsIDX > lipsMenu.SubParams.Length - 1) lipsIDX = lipsMenu.SubParams.Length - 1;
                if (lipsIDX == -1) lipsIDX = 0;
                customize.RealLipStyle = (byte)lipsIDX;
                UpdateData(true);
            }

            using(ImRaii.Disabled(CurrentActor.actorObject.ObjectKind != ObjectKind.Player))
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(initScale + 75);

                ImGui.SetNextItemWidth(48);
                if (ImGui.InputInt($"##chara_voice_id", ref voiceIDX, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsScientific))
                {
                    CurrentActor.SetVoice((ushort)voiceIDX);
                }

                ImGui.SameLine();

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 4);

                if (ImGui.Button("Select##chara_voice", new(48, 22)))
                {
                    ImGui.OpenPopup("Voice Menu Popup");
                }

                using (var popup = ImRaii.Popup("Voice Menu Popup"))
                {
                    if (popup.Success)
                    {


                        if (ImGui.BeginChild($"##voicePopupChild", new Vector2(350, 400), true, ImGuiWindowFlags.AlwaysVerticalScrollbar))
                        {
                            for (var x = 0; x < voiceData.Count; x++)
                            {
                                var data = voiceData[x];

                                ImGui.BeginGroup();
                                ImGui.Text($"{data.race} {tribesNames[(int)data.tribe]} - {data.gender.ToString()} Voices:");

                                ImGui.Separator();

                                for (var i = 0; i < data.availableVoices.Length; i++)
                                {
                                    if (i % 4 == 0)
                                    {
                                        if (i != 0)
                                        {
                                            ImGui.EndGroup();
                                        }
                                        ImGui.BeginGroup();
                                    }
                                    else
                                    {
                                        ImGui.SameLine();
                                    }


                                    if (ImGui.Button($"Voice #{(i + 1).ToString("d2")}##{data.race}{data.tribe}{data.gender}{i}"))
                                    {
                                        CurrentActor.SetVoice(data.availableVoices[i]);
                                        ImGui.CloseCurrentPopup();
                                    }
                                }


                                ImGui.EndGroup();

                                ImGui.Spacing();
                                ImGui.Spacing();
                            }

                            ImGui.EndChild();
                        }
                    }
                }

                ImGui.SameLine();

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 4);

                ImGui.Text("Voice");
            }
            
            string featureTypeName = string.Empty;
            string featureSizeName = "Muscle Tone";

            if (customize.Race == Races.Hrothgar || customize.Race == Races.Miqote || customize.Race == Races.AuRa)
            {
                featureSizeName = "Tail Size";
            }

            if (customize.Race == Races.Elezen || customize.Race == Races.Lalafel || customize.Race == Races.Viera)
            {
                featureTypeName = "Ear Type";
                featureSizeName = "Ear Size";
            }

            if (customize.Race == Races.Elezen || customize.Race == Races.Lalafel || customize.Race == Races.Viera)
            {
                if(raceFeatureTypeIDX > 4)
                {
                    raceFeatureTypeIDX = 4;
                }

                if(raceFeatureTypeIDX < 1)
                    raceFeatureTypeIDX = 1;

                ImGui.SetNextItemWidth(100);
                if (ImGui.InputInt("Ear Type##chara_earType_id", ref raceFeatureTypeIDX, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsScientific))
                {
                    customize.RaceFeatureType = (byte)raceFeatureTypeIDX;
                    UpdateData(true);
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(initScale + 75);
            }

            ImGui.SetNextItemWidth(100);
            if (ImGui.SliderInt($"{featureSizeName}##chara_raceFeatureSize_id", ref raceFeatureSizeIDX, 1, 100))
            {
                customize.RaceFeatureSize = (byte)raceFeatureSizeIDX;
                UpdateData(true);
            }
        }

        private static void DrawColorMenu()
        {
            var skinColors = GameResourceManager.Instance.HumanData.GetSkinColors(customize.Tribe, customize.Gender);
            var lipColors = GameResourceManager.Instance.HumanData.GetLipColors();
            var facePaintColors = GameResourceManager.Instance.HumanData.GetFacepaintColors();
            var eyeColors = GameResourceManager.Instance.HumanData.GetEyeColors();
            var hairColors = GameResourceManager.Instance.HumanData.GetHairColors(customize.Tribe, customize.Gender);
            var hairHighlightColors = GameResourceManager.Instance.HumanData.GetHairHighlightColors();

            var currentFaceFeatureColorIDX = customize.FaceFeaturesColor;
            var currentFaceFeatureColor = eyeColors.Length > currentFaceFeatureColorIDX ? eyeColors[currentFaceFeatureColorIDX] : 0;

            int currentSkinColorIDX = customize.SkinTone;
            var currentSkinColor = skinColors.Length > currentSkinColorIDX ? skinColors[currentSkinColorIDX] : 0;

            int currentlipColorIDX = customize.LipColor;
            var currentLipColor = lipColors.Length > currentlipColorIDX ? lipColors[currentlipColorIDX] : 0;

            int currentFacePaintColorIDX = customize.FacePaintColor;
            var currentFacePaintColor = facePaintColors.Length > currentFacePaintColorIDX ? facePaintColors[currentFacePaintColorIDX] : 0;

            int currentlEyeColorIDX = customize.LEyeColor;
            var currentlEyeColor = eyeColors.Length > currentlEyeColorIDX ? eyeColors[currentlEyeColorIDX] : 0;

            int currentrEyeColorIDX = customize.REyeColor;
            var currentrEyeColor = eyeColors.Length > currentrEyeColorIDX ? eyeColors[currentrEyeColorIDX] : 0;

            int currentHairColorIdx = customize.HairColor;
            var currentHairColor = hairColors.Length > currentHairColorIdx ? hairColors[currentHairColorIdx] : 0;

            int currentHairHighlightColorIdx = customize.HairHighlightColor;
            var currentHairHighlightColor = hairHighlightColors.Length > currentHairHighlightColorIdx ? hairHighlightColors[currentHairHighlightColorIdx] : 0;

            bool hasLipColor = customize.LipColorEnabled;
            bool hasHairHighlights = customize.HighlightsEnabled;

            ImGui.BeginGroup();

            if (BearGUI.ColoredLableButton($"skinColor", currentSkinColor, currentSkinColorIDX.ToString()))
            {
                ImGui.OpenPopup("skinColorsPopup");
            }

            if (ImGui.IsItemHovered())
            {
                if (customize.Race != Races.Hrothgar)
                {
                    ImGui.SetTooltip("Skin Color");
                }
                else
                {
                    ImGui.SetTooltip("Fur Color");
                }
            }

            DrawColorPopup("skinColorsPopup", skinColors, ref customize.SkinTone, shaderLockType.SkinColor);

            ImGui.SameLine();

            if (hasLipColor && customize.Race != Races.Hrothgar)
            {
                if (BearGUI.ColoredLableButton($"lipColor", currentLipColor, currentlipColorIDX.ToString()))
                {
                    ImGui.OpenPopup("lipColorPopup");
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Lip Color");
                }

                DrawColorPopup("lipColorPopup", lipColors, ref customize.LipColor, shaderLockType.LipColor);

                ImGui.SameLine();

                ImGui.Text("Skin Colors");
            }
            else if (customize.Race != Races.Hrothgar)
            {
                ImGui.Text("Skin Colors");
            }
            else if (customize.Race == Races.Hrothgar)
            {
                ImGui.Text("Fur Color");
            }

            ImGui.EndGroup();

            ImGui.SameLine();
            var initScale = ImGui.GetItemRectSize().X;
            if (hasLipColor && customize.Race != Races.Hrothgar)
            {
                ImGui.SetCursorPosX(initScale + 60);
            }
            else if (customize.Race != Races.Hrothgar)
            {
                ImGui.SetCursorPosX(initScale + 103);
            }
            else if (customize.Race == Races.Hrothgar)
            {
                ImGui.SetCursorPosX(initScale + 115);
            }

            unsafe
            {
                var currentIcon = eyelock ? GameResourceManager.Instance.GetResourceImage("Linked.png").ImGuiHandle : GameResourceManager.Instance.GetResourceImage("Unlinked.png").ImGuiHandle;

                if (BearGUI.ImageButton($"###eyecolorLock", currentIcon, new(22, 22)))
                {
                    eyelock = !eyelock;

                    if (eyelock)
                    {
                        customize.REyeColor = customize.LEyeColor;
                        CurrentActor.UpdateShaderLocks(shaderLockType.REyeColor, false);
                    }

                    UpdateData();
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Link Eye Colors");
            }

            ImGui.SameLine();

            if (BearGUI.ColoredLableButton($"leyeColor", currentlEyeColor, currentlEyeColorIDX.ToString()))
            {
                ImGui.OpenPopup("leyeColorPopup");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Left Eye Color");
            }

            DrawColorPopup("leyeColorPopup", eyeColors, ref customize.LEyeColor, shaderLockType.LEyeColor);

            ImGui.SameLine();

            if (BearGUI.ColoredLableButton($"reyeColor", currentrEyeColor, currentrEyeColorIDX.ToString()))
            {
                if (eyelock) ImGui.OpenPopup("leyeColorPopup");
                else ImGui.OpenPopup("ReyeColorPopup");

            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Right Eye Color");
            }

            DrawColorPopup("ReyeColorPopup", eyeColors, ref customize.REyeColor, shaderLockType.REyeColor);

            ImGui.SameLine();

            ImGui.Text("Eye Colors");

            ImGui.BeginGroup();

            string hairColorName = "Hair Colors";

            if (customize.Race == Races.Hrothgar)
            {
                hairColorName = "Hair & Pattern Colors";
            }

            if (BearGUI.ColoredLableButton($"mainHairColor", currentHairColor, currentHairColorIdx.ToString()))
            {
                ImGui.OpenPopup("HairColorsPopup");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Hair Color");
            }

            DrawColorPopup("HairColorsPopup", hairColors, ref customize.HairColor, shaderLockType.HairColor);

            if (hasHairHighlights)
            {
                ImGui.SameLine();

                if (BearGUI.ColoredLableButton($"highlightHairColor", currentHairHighlightColor, currentHairHighlightColorIdx.ToString()))
                {
                    ImGui.OpenPopup("HairHighlightsColorsPopup");
                }

                if (ImGui.IsItemHovered())
                {
                    if (customize.Race != Races.Hrothgar)
                    {
                        ImGui.SetTooltip("Hair Highlights Color");
                    }
                    else
                    {
                        ImGui.SetTooltip("Pattern Color");
                    }
                }

                DrawColorPopup("HairHighlightsColorsPopup", hairHighlightColors, ref customize.HairHighlightColor, shaderLockType.HighlightColor);
            }

            ImGui.SameLine();
            ImGui.Text(hairColorName);
            ImGui.SameLine();

            if (hasLipColor && customize.Race != Races.Hrothgar)
            {
                ImGui.SetCursorPosX(initScale + 90);
            }
            else if (customize.Race != Races.Hrothgar)
            {
                ImGui.SetCursorPosX(initScale + 133);
            }
            else if (customize.Race == Races.Hrothgar)
            {
                ImGui.SetCursorPosX(initScale + 145);
            }

            if (BearGUI.ColoredLableButton($"faceFeatureColor", currentFaceFeatureColor, currentFaceFeatureColorIDX.ToString()))
            {
                ImGui.OpenPopup("faceFeatureColorPopup");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Feature Color");
            }

            DrawColorPopup("faceFeatureColorPopup", eyeColors, ref customize.FaceFeaturesColor, shaderLockType.FeatureColor);

            ImGui.SameLine();

            if (BearGUI.ColoredLableButton($"facePaintColor", currentFacePaintColor, currentFacePaintColorIDX.ToString()))
            {
                ImGui.OpenPopup("facePaintPopup");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Face Paint Color");
            }

            DrawColorPopup("facePaintPopup", facePaintColors, ref customize.FacePaintColor, shaderLockType.None);

            ImGui.SameLine();

            ImGui.Text("Feature Colors");

            ImGui.EndGroup();
        }

        private static void DrawAdvancedMenu()
        {

            BearGUI.Text("Advanced Shader & Appearance Overrides", 1.1f);

            var alpha = CurrentActor.GetTransparency();
            var wetness = CurrentActor.GetWetness();
            var tint = CurrentActor.GetTint();
            var size = CurrentActor.GetActorScale();
            var modelIDX = CurrentActor.GetModelType();
            var mhTint = CurrentActor.GetWeaponTint(WeaponSlot.MainHand);
            var ohTint = CurrentActor.GetWeaponTint(WeaponSlot.OffHand);

            using (ImRaii.Disabled(!CurrentActor.IsLoaded() || !EventManager.validCheck && !(IllusioVitae.IsDebug && IllusioVitae.InDebug())))
            {
                ImGui.BeginGroup();

                ImGui.SetNextItemWidth(125);
                if (ImGui.DragInt("Model ID##chara_model_id", ref modelIDX, 0, 0))
                    CurrentActor.SetModelType((uint)modelIDX, true);

                ImGui.SetNextItemWidth(125);
                if (ImGui.DragFloat("Visibility##alphaValue", ref alpha, 0.1f, 0, 1))
                {
                    CurrentActor.SetTransparency(alpha);
                }

                unsafe
                {
                    var currentIcon = CurrentActor.lockWetness ? GameResourceManager.Instance.GetResourceImage("Locked.png").ImGuiHandle : GameResourceManager.Instance.GetResourceImage("Unlocked.png").ImGuiHandle;

                    if (BearGUI.ImageButton($"###wetnessLock", currentIcon, new(22, 22)))
                    {
                        CurrentActor.lockWetness = !CurrentActor.lockWetness;
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Lock Wetness");
                }

                ImGui.SameLine();

                ImGui.SetNextItemWidth(95);
                if (ImGui.DragFloat("Wetness##WetnessValue", ref wetness, 0.1f, 0, 1.01f))
                {
                    CurrentActor.SetWetness(wetness);
                }

                using(ImRaii.Disabled(CurrentActor.GetModelType() != 0 && !(IllusioVitae.IsDebug && IllusioVitae.InDebug())))
                {
                    ImGui.SetNextItemWidth(125);
                    if (ImGui.DragFloat("Muscle Tone##muscletone", ref shader.MuscleTone, 0.1f, 0, 3))
                    {
                        CurrentActor.UpdateShaderLocks(shaderLockType.MuscleTone, true);
                        CurrentActor.ApplyShaderparams(shader);
                    }
                }
                
                ImGui.SetNextItemWidth(125);
                if (ImGui.DragFloat("Scale##SizeValue", ref size, 0.1f, 0.1f, 15))
                {
                    CurrentActor.SetActorScale(size);
                }

                ImGui.EndGroup();
            }

            using(ImRaii.Disabled(!CurrentActor.IsLoaded() || CurrentActor.GetModelType() != 0 && !(IllusioVitae.IsDebug && IllusioVitae.InDebug())))
            {
                ImGui.SameLine();

                ImGui.SetCursorPos(new(ImGui.GetCursorPosX() + 15, ImGui.GetCursorPosY()));

                using (ImRaii.Disabled())

                    ImGui.BeginGroup();

                if (customize.Race != Races.Hrothgar)
                {
                    if (ImGui.ColorButton("Skin Color##rawskinColorButton", new Vector4(shader.SkinColor, 1)))
                    {
                        ImGui.OpenPopup("skinRGBPicker");
                    }
                }
                else
                {
                    if (ImGui.ColorButton("Fur Color##rawskinColorButton", new Vector4(shader.SkinColor, 1)))
                    {
                        ImGui.OpenPopup("skinRGBPicker");
                    }
                }

                using (var popup = ImRaii.Popup("skinRGBPicker"))
                {
                    if (popup.Success)
                    {
                        if (customize.Race != Races.Hrothgar)
                        {
                            if (ImGui.ColorPicker3("Skin Color##rgbWheel", ref shader.SkinColor))
                            {
                                CurrentActor.UpdateShaderLocks(shaderLockType.SkinColor, true);
                                CurrentActor.ApplyShaderparams(shader);
                            }
                        }
                        else
                        {
                            if (ImGui.ColorPicker3("Fur Color##rgbWheel", ref shader.SkinColor))
                            {
                                CurrentActor.UpdateShaderLocks(shaderLockType.SkinColor, true);
                                CurrentActor.ApplyShaderparams(shader);
                            }
                        }
                    }
                }

                ImGui.SameLine();

                //ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4);

                if (customize.Race != Races.Hrothgar)
                {

                    if (ImGui.ColorButton("Skin Gloss Color##rawskinGlossColorButton", new Vector4(shader.SkinGloss, 1)))
                    {
                        ImGui.OpenPopup("skinGlossRGBPicker");
                    }
                }
                else
                {
                    if (ImGui.ColorButton("Fur Gloss Color##rawskinGlossColorButton", new Vector4(shader.SkinGloss, 1)))
                    {
                        ImGui.OpenPopup("skinGlossRGBPicker");
                    }
                }

                ImGui.SameLine();

                //ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4);

                if (customize.Race != Races.Hrothgar)
                {
                    if (ImGui.ColorButton("Lip Color##rawlipsColorButton", shader.MouthColor))
                    {
                        ImGui.OpenPopup("lipsRGBPicker");
                    }

                    using (var popup = ImRaii.Popup("lipsRGBPicker"))
                    {
                        if (popup.Success)
                        {
                            if (ImGui.ColorPicker4("Lip Color##rgbWheel", ref shader.MouthColor))
                            {
                                CurrentActor.UpdateShaderLocks(shaderLockType.LipColor, true);
                                CurrentActor.ApplyShaderparams(shader);
                            }
                        }
                    }
                }

                ImGui.SameLine();

                //ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4);

                if (customize.Race != Races.Hrothgar)
                {
                    ImGui.Text("Skin Colors");
                }
                else
                {
                    ImGui.Text("Fur Colors");
                }

                using (var popup = ImRaii.Popup("skinGlossRGBPicker"))
                {
                    if (popup.Success)
                    {
                        if (customize.Race != Races.Hrothgar)
                        {
                            if (ImGui.ColorPicker3("Skin Gloss Color##rgbWheel", ref shader.SkinGloss))
                            {
                                CurrentActor.UpdateShaderLocks(shaderLockType.SkinColor, true);
                                CurrentActor.ApplyShaderparams(shader);
                            }
                        }
                        else
                        {
                            if (ImGui.ColorPicker3("Fur Gloss Color##rgbWheel", ref shader.SkinGloss))
                            {
                                CurrentActor.UpdateShaderLocks(shaderLockType.SkinColor, true);
                                CurrentActor.ApplyShaderparams(shader);
                            }
                        }
                    }
                }

                if (ImGui.ColorButton("Hair Color##rawhairColorButton", new Vector4(shader.HairColor, 1)))
                {
                    ImGui.OpenPopup("hairRGBPicker");
                }

                using (var popup = ImRaii.Popup("hairRGBPicker"))
                {
                    if (popup.Success)
                    {
                        if (ImGui.ColorPicker3("HairColor##rgbWheel", ref shader.HairColor))
                        {
                            CurrentActor.UpdateShaderLocks(shaderLockType.HairColor, true);
                            CurrentActor.ApplyShaderparams(shader);
                        }
                    }
                }

                ImGui.SameLine();

                if (ImGui.ColorButton("Hair Gloss##rawhairColorGlossButton", new Vector4(shader.HairGloss, 1)))
                {
                    ImGui.OpenPopup("hairGlossRGBPicker");
                }

                ImGui.SameLine();

                if (customize.Race != Races.Hrothgar)
                {
                    if (ImGui.ColorButton("Hair Highlights Color##rawHairHighlightColorButton", new Vector4(shader.HairHighlight, 1)))
                    {
                        ImGui.OpenPopup("HairHighlightRGBPicker");
                    }
                }
                else
                {
                    if (ImGui.ColorButton("Pattern Color##rawHairHighlightColorButton", new Vector4(shader.HairHighlight, 1)))
                    {
                        ImGui.OpenPopup("HairHighlightRGBPicker");
                    }
                }

                using (var popup = ImRaii.Popup("HairHighlightRGBPicker"))
                {
                    if (popup.Success)
                    {
                        if (customize.Race != Races.Hrothgar)
                        {
                            if (ImGui.ColorPicker3("Hair Highlights Color##rgbWheel", ref shader.HairHighlight))
                            {
                                CurrentActor.UpdateShaderLocks(shaderLockType.HighlightColor, true);
                                CurrentActor.ApplyShaderparams(shader);
                            }
                        }
                        else
                        {
                            if (ImGui.ColorPicker3("Pattern Color##rgbWheel", ref shader.HairHighlight))
                            {
                                CurrentActor.UpdateShaderLocks(shaderLockType.HighlightColor, true);
                                CurrentActor.ApplyShaderparams(shader);
                            }
                        }
                    }
                }

                ImGui.SameLine();

                ImGui.Text("Hair Colors");

                using (var popup = ImRaii.Popup("hairGlossRGBPicker"))
                {
                    if (popup.Success)
                    {
                        if (ImGui.ColorPicker3("Hair Gloss Color##rgbWheel", ref shader.HairGloss))
                        {
                            CurrentActor.UpdateShaderLocks(shaderLockType.HairColor, true);
                            CurrentActor.ApplyShaderparams(shader);
                        }
                    }
                }

                unsafe
                {
                    var currentIcon = eyeShaderlock ? GameResourceManager.Instance.GetResourceImage("Linked.png").ImGuiHandle : GameResourceManager.Instance.GetResourceImage("Unlinked.png").ImGuiHandle;

                    if (BearGUI.ImageButton($"###raweyecolorLock", currentIcon, new(22, 22)))
                    {
                        eyeShaderlock = !eyeShaderlock;

                        if (eyeShaderlock)
                        {
                            shader.RightEyeColor = shader.LeftEyeColor;

                            CurrentActor.UpdateShaderLocks(shaderLockType.REyeColor, true);
                        }

                        CurrentActor.ApplyShaderparams(shader);
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Link Eye Colors");
                }

                ImGui.SameLine();

                if (ImGui.ColorButton("Left Eye Color##rawlEyeColorButton", new Vector4(shader.LeftEyeColor, 1)))
                {
                    ImGui.OpenPopup("lEyeRGBPicker");
                }

                using (var popup = ImRaii.Popup("lEyeRGBPicker"))
                {
                    if (popup.Success)
                    {
                        if (ImGui.ColorPicker3("Left Eye Color##rgbWheel", ref shader.LeftEyeColor))
                        {
                            if (eyeShaderlock)
                            {
                                shader.RightEyeColor = shader.LeftEyeColor;

                                CurrentActor.UpdateShaderLocks(shaderLockType.REyeColor, true);
                            }

                            CurrentActor.UpdateShaderLocks(shaderLockType.LEyeColor, true);

                            CurrentActor.ApplyShaderparams(shader);
                        }
                    }
                }

                ImGui.SameLine();

                if (ImGui.ColorButton("Right Eye Color##rawrEyeColorButton", new Vector4(shader.RightEyeColor, 1)))
                {
                    if (eyelock) ImGui.OpenPopup("lEyeRGBPicker");
                    else ImGui.OpenPopup("rightEyeRGBPicker");

                }

                using (var popup = ImRaii.Popup("rightEyeRGBPicker"))
                {
                    if (popup.Success)
                    {
                        if (ImGui.ColorPicker3("Right Eye Color##rgbWheel", ref shader.RightEyeColor))
                        {
                            CurrentActor.UpdateShaderLocks(shaderLockType.REyeColor, true);
                            CurrentActor.ApplyShaderparams(shader);
                        }
                    }
                }

                ImGui.SameLine();

                ImGui.Text("Eye Colors");

                

                if (ImGui.ColorButton("Feature Color##rawfeatureColorButton", new Vector4(shader.FeatureColor, 1)))
                {
                    ImGui.OpenPopup("featureRGBPicker");
                }

                using (var popup = ImRaii.Popup("featureRGBPicker"))
                {
                    if (popup.Success)
                    {
                        if (ImGui.ColorPicker3("Feature Color##rgbWheel", ref shader.FeatureColor))
                        {
                            CurrentActor.UpdateShaderLocks(shaderLockType.FeatureColor, true);
                            CurrentActor.ApplyShaderparams(shader);
                        }
                    }
                }

                ImGui.SameLine();

                ImGui.Text("Feature Colors");

                if (ImGui.ColorButton("Tint Color##tintColorButton", tint))
                {
                    ImGui.OpenPopup("tintRGBPicker");
                }

                using (var popup = ImRaii.Popup("tintRGBPicker"))
                {
                    if (popup.Success)
                    {
                        if (ImGui.ColorPicker4("Tint Color##rgbWheel", ref tint))
                        {
                            CurrentActor.SetTint(tint);
                        }
                    }
                }

                ImGui.SameLine();

                if (ImGui.ColorButton("Main Hand Tint Color##tintColorButton", mhTint))
                {
                    ImGui.OpenPopup("MHtintRGBPicker");
                }

                using (var popup = ImRaii.Popup("MHtintRGBPicker"))
                {
                    if (popup.Success)
                    {
                        if (ImGui.ColorPicker4("Tint Color##rgbWheel", ref mhTint))
                        {
                            CurrentActor.SetWeaponTint(WeaponSlot.MainHand, mhTint);
                        }
                    }
                }

                ImGui.SameLine();

                if (ImGui.ColorButton("Off Hand Tint Color##tintColorButton", ohTint))
                {
                    ImGui.OpenPopup("OHtintRGBPicker");
                }

                using (var popup = ImRaii.Popup("OHtintRGBPicker"))
                {
                    if (popup.Success)
                    {
                        if (ImGui.ColorPicker4("Tint Color##rgbWheel", ref ohTint))
                        {
                            CurrentActor.SetWeaponTint(WeaponSlot.OffHand, ohTint);
                        }
                    }
                }

                ImGui.SameLine();

                ImGui.Text("Tint Colors");

                ImGui.EndGroup();
            }
        }
        
        private static void DrawCharaUi()
        {
            ImGui.Separator();

            ImGui.SetCursorPosX(150);

            ImGui.BeginGroup();

            if (BearGUI.ImageButton($"Load NPC Appearance", GameResourceManager.Instance.GetResourceImage("CharaNPC.png").ImGuiHandle, new(26, 22)))
            {
                ImGui.OpenPopup("NPC Picker Popup");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Load NPC Appearance");
            }

            using (var popup = ImRaii.Popup("NPC Picker Popup"))
            {
                if (popup.Success)
                {
                    ImGui.Text("Search:");
                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(150);
                    ImGui.InputText("##NPCSearch", ref npcSearch, 1000);

                    if (ImGui.BeginTabBar("NPCData"))
                    {
                        if (ImGui.BeginTabItem("Battle NPC##npctabitem"))
                        {
                            using (var listbox = ImRaii.ListBox("###NPClistbox", new(271, 300)))
                            {
                                if (listbox.Success)
                                {
                                    var i = 0;

                                    foreach (var item in GameResourceManager.Instance.BNpcBases.Values)
                                    {
                                        if (item.ModelChara.Value.Type == 0 || item.ModelChara.Value.Type == 5) continue;

                                        if (item.ModelChara.Value.Type == 4 && !IllusioVitae.InDebug()) continue;

                                        List<uint> namesIDX = [];


                                        if (item.RowId < GameResourceManager.Instance.BNPCNameIndicies.Count)
                                        {
                                           namesIDX = GameResourceManager.Instance.BNPCNameIndicies[(int)item.RowId].ToList();
                                        }

                                        if (namesIDX.Count == 0) namesIDX.Add(0);

                                        foreach (var idx in namesIDX)
                                        {
                                            var name = GameResourceManager.Instance.BNpcNames[idx].Singular.RawString;

                                            if (name.Length == 0) name = $"Battle NPC {item.RowId:D7}";

                                            if (!name.Contains(npcSearch, StringComparison.OrdinalIgnoreCase)) continue;

                                            i++;

                                            using (ImRaii.PushId(i))
                                            {
                                                var startPos = ImGui.GetCursorPos();

                                                var selected = ImGui.Selectable("###Selector", false, ImGuiSelectableFlags.None, new(0, 50));

                                                var endPos = ImGui.GetCursorPos();

                                                if (ImGui.IsItemVisible())
                                                {
                                                    var icon = GameResourceManager.Instance.GetResourceImage("BNPC.png");

                                                    if (icon == null) continue;

                                                    ImGui.SetCursorPos(new(startPos.X, startPos.Y + 5));

                                                    ImGui.Image(icon.ImGuiHandle, new(40, 40));
                                                    ImGui.SameLine();

                                                    ImGui.BeginGroup();

                                                    ImGui.Text($"{name.Captialize()}");

                                                    ImGui.Text($"Model ID: {item.ModelChara.Row}");

                                                    ImGui.EndGroup();

                                                    ImGui.SetCursorPos(endPos);

                                                    if (selected)
                                                    {
                                                        CurrentActor.SetModelType(item.ModelChara.Row, item.ModelChara.Row == 0 ? false : true);
                                                        SetBNPCEquipment(item.NpcEquip.Value);
                                                        CurrentActor.UpdateShaderLocks(shaderLockType.None, true);
                                                        CurrentActor.ApplyCustomize(item.BNpcCustomize.Value.ToCharacterStruct());
                                                        UpdateData(false, updateShader: false);
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

                        if (ImGui.BeginTabItem("Event NPC##npctabitem"))
                        {
                            using (var listbox = ImRaii.ListBox("###NPClistbox", new(271, 300)))
                            {
                                if (listbox.Success)
                                {
                                    var i = 0;

                                    foreach (var item in GameResourceManager.Instance.ENPCNamed.Values)
                                    {
                                        var name = item.resident.Singular.RawString;

                                        if (name.Length == 0) name = $"Event NPC {item.Base.RowId:D7}";

                                        if (!name.Contains(npcSearch, StringComparison.OrdinalIgnoreCase)) continue;

                                        i++;

                                        using (ImRaii.PushId(i))
                                        {
                                            var startPos = ImGui.GetCursorPos();

                                            var selected = ImGui.Selectable("###Selector", false, ImGuiSelectableFlags.None, new(0, 50));

                                            var endPos = ImGui.GetCursorPos();

                                            if (ImGui.IsItemVisible())
                                            {
                                                var icon = GameResourceManager.Instance.GetResourceImage("ENPC.png");

                                                if (icon == null) continue;

                                                ImGui.SetCursorPos(new(startPos.X, startPos.Y + 5));

                                                ImGui.Image(icon.ImGuiHandle, new(40, 40));
                                                ImGui.SameLine();

                                                ImGui.BeginGroup();

                                                ImGui.Text(name.Captialize());

                                                ImGui.Text($"ModelType: {item.Base.ModelChara.Row}");

                                                ImGui.EndGroup();

                                                ImGui.SetCursorPos(endPos);

                                                if (selected)
                                                {
                                                    CustomizeStruct data = new();

                                                    CurrentActor.SetModelType(item.Base.ModelChara.Row, item.Base.ModelChara.Row == 0 ? false : true);
                                                    SetENPCEquipment(item.Base);
                                                    CurrentActor.UpdateShaderLocks(shaderLockType.None, true);
                                                    CurrentActor.ApplyCustomize(item.Base.ToCharacterStruct());
                                                    UpdateData(false, updateShader:false);
                                                    ImGui.CloseCurrentPopup();
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            ImGui.EndTabItem();
                        }

                        if (ImGui.BeginTabItem("Mounts##npctabitem"))
                        {
                            using (var listbox = ImRaii.ListBox("###NPClistbox", new(271, 300)))
                            {
                                if (listbox.Success)
                                {
                                    var i = 0;

                                    foreach (var item in GameResourceManager.Instance.Mounts.Values)
                                    {
                                        var name = $"Mount {item.RowId:D7}";

                                        if (item.Singular != null && item.Singular != "") name = item.Singular.RawString;

                                        if (!name.Contains(npcSearch, StringComparison.OrdinalIgnoreCase)) continue;

                                        i++;

                                        using (ImRaii.PushId(i))
                                        {
                                            var startPos = ImGui.GetCursorPos();

                                            var selected = ImGui.Selectable("###Selector", false, ImGuiSelectableFlags.None, new(0, 50));

                                            var endPos = ImGui.GetCursorPos();

                                            if (ImGui.IsItemVisible())
                                            {
                                                var icon = GameResourceManager.Instance.GetResourceImage("Mount.png");

                                                if (item.Icon != 0) icon = DalamudServices.textureProvider.GetFromGameIcon(new(item.Icon)).GetWrapOrDefault();

                                                if (icon == null) continue;

                                                ImGui.SetCursorPos(new(startPos.X, startPos.Y + 5));

                                                ImGui.Image(icon.ImGuiHandle, new(40, 40));
                                                ImGui.SameLine();

                                                ImGui.BeginGroup();

                                                ImGui.Text(name.Captialize());

                                                ImGui.Text($"ModelType: {item.ModelChara.Row}");

                                                ImGui.EndGroup();

                                                ImGui.SetCursorPos(endPos);

                                                if (selected)
                                                {
                                                    CustomizeStruct data = new();

                                                    SetMountEquipment(item);
                                                    CurrentActor.UpdateShaderLocks(shaderLockType.None, true);
                                                    CurrentActor.SetModelType(item.ModelChara.Row, true);
                                                    UpdateData(false, updateShader: false);
                                                    ImGui.CloseCurrentPopup();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            ImGui.EndTabItem();
                        }

                        if (ImGui.BeginTabItem("Minions##npctabitem"))
                        {
                            using (var listbox = ImRaii.ListBox("###NPClistbox", new(271, 300)))
                            {
                                if (listbox.Success)
                                {
                                    var i = 0;

                                    //ImGui.Text(ResourceHandler.Instance.Companions.Count.ToString());
                                    foreach (var item in GameResourceManager.Instance.Companions.Values)
                                    {
                                        var name = $"Minion {item.RowId:D7}";

                                        if (item.Singular != null && item.Singular != "") name = item.Singular.RawString;

                                        if (!name.Contains(npcSearch, StringComparison.OrdinalIgnoreCase)) continue;

                                        i++;

                                        using (ImRaii.PushId(i))
                                        {
                                            var startPos = ImGui.GetCursorPos();

                                            var selected = ImGui.Selectable("###Selector", false, ImGuiSelectableFlags.None, new(0, 50));

                                            var endPos = ImGui.GetCursorPos();

                                            if (ImGui.IsItemVisible())
                                            {
                                                var icon = GameResourceManager.Instance.GetResourceImage("Minion.png");

                                                if (item.Icon != 0) icon = DalamudServices.textureProvider.GetFromGameIcon(new(item.Icon)).GetWrapOrDefault();

                                                if (icon == null) continue;

                                                ImGui.SetCursorPos(new(startPos.X, startPos.Y + 5));

                                                ImGui.Image(icon.ImGuiHandle, new(40, 40));
                                                ImGui.SameLine();

                                                ImGui.BeginGroup();

                                                ImGui.Text(name.Captialize());

                                                ImGui.Text($"ModelType: {item.Model.Row}");

                                                ImGui.EndGroup();

                                                ImGui.SetCursorPos(endPos);

                                                if (selected)
                                                {
                                                    CustomizeStruct data = new();

                                                    CurrentActor.UpdateShaderLocks(shaderLockType.None, true);

                                                    CurrentActor.SetModelType(item.Model.Row, true);
                                                    UpdateData(false, updateShader: false);
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

            ImGui.SameLine();

            if (BearGUI.ImageButton($"Load Local Character", GameResourceManager.Instance.GetResourceImage("CharaLocal.png").ImGuiHandle, new(26, 22)))
            {
                CharaFiles.Clear();

                var documnets = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                var charaDataFolder = Path.Combine(documnets, "My Games", "FINAL FANTASY XIV - A Realm Reborn");

                var files = Directory.GetFiles(charaDataFolder, "FFXIV*.dat");

                foreach (var file in files)
                {
                    FFXIVChara chara = new(File.ReadAllBytes(file));

                    CharaFiles.Add(chara);
                }

                ImGui.OpenPopup("FFXIVCharaFilesPopup");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Load Local Characters");
            }
            

            using (var popup = ImRaii.Popup("FFXIVCharaFilesPopup"))
            {
                if (popup.Success)
                {
                    var i = 0;
                    foreach (var file in CharaFiles)
                    {
                            
                        if (ImGui.Button($"{i.ToString("00")} - {file.comment}", new(ImGui.CalcTextSize("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX").X, 25)))
                        {
                            customize = file.customize;
                            CurrentActor.SetVoice(file.voice);

                            UpdateData(true, true);

                            ImGui.CloseCurrentPopup();
                        }

                        i++;
                    }
                }
            }

            ImGui.SameLine();

            if (BearGUI.ImageButton($"Save Character", GameResourceManager.Instance.GetResourceImage("CharaSave.png").ImGuiHandle, new(26, 22)))
            {
                CharaFile charaFile = new CharaFile();

                charaFile.WriteToFile(CurrentActor, CharaFile.SaveModes.All);

                var x = JsonHandler.Serialize(charaFile);

                WindowsManager.Instance.fileDialogManager.SaveFileDialog("Save a Character Data File", ".chara", "data.chara", ".chara", (Confirm, Path) =>
                {
                    if (!Confirm) return;
                    File.WriteAllText(Path, x);
                });
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Save Character Data");
            }

            ImGui.SameLine();

            if (BearGUI.ImageButton($"Load Character", GameResourceManager.Instance.GetResourceImage("CharaLoad.png").ImGuiHandle, new(26, 22)))
            {
                WindowsManager.Instance.fileDialogManager.OpenFileDialog("Load a Character Data File", ".chara", (Confirm, Path) =>
                {
                    if (!Confirm) return;

                    var charaFile = JsonHandler.Deserialize<CharaFile>(File.ReadAllText(Path));

                    charaFile.Apply(CurrentActor, SaveModes.All);

                    UpdateData(false);
                });
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Load Character Data");
            }

            ImGui.SameLine();

            if (BearGUI.ImageButton($"Refresh Character", GameResourceManager.Instance.GetResourceImage("CharaRefresh.png").ImGuiHandle, new(26, 22)))
            {
                CurrentActor.ResetApperance();
                UpdateData(false);
            }


            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Reset & Reload All Character Data");
            }
        }

        private static void SetBNPCEquipment(NpcEquip equip)
        {
            if (EventManager.validCheck)
            {
                CurrentActor.SetWeaponSlot(WeaponSlot.MainHand, new WeaponModelId() { Value = equip.ModelMainHand, Stain0 = (byte)equip.DyeMainHand.Row, Stain1 = (byte)equip.Dye2MainHand.Row });
                CurrentActor.SetWeaponSlot(WeaponSlot.OffHand, new WeaponModelId() { Value = equip.ModelOffHand, Stain0 = (byte)equip.DyeOffHand.Row, Stain1 = (byte)equip.Dye2OffHand.Row });
            }

            CurrentActor.SetEquipmentSlot(EquipmentSlot.Head, new() { Id = (ushort)equip.ModelHead, Stain0 = (byte)equip.DyeHead.Row, Stain1 = (byte)equip.Dye2Head.Row });
            CurrentActor.SetEquipmentSlot(EquipmentSlot.Body, new() { Id = (ushort)equip.ModelBody, Stain0 = (byte)equip.DyeBody.Row, Stain1 = (byte)equip.Dye2Body.Row });
            CurrentActor.SetEquipmentSlot(EquipmentSlot.Hands, new() { Id = (ushort)equip.ModelHands, Stain0 = (byte)equip.DyeHands.Row, Stain1 = (byte)equip.Dye2Hands.Row });
            CurrentActor.SetEquipmentSlot(EquipmentSlot.Legs, new() { Id = (ushort)equip.ModelLegs, Stain0 = (byte)equip.DyeLegs.Row, Stain1 = (byte)equip.Dye2Legs.Row });
            CurrentActor.SetEquipmentSlot(EquipmentSlot.Feet, new() { Id = (ushort)equip.ModelFeet, Stain0 = (byte)equip.DyeFeet.Row, Stain1 = (byte)equip.Dye2Feet.Row });
            CurrentActor.SetEquipmentSlot(EquipmentSlot.Neck, new() { Id = (ushort)equip.ModelNeck, Stain0 = (byte)equip.DyeNeck.Row, Stain1 = (byte)equip.Dye2Neck.Row });
            CurrentActor.SetEquipmentSlot(EquipmentSlot.Ears, new() { Id = (ushort)equip.ModelEars, Stain0 = (byte)equip.DyeEars.Row, Stain1 = (byte)equip.Dye2Ears.Row });
            CurrentActor.SetEquipmentSlot(EquipmentSlot.Wrists, new() { Id = (ushort)equip.ModelWrists, Stain0 = (byte)equip.DyeWrists.Row, Stain1 = (byte)equip.Dye2Wrists.Row });
            CurrentActor.SetEquipmentSlot(EquipmentSlot.LFinger, new() { Id = (ushort)equip.ModelLeftRing, Stain0 = (byte)equip.DyeLeftRing.Row, Stain1 = (byte)equip.Dye2LeftRing.Row });
            CurrentActor.SetEquipmentSlot(EquipmentSlot.RFinger, new() { Id = (ushort)equip.ModelRightRing, Stain0 = (byte)equip.DyeRightRing.Row, Stain1 = (byte)equip.Dye2RightRing.Row });

            CurrentActor.SetFacewear(0, equip.Unknown37);
            CurrentActor.SetFacewear(1, equip.Unknown38);
        }

        private static void SetMountEquipment(Mount equip)
        {
            if (EventManager.validCheck)
            {
                CurrentActor.SetWeaponSlot(WeaponSlot.MainHand, new WeaponModelId() { Value = 0 });
                CurrentActor.SetWeaponSlot(WeaponSlot.OffHand, new WeaponModelId() { Value = 0 });
            }

            CurrentActor.SetEquipmentSlot(EquipmentSlot.Head, new() { Id = (ushort)equip.EquipHead});
            CurrentActor.SetEquipmentSlot(EquipmentSlot.Body, new() { Id = (ushort)equip.EquipBody});
            CurrentActor.SetEquipmentSlot(EquipmentSlot.Hands, new() { Id = (ushort)0 });
            CurrentActor.SetEquipmentSlot(EquipmentSlot.Legs, new() { Id = (ushort)equip.EquipLeg });
            CurrentActor.SetEquipmentSlot(EquipmentSlot.Feet, new() { Id = (ushort)equip.EquipFoot });
        }

        private static void SetENPCEquipment(ENpcBase equip)
        {
            var outfit2 = equip.NpcEquip.Value;

            if (EventManager.validCheck)
            {
                if (equip.ModelMainHand != 0)
                    CurrentActor.SetWeaponSlot(WeaponSlot.MainHand, new WeaponModelId() { Value = equip.ModelMainHand, Stain0 = (byte)equip.DyeMainHand.Row, Stain1 = (byte)equip.Dye2MainHand.Row });
                else
                    CurrentActor.SetWeaponSlot(WeaponSlot.MainHand, new WeaponModelId() { Value = outfit2.ModelMainHand, Stain0 = (byte)outfit2.DyeMainHand.Row, Stain1 = (byte)equip.Dye2MainHand.Row });

                if (equip.ModelOffHand != 0)
                    CurrentActor.SetWeaponSlot(WeaponSlot.OffHand, new WeaponModelId() { Value = equip.ModelOffHand, Stain0 = (byte)equip.DyeOffHand.Row, Stain1 = (byte)equip.Dye2OffHand.Row });
                else
                    CurrentActor.SetWeaponSlot(WeaponSlot.OffHand, new WeaponModelId() { Value = outfit2.ModelOffHand, Stain0 = (byte)outfit2.DyeOffHand.Row, Stain1 = (byte)equip.Dye2OffHand.Row });
            }

            if(equip.ModelHead != 0)
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Head, new() { Value = equip.ModelHead, Stain0 = (byte)equip.DyeHead.Row, Stain1 = (byte)equip.Dye2Head.Row });
            else
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Head, new() { Value = outfit2.ModelHead, Stain0 = (byte)outfit2.DyeHead.Row, Stain1 = (byte)equip.Dye2Head.Row });

            if(equip.ModelBody != 0)
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Body, new() { Value = equip.ModelBody, Stain0 = (byte)equip.DyeBody.Row, Stain1 = (byte)equip.Dye2Body.Row });
            else
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Body, new() { Value = outfit2.ModelBody, Stain0 = (byte)outfit2.DyeBody.Row, Stain1 = (byte)equip.Dye2Body.Row });

            if(equip.ModelHands != 0)
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Hands, new() { Value = equip.ModelHands, Stain0 = (byte)equip.DyeHands.Row, Stain1 = (byte)equip.Dye2Hands.Row });
            else
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Hands, new() { Value = outfit2.ModelHands, Stain0 = (byte)outfit2.DyeHands.Row, Stain1 = (byte)equip.Dye2Hands.Row });

            if(equip.ModelLegs != 0)
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Legs, new() { Value = equip.ModelLegs, Stain0 = (byte)equip.DyeLegs.Row, Stain1 = (byte)equip.Dye2Legs.Row });
            else
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Legs, new() { Value = outfit2.ModelLegs, Stain0 = (byte)outfit2.DyeLegs.Row, Stain1 = (byte)equip.Dye2Legs.Row });

            if(equip.ModelFeet != 0)
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Feet, new() { Value = equip.ModelFeet, Stain0 = (byte)equip.DyeFeet.Row, Stain1 = (byte)equip.Dye2Feet.Row });
            else
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Feet, new() { Value = outfit2.ModelFeet, Stain0 = (byte)outfit2.DyeFeet.Row, Stain1 = (byte)equip.Dye2Feet.Row });

            if(equip.ModelEars != 0)
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Ears, new() { Value = equip.ModelEars, Stain0 = (byte)equip.DyeEars.Row, Stain1 = (byte)equip.Dye2Ears.Row });
            else
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Ears, new() { Value = outfit2.ModelEars, Stain0 = (byte)outfit2.DyeEars.Row, Stain1 = (byte)equip.Dye2Ears.Row });

            if(equip.ModelNeck != 0)
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Neck, new() { Value = equip.ModelNeck, Stain0 = (byte)equip.DyeNeck.Row, Stain1 = (byte)equip.Dye2Neck.Row });
            else
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Neck, new() { Value = outfit2.ModelNeck, Stain0 = (byte)outfit2.DyeNeck.Row, Stain1 = (byte)equip.Dye2Neck.Row });

            if(equip.ModelWrists != 0)
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Wrists, new() { Id = (ushort)equip.ModelWrists, Stain0 = (byte)equip.DyeWrists.Row, Stain1 = (byte)equip.Dye2Wrists.Row });
            else
                CurrentActor.SetEquipmentSlot(EquipmentSlot.Wrists, new() { Id = (ushort)outfit2.ModelWrists, Stain0 = (byte)outfit2.DyeWrists.Row, Stain1 = (byte)equip.Dye2Wrists.Row });

            if(equip.ModelLeftRing != 0)
                CurrentActor.SetEquipmentSlot(EquipmentSlot.LFinger, new() { Id = (ushort)equip.ModelLeftRing, Stain0 = (byte)equip.DyeLeftRing.Row, Stain1 = (byte)equip.Dye2LeftRing.Row });
            else
                CurrentActor.SetEquipmentSlot(EquipmentSlot.LFinger, new() { Id = (ushort)outfit2.ModelLeftRing, Stain0 = (byte)outfit2.DyeLeftRing.Row, Stain1 = (byte)equip.Dye2LeftRing.Row });

            if(equip.ModelRightRing != 0)
                CurrentActor.SetEquipmentSlot(EquipmentSlot.RFinger, new() { Id = (ushort)equip.ModelRightRing, Stain0 = (byte)equip.DyeRightRing.Row, Stain1 = (byte)equip.Dye2RightRing.Row });
            else
                CurrentActor.SetEquipmentSlot(EquipmentSlot.RFinger, new() { Id = (ushort)outfit2.ModelRightRing, Stain0 = (byte)outfit2.DyeRightRing.Row, Stain1 = (byte)equip.Dye2RightRing.Row });
        
            if(equip.Unknown102 != 0)
            {
                CurrentActor.SetFacewear(0, equip.Unknown102);
            }
            else
            {
                CurrentActor.SetFacewear(0, outfit2.Unknown37);
            }

            if (equip.Unknown103 != 0)
            {
                CurrentActor.SetFacewear(1, equip.Unknown103);
            }
            else
            {
                CurrentActor.SetFacewear(1, outfit2.Unknown38);
            }
        }

        private static void DrawFeatureButton(ValidFeature feature)
        {
            bool enabled = false;
            FacialFeatures featureType = FacialFeatures.None;

            switch (feature.featureID)
            {
                case 0: enabled = customize.FaceFeatures.HasFlag(FacialFeatures.First); featureType = FacialFeatures.First; break;
                case 1: enabled = customize.FaceFeatures.HasFlag(FacialFeatures.Second); featureType = FacialFeatures.Second; break;
                case 2: enabled = customize.FaceFeatures.HasFlag(FacialFeatures.Third); featureType = FacialFeatures.Third;break;
                case 3: enabled = customize.FaceFeatures.HasFlag(FacialFeatures.Fourth); featureType = FacialFeatures.Fourth;break;
                case 4: enabled = customize.FaceFeatures.HasFlag(FacialFeatures.Fifth); featureType = FacialFeatures.Fifth;break;
                case 5: enabled = customize.FaceFeatures.HasFlag(FacialFeatures.Sixth); featureType = FacialFeatures.Sixth;break;
                case 6: enabled = customize.FaceFeatures.HasFlag(FacialFeatures.Seventh); featureType = FacialFeatures.Seventh; break;
                case 7: enabled = customize.FaceFeatures.HasFlag(FacialFeatures.LegacyTattoo); featureType = FacialFeatures.LegacyTattoo; break;
            }

            var icon = GameResourceManager.Instance.GetResourceImage("LegacyTattoo.png").ImGuiHandle;

            if (feature.featureID != 7) 
            {
                var wrapper = DalamudServices.textureProvider.GetFromGameIcon(new(feature.icon)).GetWrapOrDefault();

                if (wrapper != null) icon = wrapper.ImGuiHandle;
            }
                

            if(feature.icon == 0 && feature.featureID != 7)
                icon = GameResourceManager.Instance.GetResourceImage("UnavailableSlot.png").ImGuiHandle;

            var currentPos = ImGui.GetCursorPos();

            if (BearGUI.ImageButton($"{featureType}", icon, new Vector2(64)))
            {
                if (!enabled)
                {
                    customize.FaceFeatures |= featureType;
                }
                else
                {
                    customize.FaceFeatures &= ~featureType;
                }

                UpdateData(true);
            }

            if (enabled)
            {
                var x = ImGui.GetCursorPosX();
                ImGui.SameLine();
                ImGui.SetCursorPosX(currentPos.X);

                ImGui.Image(GameResourceManager.Instance.GetResourceImage("SelectSlot.png").ImGuiHandle, new Vector2(64));
            }
        }
        
        private static void DrawColorPopup(string idx, uint[] colors, ref byte value, shaderLockType type)
        {
            using (var popup = Popup($"{idx}"))
            {
                if (popup.Success)
                {
                    ImGui.BeginChild($"##{idx}PopupChild", new Vector2((ImGui.CalcTextSize("XXX").X * 1.3f) * 10, (ImGui.GetTextLineHeight() * 1.5f) * 10));
                    for (var i = 0; i < colors.Length; i++)
                    {
                        if (i % 8 != 0)
                        {
                            ImGui.SameLine();
                        }

                        if (colors[i] == 0) continue;

                        if (BearGUI.ColoredLableButton($"{idx}Select{i}", colors[i], i.ToString()))
                        {
                            CurrentActor.UpdateShaderLocks(type, false);

                            value = (byte)i;

                            if (eyelock) 
                            {
                                customize.REyeColor = customize.LEyeColor;
                            } 

                            UpdateData(true);
                            ImGui.CloseCurrentPopup();
                        }
                    }
                    ImGui.EndChild();
                }
            }
        }
    
        private static void DrawIconsPopup(string idx, List<CharaMakeCustomize> data, ref byte value, bool useMask = false, bool useOLDOverlay = false)
        {
            using (var popup = Popup(idx))
            {
                if (popup.Success)
                {
                    ImGui.BeginChild($"##{idx}PopupChild", new Vector2(((ImGui.GetTextLineHeight() + 4.5f) * 4f) * 6, (ImGui.GetTextLineHeight() * 4f) * 4.5f));
                    for (var i = 0; i < data.Count; i++)
                    {
                        uint icon = data[i].Icon;

                        if (icon == 0 &&  i != 0) continue;

                        if (i % 6 != 0)
                        {
                            ImGui.SameLine();
                        }

                        var wrapper = DalamudServices.textureProvider.GetFromGameIcon(new(icon)).GetWrapOrDefault();

                        if (wrapper == null) continue;

                        var handle = wrapper.ImGuiHandle;

                        if (i == 0 && icon == 0)
                        {
                            handle = GameResourceManager.Instance.GetResourceImage("EmptySlot.png").ImGuiHandle;
                        }

                            var currentPos = ImGui.GetCursorPosX();

                        if (BearGUI.ImageButton($"{i}", handle,new Vector2(64)))
                        {
                            byte selectedValue = data[i].FeatureID;

                            selectedValue += useMask ? (byte)128 : (byte)0;

                            value = selectedValue;

                            UpdateData(true);

                        }

                        if (customize.Race == Races.Hrothgar && useOLDOverlay && data[i].FeatureID < 9)
                        {
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(currentPos);
                            ImGui.Image(GameResourceManager.Instance.GetResourceImage("OldSlot.png").ImGuiHandle, new Vector2(64));
                        }

                        int realValue = value - (useMask ? 128 : 0);

                        if (realValue == data[i].FeatureID)
                        {
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(currentPos);

                            ImGui.Image(GameResourceManager.Instance.GetResourceImage("SelectSlot.png").ImGuiHandle, new Vector2(64));
                        }


                    }
                    ImGui.EndChild();
                }
            }
        }

        private static void DrawIconsPopup(string idx, List<GenericItem> data, ref byte value, bool useOLDOverlay = false)
        {
            using (var popup = Popup(idx))
            {
                if (popup.Success)
                {
                    ImGui.BeginChild($"##{idx}PopupChild", new Vector2(((ImGui.GetTextLineHeight() + 4.5f) * 4f) * 6, (ImGui.GetTextLineHeight() * 4f) * 4.5f));
                    for (var i = 0; i < data.Count; i++)
                    {
                        uint icon = data[i].icon;

                        if (icon == 0) continue;

                        if (i % 6 != 0)
                        {
                            ImGui.SameLine();
                        }

                        var image = DalamudServices.textureProvider.GetFromGameIcon(new(icon)).GetWrapOrDefault();

                        if (image == null) continue;

                        var currentPos = ImGui.GetCursorPosX();

                        if (BearGUI.ImageButton($"{i}", image.ImGuiHandle, new Vector2(64)))
                        {
                            if (customize.Race == Races.Hrothgar && customize.Gender == Genders.Feminine)
                            {
                                value = (byte)(data[i].id+4);
                                UpdateData(true);
                            }
                            else
                            {
                                value = data[i].id;
                                UpdateData(true);
                            }
                        }

                        if(customize.Race == Races.Hrothgar && customize.Gender == Genders.Masculine && useOLDOverlay && data[i].id < 5)
                        {
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(currentPos);
                            ImGui.Image(GameResourceManager.Instance.GetResourceImage("OldSlot.png").ImGuiHandle, new Vector2(64));
                        }

                        var currentFace = data[i].id;

                        if (customize.Race == Races.Hrothgar && customize.Gender == Genders.Feminine) currentFace+=4;

                        if (value == currentFace)
                        {
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(currentPos);

                            ImGui.Image(GameResourceManager.Instance.GetResourceImage("SelectSlot.png").ImGuiHandle, new Vector2(64));
                        }
                    }
                    ImGui.EndChild();
                }
            }
        }

        private static void DrawGearPopup(string idx, ActorEquipSlot slot, byte variant, byte dye, byte dye2, bool weaponSlot = false)
        {
            using (var popup = Popup(idx))
            {
                if (popup.Success)
                {
                    ImGui.Text("Search:");

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(150);
                    ImGui.InputText("##modelSearch", ref search, 500);

                    List<EquipmentData.ModelInfo> availableGear = GameResourceManager.Instance.EquipmentData.GetAllGear().Select(x => x).Where(x => x.Slots.HasFlag(slot) && x.Name.Contains(search, StringComparison.InvariantCultureIgnoreCase)).OrderBy(x => x.Name).ToList();

                    ImGui.BeginChild($"{idx}ChildMenu", new(400, ImGui.GetTextLineHeight() * 50));
                    for(var i = 0; i < availableGear.Count; i++)
                    {
                        var currentGear = availableGear[i];

                        if (currentGear.ModelId == 9019) continue;

                        if ((!DalamudServices.clientState.IsGPosing && weaponSlot) && !CurrentActor.isCustom)
                        {
                            if (!ExtentionMethods.ValidateJob(currentGear.classJob, CurrentActor.GetClass())) continue;
                        }
                        
                        using (ImRaii.PushId(i))
                        {
                            var startPos = ImGui.GetCursorPos();

                            var selected = ImGui.Selectable("###Selector", false, ImGuiSelectableFlags.None, new(0, 50));

                            var endPos = ImGui.GetCursorPos();

                            if (ImGui.IsItemVisible())
                            {
                                ImGui.SameLine();

                                ImGui.BeginGroup();

                                var icon = currentGear.Icon;

                                nint image = 0;

                                var wrapper = DalamudServices.textureProvider.GetFromGameIcon(new(icon)).GetWrapOrDefault();

                                if (wrapper != null) image = wrapper.ImGuiHandle;

                                ImGui.Image(image, new(50,50));

                                ImGui.SameLine();
                                
                                ImGui.Text(currentGear.Name == "" ? "Unknown" : currentGear.Name);

                                ImGui.EndGroup();

                                if (selected)
                                {
                                    if (weaponSlot)
                                    {
                                        WeaponModelId weaponModelId = new()
                                        {
                                            Value = currentGear.ModelId
                                        };

                                        weaponModelId.Value = currentGear.ModelId;
                                        weaponModelId.Stain0 = dye;
                                        weaponModelId.Stain1 = dye2;
                                        CurrentActor.SetWeaponSlot(slot, weaponModelId);

                                        if(slot == ActorEquipSlot.MainHand)
                                        {
                                            if(GameResourceManager.Instance.Items.TryGetValue(currentGear.ItemId, out var weaponCombo))
                                            {
                                                WeaponModelId weaponModelId2 = new()
                                                {
                                                    Value = weaponCombo.ModelSub
                                                };

                                                weaponModelId.Value = weaponCombo.ModelSub;
                                                weaponModelId.Stain0 = dye;
                                                weaponModelId.Stain1 = dye2;
                                                CurrentActor.SetWeaponSlot(ActorEquipSlot.OffHand, weaponModelId);
                                            }
                                        }

                                        UpdateData(true, !ExtentionMethods.ValidateJob(currentGear.classJob, CurrentActor.GetClass()));
                                    }
                                    else
                                    {
                                        EquipmentModelId equipmentModelId = new()
                                        {
                                            Value = (uint)currentGear.ModelId,
                                            Stain0 = dye,
                                            Stain1 = dye2
                                        };
                                        CurrentActor.SetEquipmentSlot(slot, equipmentModelId);
                                    }

                                    ImGui.CloseCurrentPopup();
                                }
                            }
                        }
                    }
                    ImGui.EndChild();
                }
            }
        }

        private static void DrawSelector(uint iconID, string IDX, string Title, List<GenericItem> items, ref int data, ref byte maindata)
        {
            var defaultImage = GameResourceManager.Instance.GetResourceImage("UnknownSlot.png").ImGuiHandle;

            if (iconID != 0)
            {
                var wrapper = DalamudServices.textureProvider.GetFromGameIcon(new(iconID)).GetWrapOrDefault();

                if (wrapper != null) defaultImage = wrapper.ImGuiHandle;
            }
            
            ImGui.BeginGroup();

            ImGui.Text(Title);

            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt($"##{IDX}_id", ref data, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsScientific))
            {
                maindata = (byte)data;
                UpdateData(true);
            }

            ImGui.SameLine();

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 20);

            var startPos = ImGui.GetCursorPos();

            if (BearGUI.ImageButton($"{IDX}{Title}", defaultImage, new Vector2(48, 46)))
            {
                ImGui.OpenPopup($"{IDX}Popup");
            }

            ImGui.EndGroup();

            DrawIconsPopup($"{IDX}Popup", items, ref maindata, Title == "Face Type");
        }

        private static void DrawFacePaintSelector(uint iconID, string IDX, string Title, List<CharaMakeCustomize> items, ref int data)
        {
            var defaultImage = GameResourceManager.Instance.GetResourceImage("EmptySlot.png").ImGuiHandle;

            if (iconID != 0)
            {
                var wrapper = DalamudServices.textureProvider.GetFromGameIcon(new(iconID)).GetWrapOrDefault();

                if (wrapper != null) defaultImage = wrapper.ImGuiHandle;
            }

            ImGui.BeginGroup();

            ImGui.Text(Title);

            var realData = data + (customize.FacepaintFlipped ? 128 : 0);

            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt($"##{IDX}_id", ref realData, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsScientific))
            {
                customize.RealFacepaint = (byte)data;
                UpdateData(true);
            }

            ImGui.EndGroup();

            ImGui.SameLine();

            var pos = ImGui.GetCursorPos();

            if (BearGUI.ImageButton($"##{IDX}{Title}", defaultImage,new Vector2(48, 46)))
            {
                ImGui.OpenPopup($"{IDX}Popup");
            }

            DrawIconsPopup($"{IDX}Popup", items, ref customize.Facepaint, customize.FacepaintFlipped);
        }

        private static void DrawSelector(uint iconID, string IDX, string Title, List<CharaMakeCustomize> items, ref int data, ref byte maindata)
        {
            var defaultImage = GameResourceManager.Instance.GetResourceImage("UnknownSlot.png").ImGuiHandle;

            if (iconID != 0)
            {
                var wrapper = DalamudServices.textureProvider.GetFromGameIcon(new(iconID)).GetWrapOrDefault();

                if (wrapper != null) defaultImage = wrapper.ImGuiHandle;
            }

            ImGui.BeginGroup();

            ImGui.Text(Title);

            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt($"##{IDX}_id", ref data, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsScientific))
            {
                maindata = (byte)data;
                UpdateData(true);
            }

            ImGui.SameLine();

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 20);

            var startPos = ImGui.GetCursorPos();

            if (BearGUI.ImageButton($"{IDX}{Title}", defaultImage,new Vector2(48, 46)))
            {
                ImGui.OpenPopup($"{IDX}Popup");
            }

            ImGui.EndGroup();

            DrawIconsPopup($"{IDX}Popup", items, ref maindata, false, IDX == "hairstyle");
        }
    
        private static void DrawGearSelector(string slotName, EquipmentModelId equip, ActorEquipSlot slot)
        {
            bool update = false;

            var model = GameResourceManager.Instance.EquipmentData.GetModelById(equip, slot);

            uint icon = 0;
            string modelname = "Unknown";

            if (model != null)
            {
                icon = model.Icon;
                modelname = model.Name;
            }
            else
            {
                for(var i = 0; i < 10; i++)
                {
                    var newModel = GameResourceManager.Instance.EquipmentData.GetModelById(new EquipmentModelId() { Id = equip.Id, Variant = (byte)i}, slot);

                    if (newModel != null)
                    {
                        icon += newModel.Icon;
                        modelname = newModel.Name;
                        break;
                    }
                }
            }
                
            int equipId = equip.Id;
            int equipVariant = equip.Variant;
            var Stain0 = GameResourceManager.Instance.Stains[equip.Stain0];
            var Stain1 = GameResourceManager.Instance.Stains[equip.Stain1];

            var defaultImage = GameResourceManager.Instance.GetResourceImage("Qicon.png").ImGuiHandle;

            ImGui.BeginGroup();

            float scaleOffset = .15f;

            // TODO Figure out a way to do this without causing elements to offset.
            /*
            for (var i = 0; i < modelname.Length; i++)
            {
                if (i > 25)
                {
                    scaleOffset += .01f;
                }
            }
            */

            BearGUI.Text(modelname, .85f);

            if (IllusioVitae.InDebug())
            {
                var data = BitConverter.GetBytes(equip.Value);

                string test = "";

                for (var i = 0; i < data.Length; i++)
                {
                    test += data[i].ToString("X");
                }

                ImGui.SetNextItemWidth(127);
                ImGui.InputText($"##{slot}", ref test, 100);

                ImGui.SameLine();

                //if (ImGui.Button($"Test##{slot}"))
                //{
                //    if (TryGetTextureForSlot((int)slot, out var textures))
                //    {
                //        ColorSetWindow.Show(textures);
                //    }
                //}
            }

            if (icon != 0)
            {
                var wrapper = DalamudServices.textureProvider.GetFromGameIcon(new(icon)).GetWrapOrDefault();

                if (wrapper != null) defaultImage = wrapper.ImGuiHandle;
            }
            else
            {
                switch (slot)
                {
                    case ActorEquipSlot.Head:
                        defaultImage = GameResourceManager.Instance.GetResourceImage("HeadSlot.png").ImGuiHandle;
                        break;
                    case ActorEquipSlot.Body:
                        defaultImage = GameResourceManager.Instance.GetResourceImage("BodySlot.png").ImGuiHandle;
                        break;
                    case ActorEquipSlot.Hands:
                        defaultImage = GameResourceManager.Instance.GetResourceImage("HandsSlot.png").ImGuiHandle;
                        break;
                    case ActorEquipSlot.Legs:
                        defaultImage = GameResourceManager.Instance.GetResourceImage("LegsSlot.png").ImGuiHandle;
                        break;
                    case ActorEquipSlot.Feet:
                        defaultImage = GameResourceManager.Instance.GetResourceImage("FeetSlot.png").ImGuiHandle;
                        break;
                    case ActorEquipSlot.Ears:
                        defaultImage = GameResourceManager.Instance.GetResourceImage("EarsSlot.png").ImGuiHandle;
                        break;
                    case ActorEquipSlot.Neck:
                        defaultImage = GameResourceManager.Instance.GetResourceImage("NeckSlot.png").ImGuiHandle;
                        break;
                    case ActorEquipSlot.Wrists:
                        defaultImage = GameResourceManager.Instance.GetResourceImage("WristsSlot.png").ImGuiHandle;
                        break;
                    case ActorEquipSlot.LeftRing:
                        defaultImage = GameResourceManager.Instance.GetResourceImage("RingSlot.png").ImGuiHandle;
                        break;
                    case ActorEquipSlot.RightRing:
                        defaultImage = GameResourceManager.Instance.GetResourceImage("RingSlot.png").ImGuiHandle;
                        break;
                }
            }

            if (BearGUI.ImageButton($"##{slot}gearButtonPrev", defaultImage, new Vector2(ImGui.GetTextLineHeight() * 3f)))
            {
                ImGui.OpenPopup($"{slot}ModelPopup");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(slotName);
            }

            DrawGearPopup($"{slot}ModelPopup", slot, (byte)equipVariant, (byte)Stain0.RowId, (byte)Stain1.RowId);

            ImGui.SameLine();

            ImGui.BeginGroup();

            ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXX").X);
            if (ImGui.DragInt($"##{slot}gearModelID", ref equipId, 1, 0, 9999))
            {
                CurrentActor.SetEquipmentSlot(slot, new() { Id = (ushort)equipId, Variant = (byte)equipVariant, Stain0 = (byte)Stain0.RowId, Stain1 = (byte)Stain1.RowId });
            }

            ImGui.SameLine();

            ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXX").X);
            if (ImGui.DragInt($"##{slot}gearVariantID", ref equipVariant, 1, 0, 999))
            {
                CurrentActor.SetEquipmentSlot(slot, new() { Id = (ushort)equipId, Variant = (byte)equipVariant, Stain0 = (byte)Stain0.RowId, Stain1 = (byte)Stain1.RowId });
            }

            if (slot == ActorEquipSlot.Head)
            {
                ImGui.SameLine();

                unsafe
                {
                    var currentIcon = CurrentActor.GetCharacter()->DrawData.IsHatHidden ? GameResourceManager.Instance.GetResourceImage("VisibilityOff.png").ImGuiHandle : GameResourceManager.Instance.GetResourceImage("VisibilityOn.png").ImGuiHandle;

                    if (BearGUI.ImageButton($"###headgearToggle", currentIcon, new(22, 22)))
                    {
                        CurrentActor.GetCharacter()->DrawData.HideHeadgear(0, !CurrentActor.GetCharacter()->DrawData.IsHatHidden);
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Toggle Visibility");
                }

                ImGui.SameLine();

                unsafe
                {
                    var currentIcon = CurrentActor.GetCharacter()->DrawData.IsVisorToggled ? GameResourceManager.Instance.GetResourceImage("VisorUp.png").ImGuiHandle : GameResourceManager.Instance.GetResourceImage("VisorDown.png").ImGuiHandle;

                    if (BearGUI.ImageButton($"###visorToggle", currentIcon, new(22, 22)))
                    {
                        CurrentActor.GetCharacter()->DrawData.SetVisor(!CurrentActor.GetCharacter()->DrawData.IsVisorToggled);
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Toggle Visor");
                }
            }

            string Stain0Name = Stain0.Name;

            if(Stain0.Name == string.Empty) Stain0Name = "None";
            var correctedStain0Color = GUIMethods.ARGBToABGR(Stain0.Color);

            if (BearGUI.ColoredLableButton($"##{slot}{Stain0Name}dyeSlotButton", correctedStain0Color, "Dye #1", false, new(71, ImGui.GetTextLineHeight() * 1.5f)))
            {
                if(equip.Id != 0)
                    ImGui.OpenPopup($"{slot}{Stain0Name}dyeButtonPopup");
            }

            ImGui.SameLine();

            string Stain1Name = Stain1.Name;

            if (Stain0.Name == string.Empty) Stain1Name = "None";
            var correctedStain1Color = GUIMethods.ARGBToABGR(Stain1.Color);

            if (BearGUI.ColoredLableButton($"##{slot}{Stain1Name}dyeSlot2Button", correctedStain1Color, "Dye #2", false, new(71, ImGui.GetTextLineHeight() * 1.5f)))
            {
                if (equip.Id != 0)
                    ImGui.OpenPopup($"{slot}{Stain1Name}dyeButtonPopup2");
            }
            ImGui.EndGroup();

            ImGui.EndGroup();
            using (var popup = Popup($"{slot}{Stain0Name}dyeButtonPopup"))
            {
                if (popup.Success)
                {
                    ImGui.Text("Search: ");

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(80);

                    ImGui.InputText($"##{slot}colorsearch", ref dyeSearch, 1000);

                    var availableStains = GameResourceManager.Instance.Stains.Select(x => x.Value).Where(x => x.Name.RawString.ToLower().Contains(dyeSearch.ToLower())).ToList();

                    ImGui.BeginChild($"##{slot}{Stain0Name}dyeButtonPopupChild", new Vector2(175, ImGui.GetTextLineHeight() * 20));
                    for (var i = 0; i < availableStains.Count; i++)
                    {

                        var currentStain0 = availableStains[i];

                        if (i != 0 && currentStain0.Color == 0) continue;

                        var selectedStainName = currentStain0.Name.RawString;

                        if (selectedStainName == "")
                        {
                            selectedStainName = "None";
                        }

                        if (BearGUI.ColoredLableButton($"##{i}ColoredDyeButton", GUIMethods.ARGBToABGR(currentStain0.Color), selectedStainName, false, new(138, ImGui.GetTextLineHeight() * 1.5f)))
                        {
                            CurrentActor.SetEquipmentSlot(slot, new() { Id = (ushort)equipId, Variant = (byte)equipVariant, Stain0 = (byte)currentStain0.RowId, Stain1 = (byte)Stain1.RowId });
                            ImGui.CloseCurrentPopup();
                        }
                    }
                    ImGui.EndChild();
                }
            }

            using (var popup = Popup($"{slot}{Stain1Name}dyeButtonPopup2"))
            {
                if (popup.Success)
                {
                    ImGui.Text("Search: ");

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(80);

                    ImGui.InputText($"##{slot}colorsearch", ref dyeSearch, 1000);

                    var availableStains = GameResourceManager.Instance.Stains.Select(x => x.Value).Where(x => x.Name.RawString.ToLower().Contains(dyeSearch.ToLower())).ToList();

                    ImGui.BeginChild($"##{slot}{Stain1Name}dyeButtonPopupChild", new Vector2(175, ImGui.GetTextLineHeight() * 20));
                    for (var i = 0; i < availableStains.Count; i++)
                    {

                        var currentStain1 = availableStains[i];

                        if (i != 0 && currentStain1.Color == 0) continue;

                        var selectedStainName = currentStain1.Name.RawString;

                        if (selectedStainName == "")
                        {
                            selectedStainName = "None";
                        }

                        if (BearGUI.ColoredLableButton($"##{i}ColoredDyeButton", GUIMethods.ARGBToABGR(currentStain1.Color), selectedStainName, false, new(138, ImGui.GetTextLineHeight() * 1.5f)))
                        {
                            CurrentActor.SetEquipmentSlot(slot, new() { Id = (ushort)equipId, Variant = (byte)equipVariant, Stain0 = (byte)Stain0.RowId, Stain1 = (byte)currentStain1.RowId });
                            ImGui.CloseCurrentPopup();
                        }
                    }
                    ImGui.EndChild();
                }
            }
        }

        private static void DrawGearSelector(string slotName, WeaponModelId equip, ActorEquipSlot slot)
        {
            bool update = false;

            var model = GameResourceManager.Instance.EquipmentData.GetModelById(equip, slot);

            uint icon = 0;
            string modelname = "";

            if (model != null)
            {
                icon = model.Icon;
                modelname = model.Name;
            }
            else
            {
                var data = BitConverter.GetBytes(equip.Value);

                for (int i = 6; i < data.Length; i++)
                {
                    data[i] = (byte)0;
                }

                var finalValue = BitConverter.ToUInt64(data);

                var newModel = GameResourceManager.Instance.EquipmentData.GetModelById(new WeaponModelId() { Value = finalValue}, slot);

                if (newModel != null)
                {
                    icon += newModel.Icon;
                    modelname = newModel.Name;
                }
            }

            int equipId = equip.Id;
            int equipVariant = equip.Variant;
            var Stain0 = GameResourceManager.Instance.Stains[equip.Stain0];
            var Stain1 = GameResourceManager.Instance.Stains[equip.Stain1];

            var defaultImage = GameResourceManager.Instance.GetResourceImage("Qicon.png").ImGuiHandle;

            ImGui.BeginGroup();

            float scaleOffset = .15f;

            // TODO Figure out a way to do this without causing elements to offset.
            /*
            for (var i = 0; i < modelname.Length; i++)
            {
                if (i > 25)
                {
                    scaleOffset += .01f;
                }
            }
            */

            BearGUI.Text(modelname, .85f);

            if (IllusioVitae.InDebug())
            {
                var data = BitConverter.GetBytes(equip.Value);

                string test = "";

                for(var i = 0 ; i < data.Length; i++) 
                {
                    test += data[i].ToString("X");
                }

                ImGui.SetNextItemWidth(127);
                ImGui.InputText($"##{slot}", ref test, 100);

                ImGui.SameLine();

                //if (ImGui.Button("Test"))
                //{
                //    if (TryGetTextureForSlot((int)slot, out var textures))
                //    {
                //        ColorSetWindow.Show(textures);
                //    }
                //}
            }

            if (icon != 0)
            {
                var wrapper = DalamudServices.textureProvider.GetFromGameIcon(new(icon)).GetWrapOrDefault();

                if (wrapper != null) defaultImage = wrapper.ImGuiHandle;
            }
            else
            {
                switch (slot)
                {
                    case ActorEquipSlot.MainHand:
                        defaultImage = GameResourceManager.Instance.GetResourceImage("MainHandSlot.png").ImGuiHandle;
                        break;
                    case ActorEquipSlot.OffHand:
                        defaultImage = GameResourceManager.Instance.GetResourceImage("OffHandSlot.png").ImGuiHandle;
                        break;
                }
            }


            if (BearGUI.ImageButton($"##{slot}gearButtonPrev" ,defaultImage, new Vector2(ImGui.GetTextLineHeight() * 3f)))
            {
                ImGui.OpenPopup($"{slot}ModelPopup");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(slotName);
            }

            DrawGearPopup($"{slot}ModelPopup", slot, (byte)equipVariant, (byte)Stain0.RowId, (byte)Stain1.RowId, true);

            ImGui.SameLine();

            ImGui.BeginGroup();

            ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXX").X);
            if (ImGui.DragInt($"##{slot}gearModelID", ref equipId, 1, 0, 9999))
            {
                CurrentActor.SetWeaponSlot(slot, new() { Id = (ushort)equipId, Variant = (byte)equipVariant, Stain0 = (byte)Stain0.RowId, Stain1 = (byte)Stain1.RowId});
            }

            ImGui.SameLine();

            ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXX").X);
            if (ImGui.DragInt($"##{slot}gearVariantID", ref equipVariant, 1, 0, 999))
            {
                CurrentActor.SetWeaponSlot(slot, new() { Id = (ushort)equipId, Variant = (byte)equipVariant, Type = equip.Type, Stain0 = (byte)Stain0.RowId, Stain1 = (byte)Stain1.RowId});
            }

            if(slot == ActorEquipSlot.MainHand)
            {
                ImGui.SameLine();

                unsafe
                {
                    var currentIcon = CurrentActor.GetCharacter()->DrawData.IsWeaponHidden ? GameResourceManager.Instance.GetResourceImage("VisibilityOff.png").ImGuiHandle : GameResourceManager.Instance.GetResourceImage("VisibilityOn.png").ImGuiHandle;

                    if (BearGUI.ImageButton($"###weaponToggle", currentIcon, new(22, 22)))
                    {
                        CurrentActor.GetCharacter()->DrawData.HideWeapons(!CurrentActor.GetCharacter()->DrawData.IsWeaponHidden);
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Toggle Visibility");
                }
            }

            string Stain0Name = Stain0.Name;

            if (Stain0.Name == string.Empty) Stain0Name = "None";
            var correctedStain0Color = GUIMethods.ARGBToABGR(Stain0.Color);

            if (BearGUI.ColoredLableButton($"##{slot}{Stain0Name}dyeSlotButton", correctedStain0Color, "Dye #1", false, new(71, ImGui.GetTextLineHeight() * 1.5f)))
            {
                if(equip.Id != 0)
                    ImGui.OpenPopup($"{slot}{Stain0Name}dyeButtonPopup");
            }

            ImGui.SameLine();

            string Stain1Name = Stain1.Name;

            if (Stain0.Name == string.Empty) Stain1Name = "None";
            var correctedStain1Color = GUIMethods.ARGBToABGR(Stain1.Color);

            if (BearGUI.ColoredLableButton($"##{slot}{Stain1Name}dyeSlotButton2", correctedStain1Color, "Dye #2", false, new(71, ImGui.GetTextLineHeight() * 1.5f)))
            {
                if(equip.Id != 0)
                    ImGui.OpenPopup($"{slot}{Stain1Name}dyeButtonPopup2");
            }
            ImGui.EndGroup();

            ImGui.EndGroup();

            using (var popup = Popup($"{slot}{Stain0Name}dyeButtonPopup"))
            {
                if (popup.Success)
                {
                    ImGui.Text("Search: ");

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(100);

                    ImGui.InputText($"##{slot}colorsearch", ref dyeSearch, 1000);

                    var availableStains = GameResourceManager.Instance.Stains.Select(x => x.Value).Where(x=> x.Name.RawString.ToLower().Contains(dyeSearch.ToLower())).ToList();

                    ImGui.BeginChild($"##{slot}{Stain0Name}dyeButtonPopupChild", new Vector2(175, ImGui.GetTextLineHeight() * 20));
                    for (var i = 0; i < availableStains.Count; i++)
                    {
                        var currentStain0 = availableStains[i];

                        if (i != 0 && currentStain0.Color == 0) continue;

                        var selectedStainName = currentStain0.Name.RawString;

                        if (selectedStainName == "")
                        {
                            selectedStainName = "None";
                        }

                        if (BearGUI.ColoredLableButton($"##{slot}{i}ColoredDyeButton", GUIMethods.ARGBToABGR(currentStain0.Color), selectedStainName, false, new(138, ImGui.GetTextLineHeight() * 1.5f)))
                        {
                            CurrentActor.SetWeaponSlot(slot, new() { Value = equip.Value, Stain0 = (byte)currentStain0.RowId, Stain1 = (byte)Stain1.RowId}, true);
                            ImGui.CloseCurrentPopup();
                        }
                    }
                    ImGui.EndChild();
                }
            }

            using (var popup = Popup($"{slot}{Stain1Name}dyeButtonPopup2"))
            {
                if (popup.Success)
                {
                    ImGui.Text("Search: ");

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(100);

                    ImGui.InputText($"##{slot}colorsearch", ref dyeSearch, 1000);

                    var availableStains = GameResourceManager.Instance.Stains.Select(x => x.Value).Where(x => x.Name.RawString.ToLower().Contains(dyeSearch.ToLower())).ToList();

                    ImGui.BeginChild($"##{slot}{Stain1Name}dyeButtonPopupChild", new Vector2(175, ImGui.GetTextLineHeight() * 20));
                    for (var i = 0; i < availableStains.Count; i++)
                    {
                        var currentStain1 = availableStains[i];

                        if (i != 0 && currentStain1.Color == 0) continue;

                        var selectedStainName = currentStain1.Name.RawString;

                        if (selectedStainName == "")
                        {
                            selectedStainName = "None";
                        }

                        if (BearGUI.ColoredLableButton($"##{slot}{i}ColoredDyeButton", GUIMethods.ARGBToABGR(currentStain1.Color), selectedStainName, false, new(138, ImGui.GetTextLineHeight() * 1.5f)))
                        {
                            CurrentActor.SetWeaponSlot(slot, new() { Value = equip.Value, Stain0 = (byte)Stain0.RowId, Stain1 = (byte)currentStain1.RowId }, true);
                            ImGui.CloseCurrentPopup();
                        }
                    }
                    ImGui.EndChild();
                }
            }
        }
    
        private unsafe static void DrawCompanionSelector()
        {
            var companion = CurrentActor.GetCompanion();

            var currentIcon = GameResourceManager.Instance.GetResourceImage("MinionSlot.png").ImGuiHandle;

            var currentName = "Companions & Fashion";

            var id = 0;

            if(companion.id != 0)
            {
                switch (companion.type)
                {
                    case companionType.Minion:
                        var minion = GameResourceManager.Instance.Companions[companion.id];

                        var minionWrapper = DalamudServices.textureProvider.GetFromGameIcon(new(minion.Icon)).GetWrapOrDefault();

                        if (minionWrapper != null) currentIcon = minionWrapper.ImGuiHandle;

                        currentName = minion.Singular.ToString().Captialize();
                        id = (int)minion.RowId;
                        break;
                    case companionType.Mount:
                        var mount = GameResourceManager.Instance.Mounts[companion.id];

                        var mountWrapper = DalamudServices.textureProvider.GetFromGameIcon(new(mount.Icon)).GetWrapOrDefault();

                        if(mountWrapper != null) currentIcon = mountWrapper.ImGuiHandle;

                        currentName = mount.Singular.ToString().Captialize();
                        id = (int)mount.RowId;
                        break;
                    case companionType.Ornament:
                        var ornament = GameResourceManager.Instance.Ornaments[companion.id];

                        var ornamentWrapper = DalamudServices.textureProvider.GetFromGameIcon(new(ornament.Icon)).GetWrapOrDefault();

                        if(ornamentWrapper != null) currentIcon = ornamentWrapper.ImGuiHandle;

                        currentName = ornament.Singular.ToString().Captialize();
                        id = (int)ornament.RowId;
                        break;
                }
            }
            
            BearGUI.Text(currentName, .85f);

            if (BearGUI.ImageButton("companionSelect", currentIcon, new Vector2(ImGui.GetTextLineHeight() * 3f)))
            {
                ImGui.OpenPopup("Companion Select Popup");
            };

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Companion Slot");
            }

            using (var popup = ImRaii.Popup("Companion Select Popup"))
            {
                if (popup.Success)
                {
                    ImGui.Text("Search:");
                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(150);
                    ImGui.InputText("##CompanionSearch", ref companionSearch, 1000);

                    if (ImGui.BeginTabBar("CompanionData"))
                    {
                        using (ImRaii.Disabled(CurrentActor.MountCheck()))
                        {
                            if (ImGui.BeginTabItem("Minions"))
                            {
                                using (var listbox = ImRaii.ListBox("###MinionListBox", new(271, 300)))
                                {
                                    if (listbox.Success)
                                    {
                                        var i = 0;

                                        foreach (var item in GameResourceManager.Instance.Companions.Values)
                                        {
                                            var name = $"Minion {item.RowId:D7}";

                                            if (item.Singular != null && item.Singular != "") name = item.Singular.RawString;

                                            if (!name.Contains(companionSearch, StringComparison.OrdinalIgnoreCase)) continue;

                                            if (item.Icon == 0) continue;

                                            i++;

                                            using (ImRaii.PushId(i))
                                            {
                                                var startPos = ImGui.GetCursorPos();

                                                var selected = ImGui.Selectable("###Selector", false, ImGuiSelectableFlags.None, new(0, 50));

                                                var endPos = ImGui.GetCursorPos();

                                                if (ImGui.IsItemVisible())
                                                {
                                                    var icon = DalamudServices.textureProvider.GetFromGameIcon(new(item.Icon)).GetWrapOrDefault();

                                                    if (icon == null) continue;

                                                    ImGui.SetCursorPos(new(startPos.X, startPos.Y + 5));

                                                    ImGui.Image(icon.ImGuiHandle, new(40, 40));
                                                    ImGui.SameLine();

                                                    ImGui.BeginGroup();

                                                    ImGui.Text(name.Captialize());

                                                    ImGui.Text($"ModelType: {item.Model.Row}");

                                                    ImGui.EndGroup();

                                                    ImGui.SetCursorPos(endPos);

                                                    if (selected)
                                                    {
                                                        CurrentActor.SetCompanion(new(companionType.Minion, item.RowId));
                                                        ImGui.CloseCurrentPopup();
                                                    }
                                                }
                                            }
                                        }
                                    }

                                }
                                ImGui.EndTabItem();
                            }

                            if (ImGui.BeginTabItem("Ornaments"))
                            {
                                using (var listbox = ImRaii.ListBox("###NPClistbox", new(271, 300)))
                                {
                                    if (listbox.Success)
                                    {
                                        var i = 0;

                                        foreach (var item in GameResourceManager.Instance.Ornaments.Values)
                                        {
                                            var name = $"Ornament {item.RowId:D7}";

                                            if (item.Singular != null && item.Singular != "") name = item.Singular.RawString;

                                            if (!name.Contains(companionSearch, StringComparison.OrdinalIgnoreCase)) continue;

                                            if (item.Icon == 0) continue;

                                            i++;

                                            using (ImRaii.PushId(i))
                                            {
                                                var startPos = ImGui.GetCursorPos();

                                                var selected = ImGui.Selectable("###Selector", false, ImGuiSelectableFlags.None, new(0, 50));

                                                var endPos = ImGui.GetCursorPos();

                                                if (ImGui.IsItemVisible())
                                                {
                                                    var icon = DalamudServices.textureProvider.GetFromGameIcon(new(item.Icon)).GetWrapOrDefault();

                                                    if (icon == null) continue;

                                                    ImGui.SetCursorPos(new(startPos.X, startPos.Y + 5));

                                                    ImGui.Image(icon.ImGuiHandle, new(40, 40));
                                                    ImGui.SameLine();

                                                    ImGui.BeginGroup();

                                                    ImGui.Text(name.Captialize());

                                                    ImGui.Text($"ModelType: {item.Model}");

                                                    ImGui.EndGroup();

                                                    ImGui.SetCursorPos(endPos);

                                                    if (selected)
                                                    {
                                                        CurrentActor.SetCompanion(new(companionType.Ornament, item.RowId));
                                                        ImGui.CloseCurrentPopup();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                ImGui.EndTabItem();
                            }
                        }

                        using (ImRaii.Disabled((!CurrentActor.MountCheck() && !DalamudServices.clientState.IsGPosing)))
                        {
                            if (ImGui.BeginTabItem("Mounts"))
                            {
                                using (var listbox = ImRaii.ListBox("###NPClistbox", new(271, 300)))
                                {
                                    if (listbox.Success)
                                    {
                                        var i = 0;

                                        foreach (var item in GameResourceManager.Instance.Mounts.Values)
                                        {
                                            var name = $"Mount {item.RowId:D7}";

                                            if (item.Singular != null && item.Singular != "") name = item.Singular.RawString;

                                            if (!name.Contains(companionSearch, StringComparison.OrdinalIgnoreCase)) continue;

                                            if (item.Icon == 0) continue;

                                            i++;

                                            using (ImRaii.PushId(i))
                                            {
                                                var startPos = ImGui.GetCursorPos();

                                                var selected = ImGui.Selectable("###Selector", false, ImGuiSelectableFlags.None, new(0, 50));

                                                var endPos = ImGui.GetCursorPos();

                                                if (ImGui.IsItemVisible())
                                                {
                                                    var icon = DalamudServices.textureProvider.GetFromGameIcon(new(item.Icon)).GetWrapOrEmpty();

                                                    if (icon == null) continue;

                                                    ImGui.SetCursorPos(new(startPos.X, startPos.Y + 5));

                                                    ImGui.Image(icon.ImGuiHandle, new(40, 40));
                                                    ImGui.SameLine();

                                                    ImGui.BeginGroup();

                                                    ImGui.Text(name.Captialize());

                                                    ImGui.Text($"ModelType: {item.ModelChara.Row}");

                                                    ImGui.EndGroup();

                                                    ImGui.SetCursorPos(endPos);

                                                    if (selected)
                                                    {
                                                        CurrentActor.SetCompanion(new(companionType.Mount, item.RowId));
                                                        ImGui.CloseCurrentPopup();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                ImGui.EndTabItem();
                            }
                        }
                    }
                }
            }

            ImGui.SameLine();

            ImGui.BeginGroup();

            ImGui.Text(id.ToString());

            using (ImRaii.Disabled(CurrentActor.MountCheck() && !DalamudServices.clientState.IsGPosing))
            {
                if (ImGui.Button("Clear Slot"))
                {
                    CurrentActor.ClearCompanion();
                }
            }

            ImGui.EndGroup();
        }

        private static void DrawFacewearSelector()
        {
            int glassesID = CurrentActor.GetFacewear(0);
            int unkID = CurrentActor.GetFacewear(1);

            var currentIcon = GameResourceManager.Instance.GetResourceImage("GlassesSlot.png").ImGuiHandle;

            var currentName = "Facewear Slot";

            if(glassesID != 0)
            {
                var glasses = GameResourceManager.Instance.Glasses[(uint)glassesID];

                currentName = glasses.Name.RawString.Captialize() ?? "Unknown";

                var wrapper = DalamudServices.textureProvider.GetFromGameIcon(new((uint)glasses.Icon)).GetWrapOrDefault();

                if (wrapper != null) currentIcon = wrapper.ImGuiHandle;
            }

            BearGUI.Text(currentName, .85f);

            if (BearGUI.ImageButton("facewearSelect", currentIcon, new Vector2(ImGui.GetTextLineHeight() * 3f)))
            {
                ImGui.OpenPopup("facewear Select Popup");
            };

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Facewear Slot");
            }

            using (var popup = ImRaii.Popup("facewear Select Popup"))
            {
                if (popup.Success)
                {
                    ImGui.Text("Search:");
                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(150);
                    ImGui.InputText("##FacewearSearch", ref facewearSearch, 1000);

                    using (var listbox = ImRaii.ListBox("###FacewearListbox", new(271, 300)))
                    {
                        if (listbox.Success)
                        {
                            var i = 0;
                             
                            foreach (var item in GameResourceManager.Instance.Glasses.Values)
                            {
                                var name = $"Glasses {item.RowId:D7}";

                                name = item.Name.RawString;

                                if (!name.Contains(facewearSearch, StringComparison.OrdinalIgnoreCase)) continue;

                                i++;

                                using (ImRaii.PushId(i))
                                {
                                    var startPos = ImGui.GetCursorPos();

                                    var selected = ImGui.Selectable("###Selector", false, ImGuiSelectableFlags.None, new(0, 50));

                                    var endPos = ImGui.GetCursorPos();

                                    if (ImGui.IsItemVisible())
                                    {
                                        var icon = DalamudServices.textureProvider.GetFromGameIcon(new((uint)item.Icon)).GetWrapOrDefault();

                                        if (icon == null) continue;

                                        ImGui.SetCursorPos(new(startPos.X, startPos.Y + 5));

                                        ImGui.Image(icon.ImGuiHandle, new(40, 40));
                                        ImGui.SameLine();

                                        ImGui.BeginGroup();

                                        if (string.IsNullOrEmpty(name))
                                        {
                                            name = "None";
                                        }

                                        BearGUI.Text(name.Captialize(), .85f);

                                        //ImGui.Text($"ModelType: {item.Model}");

                                        ImGui.EndGroup();

                                        ImGui.SetCursorPos(endPos);

                                        if (selected)
                                        {
                                            CurrentActor.SetFacewear(0, (ushort)item.RowId);
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

            ImGui.SetNextItemWidth(50);
            if(ImGui.DragInt("##glassesID", ref glassesID, 1, 0, 120))
            {
                CurrentActor.SetFacewear(0, (ushort)glassesID);
            }

            if (IllusioVitae.InDebug())
            {
                ImGui.SameLine();

                ImGui.SetNextItemWidth(50);
                if (ImGui.DragInt("##unk", ref unkID))
                {
                    CurrentActor.SetFacewear(1, (ushort)unkID);
                }
            }
        }
        private static unsafe bool TryGetTextureForSlot(int slot, out ReadOnlySpan<Pointer<Texture>> textures)
        {
            var charBase = CurrentActor.GetCharacterBase()->CharacterBase;

            if (slot >= charBase.SlotCount || charBase.ColorTableTexturesSpan.Length < (slot + 1) * 4)
            {
                textures = [];
                return false; 
            }

            textures = charBase.ColorTableTexturesSpan.Slice(slot * 4, 4);
            return true;
        }
    }

    public struct ValidFeature()
    {
        public int featureID;

        public uint icon;
    }

    public struct GenericItem()
    {
        public byte id;
        public uint icon;
    }
}
