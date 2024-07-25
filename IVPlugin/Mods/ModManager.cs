using Dalamud.Game.Text;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Vfx;
using FFXIVClientStructs.FFXIV.Common.Lua;
using FFXIVClientStructs.Havok;
using ImGuizmoNET;
using IVPlugin.ActorData;
using IVPlugin.Actors;
using IVPlugin.Commands;
using IVPlugin.Core;
using IVPlugin.Core.Extentions;
using IVPlugin.Core.Files;
using IVPlugin.Cutscene;
using IVPlugin.Json;
using IVPlugin.Log;
using IVPlugin.Mods.Structs;
using IVPlugin.Resources;
using IVPlugin.Services;
using IVPlugin.UI;
using IVPlugin.VFX;
using Lumina.Data.Parsing.Scd;
using Penumbra.Api;
using Penumbra.Api.Api;
using Penumbra.Api.IpcSubscribers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.AccessControl;
using System.Threading.Tasks;
using static Dalamud.Interface.Utility.Raii.ImRaii;
using static FFXIVClientStructs.FFXIV.Common.Component.BGCollision.MeshPCB;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IVPlugin.Mods
{
    public class ModManager : IDisposable
    {
        public const string configFileName = "config.data", emoteFileName = "meta.data", ivcsModName = "[IVCS] Illusio Vitae Custom Skeletons";
        public bool IVCSEnabled { get; private set; } = false;
        public static ModManager Instance { get; private set; } = null!;

        public Dictionary<string, IVMod> mods = new Dictionary<string, IVMod>();

        public List<XIVActor> modActors = new List<XIVActor>();

        public string ActiveModPath = "";

        public int removalDelay = 1;

        public bool activeMod { get; private set; } = false;
        private nint activeAudioVFX;
        private List<string> internalNames = new();
        public string activeModName = "";

        private Guid lastUsedCollection;
        private int lastUsedPrio = 1;

        public Dictionary<string, List<ModVFXInfo>> ActiveModVFX { get; private set; } = new Dictionary<string, List<ModVFXInfo>>();

        public List<nint> activeAudioVFXs = new List<nint>();

        private bool processingMod = false;

        public bool LockAnimationPlay = false;

        public string PluginFileDirectory = "";

        public Dictionary<RaceCodes, string> defaultCharaFiles = new Dictionary<RaceCodes, string>()
        {
            {RaceCodes.C0101, "Midlander M.chara"},
            {RaceCodes.C0201, "Midlander F.chara"},
            {RaceCodes.C0301, "Highlander M.chara"},
            {RaceCodes.C0401, "Highlander F.chara"},
            {RaceCodes.C0501, "Elezen M.chara"},
            {RaceCodes.C0601, "Elezen F.chara"},
            {RaceCodes.C0701, "Miqo'te M.chara"},
            {RaceCodes.C0801, "Miqo'te F.chara"},
            {RaceCodes.C0901, "Roegadyn M.chara"},
            {RaceCodes.C1001, "Roegadyn F.chara"},
            {RaceCodes.C1101, "Lalafell M.chara"},
            {RaceCodes.C1201, "Lalafell F.chara"},
            {RaceCodes.C1301, "Au Ra M.chara"},
            {RaceCodes.C1401, "Au Ra F.chara" },
            {RaceCodes.C1501, "Hrothgar M.chara"},
            {RaceCodes.C1601, "Hrothgar F.chara"},
            {RaceCodes.C1701, "Viera M.chara"},
            {RaceCodes.C1801, "Viera F.chara"}
        };

        public ModManager()
        {
            Instance = this;

            CheckForMods();

            WriteFilesToDisk();

            if(IllusioVitae.configuration.installIVCS && !IllusioVitae.configuration.firstTimeCheck)
                //EnableIVCSMods(true);

            DalamudServices.framework.Update += ModManagerUpdate;

            EventManager.GPoseChange += (_) => CleanGpose();
        }

        private void CheckForMods()
        {
            if (IllusioVitae.configuration.ModLocation == string.Empty) return;

            var subdirectoryEntries = Directory.GetDirectories(IllusioVitae.configuration.ModLocation);

            foreach (var subdirectoryEntry in subdirectoryEntries)
            {
                try
                {
                    IVMod currentMod = new();

                    if (Path.Exists(Path.Combine(subdirectoryEntry, configFileName)))
                    {
                        currentMod.config = JsonHandler.Deserialize<ModConfig>(File.ReadAllText(Path.Combine(subdirectoryEntry, configFileName)));
                    }
                    else
                    {
                        ModConfig config = new(subdirectoryEntry);

                        File.WriteAllText(Path.Combine(subdirectoryEntry, configFileName), JsonHandler.Serialize(config));

                        currentMod.config = config;
                    }

                    if (Path.Exists(Path.Combine(subdirectoryEntry, "meta.data")))
                    {
                        currentMod.emote = JsonHandler.Deserialize<CustomEmote>(File.ReadAllText(Path.Combine(subdirectoryEntry, "meta.data")));
                    }
                    else
                    {
                        IllusioDebug.Log($"Unable to Process Meta: {subdirectoryEntry}", LogType.Warning);
                        continue;
                    }

                    InstallMod(currentMod);
                }
                catch
                {
                    IllusioDebug.Log($"Unable to Process Mod: {subdirectoryEntry}", LogType.Warning);
                    continue;
                }
            }
        }

        private void ModManagerUpdate(IFramework framework)
        {
            try
            {
                if (!activeMod)
                {
                    if (activeAudioVFXs.Count != 0)
                    {
                        activeAudioVFXs.RemoveAll(x => VFXManager.Instance.ActorVFX.TryGetValue(x, out var result) && result.Removed);


                        foreach (var audioVFX in activeAudioVFXs)
                        {
                            if (VFXManager.Instance.ActorVFX.TryGetValue(audioVFX, out var result))
                            {
                                if (!result.Removed)
                                {
                                    IllusioDebug.Log($"Hiccup Detected! Removing lingering VFX", LogType.Debug);
                                    VFXManager.Instance.RemoveActorVFX(audioVFX);
                                }
                            }
                        }
                    }
                }
            }catch(Exception e)
            {
                IllusioDebug.Log($"{e} Error in mod Manager update", LogType.Error);
            }
            
        }
        
        public void Refresh()
        {
            mods.Clear();
            CommandManager.Instance.RemoveCommands();
            ClearModData();
            CheckForMods();
        }

        public void InstallMod(IVMod mod)
        {
            IllusioDebug.Log($"Initializing Mod: {mod.emote.Name}", LogType.Debug);
            if (mods.ContainsKey(mod.emote.Name))
            {
                IllusioDebug.Log($"Multiple Mods with name {mod.emote.Name}. Unable to process", LogType.Warning, false);
                return;
            }
            
            mods.Add(mod.emote.Name, mod);

            CommandManager.Instance.RegisterNewCommand(mod);
        }

        public void WriteFilesToDisk()
        {
            PluginFileDirectory = Path.Combine(DalamudServices.PluginInterface.ConfigDirectory.FullName, "Files");

            if (Directory.Exists(PluginFileDirectory))
                Directory.Delete(PluginFileDirectory, true);

            Directory.CreateDirectory(PluginFileDirectory);

            if(Directory.Exists(Path.Combine(PluginFileDirectory, "Mods")))
                Directory.Delete(Path.Combine(PluginFileDirectory, "Mods"), true);

            Directory.CreateDirectory(Path.Combine(PluginFileDirectory, "Mods"));

            if (Directory.Exists(Path.Combine(PluginFileDirectory, "Animations")))
                Directory.Delete(Path.Combine(PluginFileDirectory, "Animations"), true);

            Directory.CreateDirectory(Path.Combine(PluginFileDirectory, "Animations"));

            if (Directory.Exists(Path.Combine(PluginFileDirectory, "Chara")))
                Directory.Delete(Path.Combine(PluginFileDirectory, "Chara"), true);

            Directory.CreateDirectory(Path.Combine(PluginFileDirectory, "Chara"));

            var modPack = GameResourceManager.Instance.GetResourceByteFile($"Mods.IVCS.pmp");

            File.WriteAllBytes(Path.Combine(PluginFileDirectory, "Mods", "IVCS.pmp"), modPack);

            var weaponTMB = GameResourceManager.Instance.GetResourceByteFile($"TMB.battle_idle.tmb");

            File.WriteAllBytes(Path.Combine(PluginFileDirectory, "Animations", "battle_idle.tmb"), weaponTMB);

            
            foreach(var charaFile in defaultCharaFiles.Values)
            {
                var f = GameResourceManager.Instance.GetResourceStringFile($"Chara.{charaFile}");
                File.WriteAllText(Path.Combine(PluginFileDirectory, "Chara", charaFile), f);
            }
        }

        public void InstallIVCSMod(bool reload = false)
        {
            if (!DalamudServices.penumbraServices.CheckAvailablity()) return;
           
            new InstallMod(DalamudServices.PluginInterface).Invoke(Path.Combine(PluginFileDirectory, "Mods", "IVCS.pmp"));

            //lastUsedCollection = IllusioVitae.configuration.SelectedCollection;
            //lastUsedPrio = IllusioVitae.configuration.defaultPriority;

            IVCSEnabled = true;
        }

        public void UpdateIVCSMod()
        {
            if (!IVCSEnabled) return;

            DisableIVCSMods();
            //EnableIVCSMods();
        }

        public void DisableIVCSMods()
        {
            if (!DalamudServices.penumbraServices.CheckAvailablity()) return;

            if (!IVCSEnabled) return;

            new RemoveTemporaryMod(DalamudServices.PluginInterface).Invoke(ivcsModName, lastUsedCollection, lastUsedPrio);
        }

        public IVMod GetMod(string name)
        {
            if (mods.ContainsKey(name))
            {
                return mods[name];
            }

            return null;
        }

        public void ToggleModStatus(string modName)
        {
            mods[modName].config.ToggleEnable();
        }

        public void ToggleCameraStatus(string modName)
        {
            mods[modName].config.ToggleCamera();
        }

        public void ToggleBGMStatus(string modName)
        {
            mods[modName].config.ToggleBGM();
        }

        public void ToggleVFXStatus(string modName)
        {
            mods[modName].config.ToggleVFX();
        }

        public void importCamera(string modName)
        {
            if (IllusioVitae.configuration.ModLocation == string.Empty) return;

            WindowsManager.Instance.fileDialogManager.OpenFileDialog("Import Camera", ".xcp", (Confirm, OpenPath) =>
            {
                if (!Confirm) return;

                var subdirectoryEntries = Directory.GetDirectories(IllusioVitae.configuration.ModLocation);

                byte[] file = File.ReadAllBytes(OpenPath);

                string fileName = Path.GetFileName(OpenPath);

                foreach (var subdirectoryEntry in subdirectoryEntries)
                {
                    if (subdirectoryEntry.Contains(modName))
                    {
                        File.WriteAllBytes(Path.Combine(subdirectoryEntry, fileName), file);

                        string emoteDatapath = subdirectoryEntry + "/" + ModManager.emoteFileName;

                        var data = mods[modName].emote;

                        data.cameraPath = Path.Combine(modName, fileName);

                        var newData = JsonHandler.Serialize(data);

                        File.WriteAllText(emoteDatapath, newData);

                        mods[modName].emote = data;
                    }
                }
            });
        }


        public void PlayMod(IVMod mod, int modID = 0, bool spawnNPC = false, bool targetOverride = false)
        {
            if (!mod.emote.allowNPC) spawnNPC = false;

            XIVActor overrideActor = null;

            if (targetOverride && !ActorManager.Instance.TryGetActor(DalamudServices.TargetManager.Target, out overrideActor)){
                overrideActor = ActorManager.Instance.SetUpActor(DalamudServices.TargetManager.Target);

                DalamudServices.framework.RunOnTick(() => PlayMod(mod, modID, spawnNPC, true), TimeSpan.FromSeconds(.1));
                return;
            }

            try
            {
                if (mod.emote.emoteData.Count > 0)
                {
                    if (!spawnNPC)
                    {
                        if (ActiveModVFX.TryGetValue(mod.emote.emoteData[modID].GetCommand() + "VFX", out var vfxList))
                        {
                            if (vfxList.Count > 0)
                            {
                                foreach (var vfx in vfxList)
                                {
                                    switch (vfx.type)
                                    {
                                        case VFXType.actorVFX:
                                            VFXManager.Instance.RemoveActorVFX(vfx.ptr);
                                            break;
                                        case VFXType.staticVFX:
                                            VFXManager.Instance.RemoveStaticVFX(vfx.ptr);
                                            break;
                                        default:
                                            IllusioDebug.Log("Unable to determine VFX Type", LogType.Error, false);
                                            break;
                                    }

                                    //ResourceProcessor.Instance.RemoveReplacePath(vfx.modName);
                                    new RemoveTemporaryModAll(DalamudServices.PluginInterface).Invoke(vfx.modName, int.MaxValue);
                                    
                                }

                                ActiveModVFX.Remove(mod.emote.emoteData[modID].GetCommand() + "VFX");

                                IllusioDebug.Log($"Removing Shared Resources", LogType.Debug);
                                //ResourceProcessor.Instance.RemoveReplacePath($"{mod.emote.Name}SharedIVModPack");
                                new RemoveTemporaryModAll(DalamudServices.PluginInterface).Invoke($"{mod.emote.Name}SharedIVModPack", int.MaxValue);
                                return;
                            }
                        }
                    }

                    XIVActor playerActor = ActorManager.Instance.playerActor;

                    if (DalamudServices.clientState.IsGPosing) playerActor = ActorManager.Instance.mainGposeActor;

                    if ((mod.emote.category == ModCatagory.Male || mod.emote.category == ModCatagory.Gay) && playerActor.GetCustomizeData().Gender == Genders.Feminine)
                    {
                        IllusioDebug.ChatLog("That Command is Unavailable for Female Characters.", XivChatType.ErrorMessage);
                        return;
                    }

                    if ((mod.emote.category == ModCatagory.Female || mod.emote.category == ModCatagory.Lesbian) && playerActor.GetCustomizeData().Gender == Genders.Masculine)
                    {
                        IllusioDebug.ChatLog("That Command is Unavailable for Male Characters.", XivChatType.ErrorMessage);
                        return;
                    }

                    var isBlend = GameResourceManager.Instance.BlendEmotes.Any(x => x.Value.RowId == mod.emote.emoteData[modID].emoteID);

                    if (isBlend) IllusioDebug.Log("Blend Animation Detected for Mod", LogType.Debug);

                    var data = mod.emote;

                    if (LockAnimationPlay)
                    {
                        IllusioDebug.ChatLog($"That Command is on Cooldown.", XivChatType.ErrorMessage);
                        return;
                    }

                    if(!EventManager.validCheck && mod.emote.emoteData[modID].emoteType != EmoteType.Additive)
                    {
                        IllusioDebug.ChatLog($"That Command is Unavailable at this time.", XivChatType.ErrorMessage);

                        return;
                    }

                    if (!ActorManager.Instance.playerActor.validAnim || !mod.config.enabled || processingMod)
                    {
                        if (!ActorManager.Instance.playerActor.validAnim)
                            IllusioDebug.Log($"Player not in valid state.", LogType.Warning);

                        if (!mod.config.enabled)
                            IllusioDebug.Log($"Emote is disabled in config.", LogType.Warning);

                        if (processingMod)
                            IllusioDebug.Log($"Emote is still being processed", LogType.Warning);

                        return;
                    }

                    if (activeMod && mod.emote.emoteData[modID].emoteID != 0 && !isBlend)
                    {
                        IllusioDebug.Log("Mod Active Resetting Player", LogType.Warning);
                        if (DalamudServices.clientState.IsGPosing)
                        {
                            if(overrideActor == null)
                                ActorManager.Instance.mainGposeActor.PlayAnimation(0);
                            else
                                overrideActor.PlayAnimation(0);
                        }
                        else
                        {
                            if (overrideActor == null)
                                ActorManager.Instance.playerActor.PlayAnimation(0);
                            else
                                overrideActor.PlayAnimation(0);
                        }

                        ClearModData();

                        DalamudServices.framework.RunOnTick(() =>
                        {
                            PlayMod(mod, modID, spawnNPC);
                        }, TimeSpan.FromSeconds(.8f));

                        return;
                    }

                    processingMod = true;

                    if(mod.emote.emoteData[modID].emoteID != 0 && !isBlend)
                        ClearModData();

                    activeModName = mod.emote.Name;

                    if (mod.emote.SharedResources != null && mod.emote.SharedResources.Count > 0)
                    {
                        //ResourceProcessor.Instance.AddReplacePath($"{mod.emote.Name}SharedIVModPack", mod.emote.GetSharedResourcesDictionary(mod.config.folderPath));
                        var sharedResult = new AddTemporaryModAll(DalamudServices.PluginInterface).Invoke($"{mod.emote.Name}SharedIVModPack", mod.emote.GetSharedResourcesDictionary(mod.config.folderPath), string.Empty, int.MaxValue);
                    }

                    var mainCollectionName = new GetCollectionForObject(DalamudServices.PluginInterface).Invoke(ActorManager.Instance.playerActor.actorObject.ObjectIndex).Item3;

                    if (modID >= data.emoteData.Count)
                    {
                        processingMod = false;
                        return;
                    }

                    if (!spawnNPC)
                    {
                        var MainEmoteData = data.emoteData[modID];

                        //Check if customEmote is available
                        if (data.emoteData[modID].dataPaths.Count > 0 && !string.IsNullOrEmpty(data.emoteData[modID].dataPaths[0].GamePath))
                        {
                            bool validForRace = false;

                            foreach (var path in MainEmoteData.dataPaths)
                            {
                                if (Path.GetExtension(path.GamePath) != ".pap") continue;

                                if (path.validRaces.HasFlag(ActorManager.Instance.playerActor.GetCustomizeData().GetRaceCode()))
                                {
                                    validForRace = true;
                                    break;
                                }
                            }

                            if (!validForRace)
                            {
                                processingMod = false;

                                IllusioDebug.ChatLog("This Emote is Unavaiable For your Current Race or Gender.", XivChatType.ErrorMessage);

                                return;
                            }
                            var result = Penumbra.Api.Enums.PenumbraApiEc.ModMissing;

                            //ResourceProcessor.Instance.AddReplacePath(data.emoteData[modID].GetCommand(), MainEmoteData.GetDictioanry(mod.config.folderPath));
                            internalNames.Add(data.emoteData[modID].GetCommand());
                            result = new Penumbra.Api.IpcSubscribers.AddTemporaryMod(DalamudServices.PluginInterface).Invoke(data.emoteData[modID].GetCommand(), mainCollectionName.Id, MainEmoteData.GetDictioanry(mod.config.folderPath), string.Empty, int.MaxValue);

                            if (result == Penumbra.Api.Enums.PenumbraApiEc.Success) 
                            {
                                if(data.emoteData[modID].emoteID != 0)
                                {

                                    IVTracklist trackslist = null;
                                    if (!string.IsNullOrEmpty(MainEmoteData.tracklistPath))
                                    {
                                        try
                                        {
                                            trackslist = JsonHandler.Deserialize<IVTracklist>(File.ReadAllText(Path.Combine(mod.config.folderPath, MainEmoteData.tracklistPath)));
                                        }
                                        catch (Exception e)
                                        {
                                            IllusioDebug.Log($"Unable to Parse tracklist for {mod.emote.Name} {MainEmoteData.GetCommand()} + {e}", LogType.Error, false);
                                        }
                                    }

                                    XIVActor actor = null;

                                    if (DalamudServices.clientState.IsGPosing)
                                    {
                                        if (overrideActor == null)
                                            actor = ActorManager.Instance.mainGposeActor;
                                        else
                                            actor = overrideActor;
                                    }
                                    else
                                    {
                                        if (overrideActor == null)
                                            actor = ActorManager.Instance.playerActor;
                                        else
                                            actor = overrideActor;
                                    }

                                    unsafe
                                    {
                                        if (MainEmoteData.disableWeapon && !actor.GetCharacter()->IsWeaponDrawn)
                                        {
                                            Dictionary<string, string> weaponTests = new Dictionary<string, string>
                                        {
                                            { "chara/action/weapon/battle_idle.tmb", Path.Combine(PluginFileDirectory, "Animations", "battle_idle.tmb") }
                                        };
                                            ResourceProcessor.Instance.AddReplacePath("WeaponTMB", weaponTests);

                                            //new AddTemporaryModAll(DalamudServices.PluginInterface).Invoke("WeaponTMB", weaponTests, string.Empty, int.MaxValue);

                                            ActorManager.Instance.EnableWeaponHooks();
                                        }
                                    }
                                    

                                    if (MainEmoteData.isLooping)
                                    {
                                        actor.SetLoopAnimation(MainEmoteData.emoteID, trackslist);
                                    }
                                    else
                                    {
                                        actor.PlayAnimation(MainEmoteData.emoteID, tracklist: trackslist);
                                    }

                                    unsafe
                                    {
                                        if (MainEmoteData.disableWeapon && !actor.GetCharacter()->IsWeaponDrawn)
                                        {

                                            DalamudServices.framework.RunOnTick(() =>
                                            {
                                                ResourceProcessor.Instance.RemoveReplacePath("WeaponTMB");

                                                //new RemoveTemporaryModAll(DalamudServices.PluginInterface).Invoke("WeaponTMB", int.MaxValue);

                                                ActorManager.Instance.DisableWeaponHooks();

                                            }, TimeSpan.FromMilliseconds(500));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                IllusioDebug.Log($"Penumbra could not enable mod", LogType.Warning);
                            }

                            if (DalamudServices.clientState.IsGPosing && mod.config.cameraEnabled)
                                PlayCamera(mod, true);

                            if(overrideActor == null)
                                PlayBGM(mod, mainCollectionName.Id);

                            LockAnimationPlay = true;

                            DalamudServices.framework.RunOnTick(() =>
                            {
                                IllusioDebug.Log($"Removing Mod: {MainEmoteData.GetCommand()}", LogType.Debug);

                                new RemoveTemporaryMod(DalamudServices.PluginInterface).Invoke(MainEmoteData.GetCommand(), mainCollectionName.Id, int.MaxValue);
                                LockAnimationPlay = false;
                            }, TimeSpan.FromSeconds(removalDelay));

                            if (data.emoteData[modID].emoteID != 0 && !isBlend)
                                activeMod = true;
                        }
                        else
                        {
                            if (data.emoteData[modID].emoteID != 0)
                            {
                                //Play Assigned Emote ID
                                if (DalamudServices.clientState.IsGPosing)
                                {
                                    ActorManager.Instance.mainGposeActor.PlayAnimation(MainEmoteData.emoteID);
                                    ActorManager.Instance.mainGposeActor.loopCurrentAnimation = MainEmoteData.isLooping;
                                }
                                else
                                {
                                    ActorManager.Instance.playerActor.PlayAnimation(MainEmoteData.emoteID);
                                    ActorManager.Instance.playerActor.loopCurrentAnimation = MainEmoteData.isLooping;
                                }
                            }
                        }

                        if(mod.config.VFXEnabled && data.emoteData[modID].vfxData != null && data.emoteData[modID].vfxData.Count > 0)
                        {
                            PlayVFX(mod.config, MainEmoteData, DalamudServices.clientState.IsGPosing ? ActorManager.Instance.mainGposeActor : ActorManager.Instance.playerActor); //mainCollectionName.Id
                        }
                        
                        processingMod = false;
                    }
                    else
                    {
                        int actorSlot = 0;

                        List<int> usedSlotIndices = new();

                        bool validForRace = false;

                        foreach (var path in data.emoteData[modID].dataPaths)
                        {
                            if (Path.GetExtension(path.GamePath) != ".pap") continue;

                            if (DalamudServices.clientState.IsGPosing)
                            {
                                if (path.validRaces.HasFlag(ActorManager.Instance.mainGposeActor.GetCustomizeData().GetRaceCode()))
                                {
                                    validForRace = true;
                                }
                            }
                            else
                            {
                                if (path.validRaces.HasFlag(ActorManager.Instance.playerActor.GetCustomizeData().GetRaceCode()))
                                {
                                    validForRace = true;
                                }
                            }
                        }

                        if (!validForRace)
                        {
                            processingMod = false;

                            IllusioDebug.ChatLog("This Emote is Unavaiable For your Current Race or Gender.", XivChatType.ErrorMessage);

                            return;
                        }

                        for (var i = 0; i < data.emoteData.Count; i++)
                        {
                            var CurrentEmoteData = data.emoteData[i];

                            if (CurrentEmoteData.dataPaths[0].GamePath == "")
                            {
                                continue;
                            }

                            XIVActor currentActor = null;

                            if (i != modID)
                            {
                                CharaFile charaFile = null;

                                string actorName = "";

                                bool foundActor = false;

                                for(var x = 0; x < IllusioVitae.configuration.PresetActors.Length; x++)
                                {
                                    if (usedSlotIndices.Contains(x)) continue;

                                    var currentPreset = IllusioVitae.configuration.PresetActors[x];

                                    if (!string.IsNullOrEmpty(currentPreset.charaPath) && File.Exists(currentPreset.charaPath))
                                    {
                                        charaFile = JsonHandler.Deserialize<CharaFile>(File.ReadAllText(currentPreset.charaPath));

                                        if ((mod.emote.category == ModCatagory.Male || mod.emote.category == ModCatagory.Gay) && charaFile.Gender == Genders.Feminine)
                                        {
                                            charaFile = null;
                                            continue;
                                        }

                                        if ((mod.emote.category == ModCatagory.Female || mod.emote.category == ModCatagory.Lesbian) && charaFile.Gender == Genders.Masculine)
                                        {
                                            charaFile = null;
                                            continue;
                                        }

                                        bool validRace = false;

                                        foreach(var path in CurrentEmoteData.dataPaths)
                                        {
                                            if (Path.GetExtension(path.GamePath) != ".pap") continue;

                                            if (path.validRaces.HasFlag(ExtentionMethods.GetRaceCode((Races)charaFile.Race, (Genders)charaFile.Gender, (Tribes)charaFile.Tribe)))
                                            {
                                                validRace = true;
                                                break;
                                            }
                                        }

                                        if (!validRace) continue;

                                        foundActor = true;
                                        usedSlotIndices.Add(x);
                                        actorName = currentPreset.Name;
                                        break;
                                    }

                                    
                                }

                                if (!foundActor)
                                {
                                    RaceCodes playrace = ActorManager.Instance.playerActor.GetCustomizeData().GetRaceCode();

                                    if (DalamudServices.clientState.IsGPosing)
                                    {
                                        playrace = ActorManager.Instance.mainGposeActor.GetCustomizeData().GetRaceCode();
                                    }

                                    bool validRace = false;

                                    foreach (var path in CurrentEmoteData.dataPaths)
                                    {
                                        if (Path.GetExtension(path.GamePath) != ".pap") continue;

                                        if (path.validRaces.HasFlag(playrace))
                                        {
                                            validRace = true;
                                            break;
                                        }
                                    }

                                    if (validRace)
                                    {
                                        charaFile = JsonHandler.Deserialize<CharaFile>(File.ReadAllText(Path.Combine(PluginFileDirectory, "Chara", defaultCharaFiles[playrace])));
                                    }
                                    else
                                    {
                                        var c = Enum.GetName(typeof(RaceCodes), playrace);
                                        var x = Enum.GetNames(typeof(RaceCodes)).ToList();

                                        var index = x.IndexOf(c);

                                        RaceCodes counterPartRace = RaceCodes.C0101;

                                        if(index % 2 == 0) //male
                                        {
                                            counterPartRace = Enum.Parse<RaceCodes>(x[index + 1]);
                                        }else //female
                                        {
                                            counterPartRace = Enum.Parse<RaceCodes>(x[index - 1]);
                                        }

                                        foreach (var path in CurrentEmoteData.dataPaths)
                                        {
                                            if (Path.GetExtension(path.GamePath) != ".pap") continue;

                                            if (path.validRaces.HasFlag(counterPartRace))
                                            {
                                                validRace = true;
                                                break;
                                            }
                                        }

                                        if (validRace)
                                        {
                                            charaFile = JsonHandler.Deserialize<CharaFile>(File.ReadAllText(Path.Combine(PluginFileDirectory, "Chara", defaultCharaFiles[counterPartRace])));
                                        }
                                        else
                                        {
                                            foreach (var path in CurrentEmoteData.dataPaths)
                                            {
                                                if (Path.GetExtension(path.GamePath) != ".pap") continue;

                                                var shouldBreak = false;
                                                foreach(var code in x)
                                                {
                                                    if (path.validRaces.HasFlag(Enum.Parse<RaceCodes>(code)))
                                                    {
                                                        charaFile = JsonHandler.Deserialize<CharaFile>(File.ReadAllText(Path.Combine(PluginFileDirectory, "Chara", defaultCharaFiles[Enum.Parse<RaceCodes>(code)])));

                                                        shouldBreak = true;
                                                        break;
                                                    }
                                                }

                                                if (shouldBreak) break;
                                            }
                                        }
                                    }
                                }

                                currentActor = ActorManager.Instance.SetUpCustomActor(charaFile, actorName);

                                actorSlot++;
                            }
                            else
                            {
                                

                                if (DalamudServices.clientState.IsGPosing)
                                {
                                    currentActor = ActorManager.Instance.mainGposeActor;
                                }
                                else
                                {
                                    currentActor = ActorManager.Instance.playerActor;
                                }
                            }
                            
                            modActors.Add(currentActor);
                        }


                        if(modActors.Count == 0)
                        {
                            processingMod = false;
                            return;
                        }


                        DalamudServices.framework.RunOnTick(() => ModReadyCheck(mod, 500));
                    }
                }

                if (activeMod) ActiveModPath = mod.config.folderPath;
            }
            catch (Exception e)
            {
                IllusioDebug.Log($"{e} Error With Mod Manipulation", LogType.Error);
            }
        }

        private void PlayVFX(ModConfig config, CustomEmoteData data, XIVActor actor)
        {
            List<ModVFXInfo> vfxPointers = new();

            int i = 0;
            foreach (var vfx in data.vfxData)
            {
                if (vfx.validRaces.HasFlag(actor.GetCustomizeData().GetRaceCode()))
                {
                   //ResourceProcessor.Instance.AddReplacePath(data.GetCommand() + "VFX" + i.ToString(), vfx.GetVFXDictionary(config.folderPath));
                    var result = new Penumbra.Api.IpcSubscribers.AddTemporaryModAll(DalamudServices.PluginInterface).Invoke(data.GetCommand() + "VFX" + i.ToString(), vfx.GetVFXDictionary(config.folderPath), string.Empty, int.MaxValue);

                    //var result = Penumbra.Api.Enums.PenumbraApiEc.UnknownError;

                    if(result == Penumbra.Api.Enums.PenumbraApiEc.Success)
                    {
                        foreach(var path in vfx.vfxDatapaths)
                        {
                            if (!string.IsNullOrEmpty(path.LocalPath) && Path.GetExtension(path.LocalPath).ToLower() == ".avfx")
                            {
                                nint activeVFX = nint.Zero;
                                switch (vfx.VFXType)
                                {
                                    case VFXType.actorVFX:
                                        activeVFX = VFXManager.Instance.SpawnActorVFX(path.GamePath, actor.actorObject.Address, actor.actorObject.Address);
                                        vfxPointers.Add(new() { type = VFXType.actorVFX, ptr = activeVFX, modName = data.GetCommand() + "VFX" + i.ToString() });
                                        break;
                                    case VFXType.staticVFX:
                                        activeVFX = VFXManager.Instance.SpawnStaticVFX(path.GamePath, actor.actorObject.Position, new Vector3(0,0, actor.actorObject.Rotation));
                                        vfxPointers.Add(new() { type = VFXType.staticVFX, ptr = activeVFX, modName = data.GetCommand() + "VFX" + i.ToString() });
                                        break;
                                    default:
                                        IllusioDebug.Log($"Unable to process VFX Type for mod {data.emoteCommand}", LogType.Error, false);
                                        break;
                                }
                            }
                        }
                    }

                    i++;
                }
            }
                
            if(vfxPointers.Count > 0)
                ActiveModVFX.Add(data.GetCommand() + "VFX", vfxPointers);
        }

        private async Task ModReadyCheck(IVMod mod, int maxAttempts, int currentAttempt = 0)
        {
            if (maxAttempts == currentAttempt)
            {
                IllusioDebug.Log($"Unable to play mod. Max Allocated Ticks", LogType.Error);
                processingMod = false;
                LockAnimationPlay = false;
                ClearModData();
                
                return;
            }

            int isReady = 0;

            foreach (var actor in modActors)
            {
                if (actor.IsLoaded(true))
                {
                    isReady++;
                }
            }

            var result = isReady == modActors.Count;

            if (result)
            {
                IllusioDebug.Log($"Completed at {currentAttempt} ticks", LogType.Info);
                PlayMultiMod(mod);
            }
            else
            {
                currentAttempt += 1;
                DalamudServices.framework.RunOnTick(() => ModReadyCheck(mod, maxAttempts, currentAttempt));
            }
        }

        private void PlayMultiMod(IVMod mod)
        {
            if (modActors.Count == 0)
            {
                processingMod = false;
                return;
            }

            for (int i = 0; i < mod.emote.emoteData.Count; i++)
            {
                var currentEmoteData = mod.emote.emoteData[i];
                var currentActor = modActors[i];

                if (currentActor.isCustom && currentActor.GetName() == "Illusio Actor")
                {
                    currentActor.Rename($"Illusio {CustomActor.GetColumnName(i)}-actor");
                }

                var collectionGUID = new GetCollectionForObject(DalamudServices.PluginInterface).Invoke(currentActor.actorObject.ObjectIndex).Item3.Id;

                if(currentActor != ActorManager.Instance.playerActor)
                {
                    collectionGUID = new CreateTemporaryCollection(DalamudServices.PluginInterface).Invoke($"Custom Actor Collection {i}");

                    new AssignTemporaryCollection(DalamudServices.PluginInterface).Invoke(collectionGUID, currentActor.actorObject.ObjectIndex);
                }

                //ResourceProcessor.Instance.AddReplacePath(currentEmoteData.GetCommand(), currentEmoteData.GetDictioanry(mod.config.folderPath));
                internalNames.Add(currentEmoteData.GetCommand());
                var result = new AddTemporaryMod(DalamudServices.PluginInterface).Invoke(currentEmoteData.GetCommand(), collectionGUID, currentEmoteData.GetDictioanry(mod.config.folderPath), string.Empty, int.MaxValue);

                //var result = Penumbra.Api.Enums.PenumbraApiEc.UnknownError;

                if (result == Penumbra.Api.Enums.PenumbraApiEc.Success)
                {
                    IVTracklist trackslist = null;
                    if (!string.IsNullOrEmpty(currentEmoteData.tracklistPath))
                    {
                        try
                        {
                            trackslist = JsonHandler.Deserialize<IVTracklist>(File.ReadAllText(Path.Combine(mod.config.folderPath, currentEmoteData.tracklistPath)));
                        }
                        catch(Exception e)
                        {
                            IllusioDebug.Log($"Unable to Parse tracklist for {mod.emote.Name} {currentEmoteData.GetCommand()}", LogType.Error, false);
                        }
                    }

                    unsafe
                    {
                        if (currentEmoteData.disableWeapon && !currentActor.GetCharacter()->IsWeaponDrawn)
                        {
                            Dictionary<string, string> weaponTests = new Dictionary<string, string>
                            {
                                { "chara/action/weapon/battle_idle.tmb", Path.Combine(PluginFileDirectory, "Animations", "battle_idle.tmb") }
                            };
                            ResourceProcessor.Instance.AddReplacePath("WeaponTMB", weaponTests);

                            //new AddTemporaryModAll(DalamudServices.PluginInterface).Invoke("WeaponTMB", weaponTests, string.Empty, int.MaxValue);

                            ActorManager.Instance.EnableWeaponHooks();
                        }
                    }

                    if (currentEmoteData.isLooping)
                    {
                        currentActor.SetLoopAnimation(currentEmoteData.emoteID, tracklist: trackslist);
                    }   
                    else
                    {
                        currentActor.PlayAnimation(currentEmoteData.emoteID, tracklist: trackslist);
                    }
                }

                DalamudServices.framework.RunOnTick(() =>
                {
                    IllusioDebug.Log($"Removing Multi Mod: {currentEmoteData.GetCommand()}", LogType.Debug);

                    DalamudServices.framework.RunOnTick(() =>
                    {
                        ResourceProcessor.Instance.RemoveReplacePath("WeaponTMB");

                        //new RemoveTemporaryModAll(DalamudServices.PluginInterface).Invoke("WeaponTMB", int.MaxValue);

                        ActorManager.Instance.DisableWeaponHooks();

                    }, TimeSpan.FromMilliseconds(200));

                    new RemoveTemporaryMod(DalamudServices.PluginInterface).Invoke(currentEmoteData.GetCommand(), collectionGUID, int.MaxValue);

                    if (currentActor != ActorManager.Instance.playerActor) ;
                        new DeleteTemporaryCollection(DalamudServices.PluginInterface).Invoke(collectionGUID);
                }, TimeSpan.FromSeconds(removalDelay));
            }

            if (DalamudServices.clientState.IsGPosing && mod.config.cameraEnabled)
                PlayCamera(mod, false);

            PlayBGM(mod, new GetCollectionForObject(DalamudServices.PluginInterface).Invoke(ActorManager.Instance.playerActor.actorObject.ObjectIndex).EffectiveCollection.Id);

            processingMod = false;

            LockAnimationPlay = true;

            DalamudServices.framework.RunOnTick(() =>
            {
                LockAnimationPlay = false;
            }, TimeSpan.FromSeconds(removalDelay));

            activeMod = true;

            ActiveModPath = mod.config.folderPath;
        }

        private void PlayCamera(IVMod mod, bool useCharaHeight)
        {
            if (!string.IsNullOrEmpty(mod.emote.cameraPath))
            {
                IllusioCutsceneManager.Instance.CameraPath = mod.emote.GetCameraFile(mod.config.folderPath);

                IllusioCutsceneManager.Instance.StartPlayback(useCharaHeight);
            }
        }
    
        private void PlayBGM(IVMod mod, Guid mainCollectionName)
        {
            if (mod.config.BGMEnabled)
            {
                if (mod.emote.bgmData.vfxPath != null && mod.emote.bgmData.scdPath != null)
                {
                    //ResourceProcessor.Instance.AddReplacePath($"{mod.emote.Name}BGMVFX", mod.emote.GetBGMDictionary(mod.config.folderPath));
                    var result = new AddTemporaryMod(DalamudServices.PluginInterface).Invoke($"{mod.emote.Name}BGMVFX", mainCollectionName, mod.emote.GetBGMDictionary(mod.config.folderPath), string.Empty, int.MaxValue);

                    //var result = Penumbra.Api.Enums.PenumbraApiEc.UnknownError;

                    if (result == Penumbra.Api.Enums.PenumbraApiEc.Success && ActorManager.Instance.playerActor.IsLoaded()) //
                    {
                        var actorAdress = ActorManager.Instance.playerActor.actorObject.Address;

                        if (DalamudServices.clientState.IsGPosing)
                        {
                            actorAdress = ActorManager.Instance.GPoseActors[0].actorObject.Address;
                        }

                        activeAudioVFX = VFXManager.Instance.SpawnActorVFX($"vfx/illusiovitae/bgm/{Path.GetFileNameWithoutExtension(mod.emote.bgmData.vfxPath)}.avfx".ToLower(), actorAdress, actorAdress);

                        activeAudioVFXs.Add(activeAudioVFX);

                        DalamudServices.framework.RunOnTick(() =>
                        {
                            IllusioDebug.Log($"Removing Mod: {mod.emote.Name}BGMVFX", LogType.Debug);


                           //ResourceProcessor.Instance.RemoveReplacePath($"{mod.emote.Name}BGMVFX");
                           new RemoveTemporaryMod(DalamudServices.PluginInterface).Invoke($"{mod.emote.Name}BGMVFX", mainCollectionName, int.MaxValue);
                        }, TimeSpan.FromSeconds(removalDelay));
                    }
                }
            }
        }
        
        public void DeleteMod(string modName)
        {
            if (IllusioVitae.configuration.ModLocation == string.Empty)
            {
                IllusioDebug.Log($"Could Not find Modlocation in configuration", LogType.Error);
                return;
            }

            if (mods.TryGetValue(modName, out var result))
            {
                Directory.Delete(result.config.folderPath, true);

                mods.Remove(modName);

                Refresh();
            }
            else
            {
                IllusioDebug.Log($"Unable to find mod {modName} for deletion", LogType.Error);
                return;
            }
        }

        

        public void ClearModData()
        {
            if (activeMod)
            {

                IllusioDebug.Log("Clearing current mod data", LogType.Debug);

                foreach (var actor in modActors)
                {
                    actor.DestroyActor();
                }

                IllusioDebug.Log($"Removing Shared Resources", LogType.Debug);

                //ResourceProcessor.Instance.RemoveReplacePath($"{activeModName}SharedIVModPack");
                new RemoveTemporaryModAll(DalamudServices.PluginInterface).Invoke($"{activeModName}SharedIVModPack", int.MaxValue);

                modActors.Clear();

                if (activeAudioVFX != nint.Zero && activeAudioVFXs.Count > 0)
                {
                    VFXManager.Instance.RemoveActorVFX(activeAudioVFXs.Last());
                }

                
                DalamudServices.framework.RunOnTick(() => 
                {
                    foreach (var modName in internalNames)
                    {
                        //ResourceProcessor.Instance.RemoveReplacePath(modName);
                    }

                    internalNames.Clear();
                }, TimeSpan.FromSeconds(.5f));
                    

                ActiveModPath = "";

                activeModName = "";

                activeMod = false;

                if (IllusioCutsceneManager.Instance.IsRunning)
                {
                    IllusioCutsceneManager.Instance.StopPlayback();
                }

                if (DalamudServices.clientState.IsGPosing)
                {
                    ActorManager.Instance.mainGposeActor.FadeIn();
                    ActorManager.Instance.mainGposeActor.RestoreOutfitFromAnim();
                }
                else
                {
                    ActorManager.Instance.playerActor.FadeIn();
                    ActorManager.Instance.playerActor.RestoreOutfitFromAnim();
                }
            }
        }

        private void CleanGpose()
        {
            foreach(var vfxList in ActiveModVFX.Values)
            {
                foreach (var vfx in vfxList)
                {
                    switch (vfx.type)
                    {
                        case VFXType.actorVFX:
                            VFXManager.Instance.RemoveActorVFX(vfx.ptr);
                            break;
                        case VFXType.staticVFX:
                            VFXManager.Instance.RemoveStaticVFX(vfx.ptr);
                            break;
                        default:
                            IllusioDebug.Log("Unable to determine VFX Type", LogType.Error, false);
                            break;
                    }
                }
            }

            ActiveModVFX.Clear();

            ActorManager.Instance.playerActor.PlayAnimation(0);

            ClearModData();
        }

        public void Dispose()
        {

            if (DalamudServices.penumbraServices.CheckAvailablity())
            {
                DisableIVCSMods();
            }

            DalamudServices.framework.Update -= ModManagerUpdate;
            EventManager.GPoseChange -= (_) => ClearModData();

            if (activeAudioVFXs.Count != 0)
            {
                activeAudioVFXs.RemoveAll(x => VFXManager.Instance.ActorVFX.TryGetValue(x, out var result) && result.Removed);


                foreach (var audioVFX in activeAudioVFXs)
                {
                    if (VFXManager.Instance.ActorVFX.TryGetValue(audioVFX, out var result))
                    {
                        if (!result.Removed)
                        {
                            IllusioDebug.Log($"Hiccup Detected! Removing lingering VFX", LogType.Debug);
                            VFXManager.Instance.RemoveActorVFX(audioVFX);
                        }
                    }
                }
            }

            foreach (var vfxData in ActiveModVFX.Values)
            {
                foreach(var vfx in vfxData)
                {
                    switch (vfx.type)
                    {
                        case VFXType.actorVFX:
                            VFXManager.Instance.RemoveActorVFX(vfx.ptr);
                            break;
                        case VFXType.staticVFX:
                            VFXManager.Instance.RemoveStaticVFX(vfx.ptr);
                            break;
                        default:
                            IllusioDebug.Log("Unable to determine VFX Type", LogType.Error, false);
                            break;
                    }

                    new RemoveTemporaryModAll(DalamudServices.PluginInterface).Invoke(vfx.modName, int.MaxValue);
                }
            }
        }
    }

    public class IVMod
    {
        public CustomEmote emote;
        public ModConfig config;
        public ErrorType error;

        public IVMod()
        {
            emote = new();
            config = new();
            error = new();
        }
    }

    public struct ModVFXInfo
    {
        public VFXType type;
        public nint ptr;
        public string modName;
    }

    [Flags]
    public enum ErrorType { none = 0, unknown = 1, commandError = 2, configErorr = 4, metaError = 8}
}
