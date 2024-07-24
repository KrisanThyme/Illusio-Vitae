using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using IVPlugin.ActorData;
using IVPlugin.Mods;
using IVPlugin.Resources;
using IVPlugin.Services;
using IVPlugin.UI.Helpers;
using IVPlugin.VFX;
using Penumbra.Api.Enums;
using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using System.Text;
using Lumina.Excel.GeneratedSheets;
using IVPlugin.Core;

namespace IVPlugin.UI.Windows.Tabs
{
    public static class DebugTab
    {
        private static int SongID = 1;
        private static int Priority = 0;

        private static string vfxPath = "";

        private static nint spawnedVFX = nint.Zero;

        private static bool showActor = false, showStatic = false;

        private static Guid generated;

        private static PenumbraApiEc PenumbraApiEc;

        private static int transformID;

        private static string camPath = "";

        private static string sourcePath = "", newPath = "", modName = "";
        private static string popupPath = "";

        private static bool usehooks = false;

        private static string curZoneBg = String.Empty;
        private static uint curZoneID = 0;
        private static bool layerMode = false;
        private static bool swapLayers = false;
        private static bool swapEnv = false;  

        private static TerritoryType selectedMap = null;

        public static void Draw()
        {

            EventManager.GPoseChange += changedGpose;

            BearGUI.Text("Debug Menu", 1.1f);

            ImGui.Spacing();

            bool debugData = IllusioVitae.InDebug();

            if (ImGui.Checkbox("Enable Debug Mode", ref debugData))
            {
                IllusioVitae.configuration.ShowDebugData = debugData;
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.SetNextItemWidth(100);
            ImGui.InputInt("Penumbra IPC Removal Delay", ref ModManager.Instance.removalDelay, 1, 1);

            if (ImGui.Button("Spawn Actor with Companion Slot"))
            {
                ActorManager.Instance.SetUpCustomActor(null, spawnWithCompanion: true);
            }

            if (ImGui.Checkbox("Enable Weapon Hooks", ref usehooks))
            {
                if (usehooks)
                {
                    ActorManager.Instance.EnableWeaponHooks();
                }
                else
                {
                    ActorManager.Instance.DisableWeaponHooks();
                }
            }

            ImGui.Separator();

            unsafe
            {
                if (ImGui.CollapsingHeader("Zone"))
                {
                    var world = LayoutWorld.Instance();

                    using (var listbox = ImRaii.ListBox("##Layouts", new(385, 200)))
                    {
                        if (listbox.Success)
                        {
                            int i = 0;
                            foreach (var layout in GameResourceManager.Instance.territories.Values)
                            {
                                if (layout.Bg.RawString == "") continue;

                                using (ImRaii.PushId(i))
                                {
                                    var selected = ImGui.Selectable($"{layout.Name.RawString} -- {layout.PlaceNameRegion.Value.Name.RawString} - {layout.PlaceName.Value.Name.RawString}", layout == selectedMap);

                                    if (selected)
                                    {
                                        selectedMap = layout;
                                    }
                                }

                                i++;
                            }
                        }
                    }

                    using (ImRaii.Disabled(selectedMap == null))
                    {
                        if (DalamudServices.clientState.IsGPosing)
                        {
                            if (ImGui.Button("Load Layout"))
                            {
                                if (layerMode == false)
                                {
                                    ActorManager.Instance.playerActor.SetOverrideTransform(ActorManager.Instance.playerActor.GetTransform());

                                    curZoneID = world->ActiveLayout->TerritoryTypeId;
                                    curZoneBg = GameResourceManager.Instance.territories[curZoneID].Bg.RawString;
                                    layerMode = true;
                                }

                                WaitForReload();
                            }

                            ImGui.SameLine();

                            /*
                            if (ImGui.Button("Restore Layout"))
                            {
                                layerMode = false;
                                WaitForReload();
                            }
                            */
                        }

                        if (ImGui.Button("Load Environment"))
                        {
                            if (swapEnv == true)
                            {
                                void* orgEnv = world->ActiveLayout->Environment;
                                void* newEnv = world->PrefetchLayout->Environment;
                                world->ActiveLayout->Environment = newEnv;
                                world->PrefetchLayout->Environment = orgEnv;
                                world->ActiveLayout->InitState = 2;
                                swapEnv = false;
                            }

                            string mapname = selectedMap.Bg;
                            world->LoadPrefetchLayout(2, Encoding.ASCII.GetBytes(mapname), 0, 0, selectedMap.RowId, GameMain.Instance(), 0);
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("Swap Environments"))
                        {
                            void* orgEnv = world->ActiveLayout->Environment;
                            void* newEnv = world->PrefetchLayout->Environment;
                            world->ActiveLayout->Environment = newEnv;
                            world->PrefetchLayout->Environment = orgEnv;
                            world->ActiveLayout->InitState = 2;

                            if (swapEnv == false)
                            {
                                swapEnv = true;
                            }
                            else
                            {
                                swapEnv = false;
                            }    
                        }
                    }
                }

                if (ActorManager.Instance.playerActor.IsLoaded())
                {
                    if (ImGui.CollapsingHeader("VFX"))
                    {
                        ImGui.SetNextItemWidth(300);
                        ImGui.InputText("VFX Path##vfxpath", ref vfxPath, 10000);

                        using (ImRaii.Disabled(DalamudServices.TargetManager.Target == null || vfxPath == ""))
                        {
                            if (ImGui.Button("Spawn Target VFX"))
                            {
                                spawnedVFX = VFXManager.Instance.SpawnActorVFX(vfxPath, ActorManager.Instance.playerActor.actorObject.Address, DalamudServices.TargetManager.Target.Address);
                            }
                        }

                        ImGui.SameLine();

                        using (ImRaii.Disabled(vfxPath == ""))
                        {
                            if (ImGui.Button("Spawn Static VFX"))
                            {
                                VFXManager.Instance.SpawnStaticVFX(vfxPath, ActorManager.Instance.playerActor.actorObject.Position, new(0, 0, ActorManager.Instance.playerActor.actorObject.Rotation));
                            }
                        }

                        if (ImGui.Button("Force Remove All VFX"))
                        {
                            foreach (var vfx in VFXManager.Instance.ActorVFX)
                            {
                                if (!vfx.Value.Removed)
                                {
                                    VFXManager.Instance.RemoveActorVFX(vfx.Key);
                                }
                            }

                            foreach (var vfx in VFXManager.Instance.staticVFX)
                            {
                                if (!vfx.Value.Removed)
                                {
                                    VFXManager.Instance.RemoveStaticVFX(vfx.Key);
                                }
                            }
                        }

                        ImGui.Spacing();

                        ImGui.Checkbox("Show Actor VFX", ref showActor);

                        ImGui.SameLine();

                        ImGui.Checkbox("Show Static VFX", ref showStatic);

                        using (var listbox = ImRaii.ListBox("##VFXTable", new(500, 200)))
                        {
                            if (listbox.Success)
                            {
                                if (showActor)
                                {
                                    foreach (var vfx in VFXManager.Instance.ActorVFX)
                                    {
                                        ImGui.Text("A-VFX");
                                        ImGui.SameLine();
                                        ImGui.TextColored(vfx.Value.Removed ? IVColors.Red : IVColors.Green, vfx.Key.ToString("X"));
                                        ImGui.SameLine();
                                        ImGui.Text(vfx.Value.Path);
                                    }
                                }

                                if (showStatic)
                                {
                                    foreach (var vfx in VFXManager.Instance.staticVFX)
                                    {
                                        ImGui.Text("S-VFX");
                                        ImGui.SameLine();
                                        ImGui.TextColored(vfx.Value.Removed ? IVColors.Red : IVColors.Green, vfx.Key.ToString("X"));
                                        ImGui.SameLine();
                                        ImGui.Text(vfx.Value.Path);
                                    }
                                }
                            }
                        }
                    }

                    //if (ImGui.CollapsingHeader("Replaced Files"))
                    //{
                    //    ImGui.InputText("Mod Name", ref modName, 100);
                    //    ImGui.InputText("Source File", ref sourcePath, 260);
                    //    ImGui.InputText("Replace File", ref newPath, 260);

                    //    using (ImRaii.Disabled(string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(newPath) || string.IsNullOrEmpty(modName)))
                    //    {
                    //        if (ImGui.Button("Add Replacement"))
                    //        {
                    //            Dictionary<string, string> replacements = new();
                    //            replacements.Add(sourcePath, newPath);
                    //           ResourceProcessor.Instance.AddReplacePath(modName, replacements);
                    //        }
                    //    }

                    //    //using (var listbox = ImRaii.ListBox("##ReplacementTable", new(500, 200)))
                    //    //{
                    //    //    if (listbox.Success)
                    //    //    {
                    //    //        foreach (var file in ResourceProcessor.Instance.replacedPaths)
                    //    //        {
                    //    //            var selected = ImGui.Selectable($"##{file.Key}", file.Key == popupPath, ImGuiSelectableFlags.DontClosePopups);
                    //    //            ImGui.SameLine(0);
                    //    //            ImGui.Text(file.Key);

                    //    //            if (selected)
                    //    //            {
                    //    //                popupPath = file.Key;
                    //    //                ImGui.OpenPopup("PathPopup");
                    //    //            }

                    //    //            using (var popup = ImRaii.Popup("PathPopup"))
                    //    //            {
                    //    //                if (popup.Success)
                    //    //                {
                    //    //                    if (ImGui.Selectable("Remove", false))
                    //    //                    {
                    //    //                        ResourceProcessor.Instance.RemoveReplacePath(popupPath);
                    //    //                        ImGui.CloseCurrentPopup();
                    //    //                    }
                    //    //                }
                    //    //            }
                    //    //        }
                    //    //    }
                    //    //}
                    //}

                    ImGui.EndTabItem();
                }
            }
        }

        static void changedGpose(bool inGpose)
        {
            if (inGpose) return;

            if (layerMode == true)
            {
                layerMode = false;
                WaitForReload();
            }
            else
            {
                return;
            }
        }

        static unsafe void WaitForReload()
        {
            DalamudServices.framework.RunOnTick(() =>
            {
                var world = LayoutWorld.Instance();

                if (world->ActiveLayout->TerritoryTypeId != 0)
                {
                    if (world->PrefetchLayout == null)
                    {
                        swapLayers = false;
                    }

                    if (swapLayers == true)
                    {
                        LayoutManager* orgZone = world->ActiveLayout;
                        LayoutManager* newZone = world->PrefetchLayout;
                        world->ActiveLayout = newZone;
                        world->PrefetchLayout = orgZone;
                    }

                    //world->LoadPrefetchLayout(2, Encoding.ASCII.GetBytes("ffxiv/zon_z1/chr/z1c1/level/z1c1"), 0, 0, 0, GameMain.Instance(), 0);
                    world->LoadPrefetchLayout(2, Encoding.ASCII.GetBytes("ffxiv/dummy"), 0, 0, 0, GameMain.Instance(), 0);
                    WaitForPrefetch();
                }
                else
                {
                    WaitForPrefetch();
                }
            }, TimeSpan.FromTicks(1));
        }

        static unsafe void WaitForPrefetch()
        {
            DalamudServices.framework.RunOnTick(() =>
            {
                var world = LayoutWorld.Instance();

                if (world->ActiveLayout->TerritoryTypeId != 0)
                {
                    LayoutManager* orgZone = world->ActiveLayout;
                    LayoutManager* newZone = world->PrefetchLayout;
                    world->ActiveLayout = newZone;
                    world->PrefetchLayout = orgZone;
                    world->ActiveLayout->InitState = 2;

                    WaitForLoad();
                }
                else
                {
                    WaitForLoad();
                }
            }, TimeSpan.FromTicks(1));
        }

        static unsafe void WaitForLoad()
        {
            DalamudServices.framework.RunOnTick(() =>
            {
                var world = LayoutWorld.Instance();
                if (layerMode != false)
                {
                    string mapname = selectedMap.Bg;
                    world->LoadPrefetchLayout(2, Encoding.ASCII.GetBytes(mapname), 0, 0, selectedMap.RowId, GameMain.Instance(), 0);
                }
                else
                {
                    world->LoadPrefetchLayout(2, Encoding.ASCII.GetBytes(curZoneBg), 0, 0, curZoneID, GameMain.Instance(), 0);
                }
                WaitForActive();


            }, TimeSpan.FromTicks(1));
        }

        static unsafe void WaitForActive()
        {
            DalamudServices.framework.RunOnTick(() =>
            {
                var world = LayoutWorld.Instance();

                if (world->PrefetchLayout->InitState >= 4)
                {
                    world->PrefetchLayout->InitState = 2;

                    FinalizeLayout();
                }
                else
                {
                    WaitForActive();
                }
            }, TimeSpan.FromTicks(1));
        }

        static unsafe void FinalizeLayout()
        {
            DalamudServices.framework.RunOnTick(() =>
            {
                var world = LayoutWorld.Instance();

                if (world->PrefetchLayout->InitState >= 7)
                {
                    /*
                    var orgInstance = world->ActiveLayout->InstancesByType;
                    var newInstance = world->PrefetchLayout->InstancesByType;
                    //world->ActiveLayout = newZone;
                    world->PrefetchLayout->InstancesByType = orgInstance;
                    */

                    LayoutManager* orgZone = world->ActiveLayout;
                    LayoutManager* newZone = world->PrefetchLayout;
                    world->ActiveLayout = newZone;
                    world->PrefetchLayout = orgZone;

                    swapLayers = true;

                    if (layerMode == false)
                    {
                        releasePlayer();
                    }
                }
                else
                {
                    FinalizeLayout();
                }
            }, TimeSpan.FromTicks(1));
        }

        static unsafe void releasePlayer()
        {
            DalamudServices.framework.RunOnTick(() =>
            {
                var world = LayoutWorld.Instance();

                if (world->ActiveLayout->InitState >= 7)
                {
                    ActorManager.Instance.playerActor.ResetTransform();
                }
                else
                {
                    releasePlayer();
                }
            }, TimeSpan.FromTicks(1));
        }
    }
}
