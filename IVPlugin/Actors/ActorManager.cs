using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using IVPlugin.Actors.Structs;
using IVPlugin.Core;
using IVPlugin.Core.Extentions;
using IVPlugin.Core.Files;
using IVPlugin.Log;
using IVPlugin.Mods;
using IVPlugin.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;


namespace IVPlugin.ActorData
{
    public unsafe class ActorManager : IDisposable
    {
        public static ActorManager Instance { get; private set; } = null!;
        public LocalPlayerActor playerActor { get; private set; }
        public XIVActor mainGposeActor { get; private set; }
        public List<XIVActor> Actors { get; private set; } = new List<XIVActor>();
        public List<XIVActor> GPoseActors { get; private set; } = new List<XIVActor>();
        public Dictionary<int, XIVActor> CustomActors { get; private set; } = new();

        private delegate byte EnforceKindRestrictionsDelegate(nint a1, nint a2);
        private readonly Hook<EnforceKindRestrictionsDelegate> _enforceKindRestrictionsHook = null!;

        private delegate bool CalculateAndApplyOverallSpeedDelegate(TimelineContainer* a1);
        private readonly Hook<CalculateAndApplyOverallSpeedDelegate> _calculateAndApplyOverallSpeedHook = null!;

        private delegate void SetSlotSpeedDelegate(ActionTimelineSequencer* a1, int slot, float speed);
        private readonly Hook<SetSlotSpeedDelegate> _setSpeedSlotHook = null!;

        internal unsafe delegate void ChangeGlassesDelegate(ExtendedDrawDataContainer* writeTo, int slot, ushort id);
        internal static ChangeGlassesDelegate? ChangeGlasses;

        private delegate byte WeaponAttachmentDelegate(nint a1);
        private readonly Hook<WeaponAttachmentDelegate> _weaponAttachmentHook = null!;

        private delegate byte WeaponScaleDelegate(nint a1);
        private readonly Hook<WeaponScaleDelegate> _weaponScaleHook = null!;

        private delegate byte WeaponStateDelegate(nint a1);
        private readonly Hook<WeaponStateDelegate> _weaponStateHook = null!;

        public ActorManager()
        {
            Instance = this;

            DalamudServices.framework.Update += Update;

            DalamudServices.clientState.Login += GetLocalPlayer;

            DalamudServices.clientState.TerritoryChanged += RemoveActors;

            var enforceKindRestrictionsAddress = DalamudServices.SigScanner.ScanText(XIVSigs.npcFeatureRestriction);
            _enforceKindRestrictionsHook = DalamudServices.GameInteropProvider.HookFromAddress<EnforceKindRestrictionsDelegate>(enforceKindRestrictionsAddress, EnforceKindRestrictionsDetour);
            _enforceKindRestrictionsHook.Enable();

            var calculateAndApplyAddress = DalamudServices.SigScanner.ScanText(XIVSigs.animationCalcApplySpeed);
            _calculateAndApplyOverallSpeedHook = DalamudServices.GameInteropProvider.HookFromAddress<CalculateAndApplyOverallSpeedDelegate>(calculateAndApplyAddress, CalculateAndApplyOverallSpeedDetour);
            _calculateAndApplyOverallSpeedHook.Enable();

            _setSpeedSlotHook = DalamudServices.GameInteropProvider.HookFromAddress<SetSlotSpeedDelegate>(ActionTimelineSequencer.Addresses.SetSlotSpeed.Value, SetSlotSpeedDetour);
            _setSpeedSlotHook.Enable();

            var weaponAttachAdd = DalamudServices.SigScanner.ScanText(XIVSigs.animationWeaponAttach);
            _weaponAttachmentHook = DalamudServices.GameInteropProvider.HookFromAddress<WeaponAttachmentDelegate>(weaponAttachAdd, WeaponAttachDetour);

            var weaponScaleAdd = DalamudServices.SigScanner.ScanText(XIVSigs.animationWeaponScale);
            _weaponScaleHook = DalamudServices.GameInteropProvider.HookFromAddress<WeaponScaleDelegate>(weaponScaleAdd, WeaponScaleDetour);

            var weaponStateAdd = DalamudServices.SigScanner.ScanText(XIVSigs.animationWeaponState);
            _weaponStateHook = DalamudServices.GameInteropProvider.HookFromAddress<WeaponStateDelegate>(weaponStateAdd, WeaponStateDetour);

            var changeGlassesAddress = DalamudServices.SigScanner.ScanText(XIVSigs.gearswapFacewear);
            ChangeGlasses = Marshal.GetDelegateForFunctionPointer<ChangeGlassesDelegate>(changeGlassesAddress);
        }

        private byte WeaponAttachDetour(nint a1)
        {
            return 0;
        }

        private byte WeaponScaleDetour(nint a1)
        {
            return 0;
        }

        private byte WeaponStateDetour(nint a1)
        {
            return 0;
        }

        public void EnableWeaponHooks()
        {
            _weaponAttachmentHook.Enable();
            _weaponScaleHook.Enable();
            _weaponStateHook.Enable();
        }

        public void DisableWeaponHooks()
        {
            _weaponAttachmentHook.Disable();
            _weaponScaleHook.Disable();
            _weaponStateHook.Disable();
        }

        private byte EnforceKindRestrictionsDetour(nint a1, nint a2)
        {
            if (IllusioVitae.configuration.UseNPCHack)
            {
                return 0;
            }

            return _enforceKindRestrictionsHook.Original(a1, a2);
        }

        private bool CalculateAndApplyOverallSpeedDetour(TimelineContainer* a1)
        {
            bool result = _calculateAndApplyOverallSpeedHook.Original(a1);

            if (EventManager.validCheck || IllusioVitae.InDebug())
            {
                if (!DalamudServices.clientState.IsGPosing)
                {
                    var actor = Actors.FirstOrDefault(x => x.GetCharacter() == a1->OwnerObject);

                    if (actor != null)
                    {
                        if (actor.animPaused)
                        {
                            a1->OverallSpeed = 0;
                        }
                        else
                        {
                            a1->OverallSpeed = actor.animSpeed;
                        }
                    }
                }
                else
                {
                    var actor = GPoseActors.FirstOrDefault(x => x.GetCharacter() == a1->OwnerObject);

                    if (actor != null)
                    {
                        if (actor.animPaused)
                        {
                            a1->OverallSpeed = 0;
                        }
                        else
                        {
                            a1->OverallSpeed = actor.animSpeed;
                        }
                    }
                }
            }

            return result;
        }

        private unsafe void SetSlotSpeedDetour(ActionTimelineSequencer* a1, int slot, float speed)
        {
            float finalSpeed = speed;

            if (EventManager.validCheck || IllusioVitae.InDebug())
            {
                var owner = Actors.FirstOrDefault(x => x.GetCharacter() == a1->Parent, null);

                if (DalamudServices.clientState.IsGPosing)
                {
                    owner = GPoseActors.FirstOrDefault(x => x.GetCharacter() == a1->Parent, null);
                }

                if (owner != null)
                {
                    if(owner.GetSlotSpeed(slot) != 1)
                    {
                        finalSpeed = owner.GetSlotSpeed(slot);
                    }
                }
            }

            _setSpeedSlotHook.Original(a1, slot, finalSpeed);
        }

        private void GetLocalPlayer()
        {
            playerActor = new(DalamudServices.clientState.LocalPlayer);

            DalamudServices.framework.Update += playerActor.Update;
        }

        private void GetGposePlayer()
        {
            if (DalamudServices.objectTables[201] == null)
            {
                return;
            }

            for(var i = 201; i < 241; i++)
            {
                if (DalamudServices.objectTables[i].Name.TextValue == playerActor.GetName())
                {
                    mainGposeActor = new(DalamudServices.objectTables[i]);
                    break;
                }
                    
            }

            DalamudServices.framework.Update += mainGposeActor.Update;
        }

        public XIVActor SetUpCustomActor(CharaFile data, string Name = "", bool spawnWithCompanion = false)
        {
            var actorID = CustomActors.Count;

            for(var i = 0; i < CustomActors.Count; i++)
            {
                if (CustomActors[i] == null)
                {
                    actorID = i;
                    break;
                }
            }

            CustomActor actor = new CustomActor(actorID, data, Name, spawnWithCompanion);

            XIVActor xivActor = null;


            if(DalamudServices.clientState.IsGPosing)
            {
                xivActor = SetUpGposeActor(actor.customGO, true);
            }
            else
            {
                xivActor = SetUpActor(actor.customGO, true);
            }

            if (!CustomActors.TryAdd(actorID, xivActor))
            {
                CustomActors[actorID] = xivActor;
            }

            return xivActor;
        }
        public void RemoveActors(ushort area = 0)
        {
            Actors.Clear();
            CustomActors.Clear();
        }

        public XIVActor SetUpActor(IGameObject? _actor, bool custom = false)
        {
            if (!custom)
            {
                if (_actor == playerActor.actorObject) return playerActor;

                foreach (var actor in Actors)
                {
                    if (actor.actorObject?.ObjectIndex == _actor?.ObjectIndex)
                    {
                        return actor;
                    }
                }
            }
           

            XIVActor actorData = new(_actor, custom);

            DalamudServices.framework.Update += actorData.Update;

            Actors.Add(actorData);

            return actorData;
        }

        public XIVActor SetUpGposeActor(IGameObject? _actor, bool custom = false)
        {
            if (!custom)
            {
                foreach (var actor in GPoseActors)
                {
                    if (actor.actorObject?.ObjectIndex == _actor?.ObjectIndex)
                    {
                        return actor;
                    }
                }
            }
            
            XIVActor actorData = new(_actor, custom);

            DalamudServices.framework.Update += actorData.Update;

            GPoseActors.Add(actorData);

            return actorData;
        }

        public bool TryGetActor(IGameObject inputActor, out XIVActor actor)
        {
            if (DalamudServices.clientState.IsGPosing)
            {
                foreach(var a in GPoseActors)
                {
                    if(a.actorObject?.ObjectIndex == inputActor?.ObjectIndex)
                    {
                        actor = a;
                        return true;
                    }
                }
            }
            else
            {
                foreach (var a in Actors)
                {
                    if (a.actorObject?.ObjectIndex == inputActor?.ObjectIndex)
                    {
                        actor = a;
                        return true;
                    }
                }
            }

            actor = null;
            return false;
        }


        public void RemoveActor(XIVActor _actorData)
        {
            if(_actorData == playerActor ||  _actorData == mainGposeActor) return;
            Actors.Remove(_actorData);
            GPoseActors.Remove(_actorData);

            if(_actorData.isCustom)
            {
                for(var i = 0; i < CustomActors.Count; i++)
                {
                    if (CustomActors[i] ==  _actorData)
                    {
                        CustomActors[i] = null;
                    }
                }
            }

            DalamudServices.framework.Update -= _actorData.Update;
        }

        public void Update(IFramework _framework)
        {
            if (DalamudServices.clientState.IsLoggedIn) 
            {
                if (playerActor == null) GetLocalPlayer();

                if (DalamudServices.clientState.IsGPosing)
                {
                    if(mainGposeActor == null) GetGposePlayer();


                    if (GPoseActors.Count == 0 && mainGposeActor != null)
                    {
                        IllusioDebug.Log("Adding Main GPose Actor", LogType.Debug);

                        GPoseActors.Add(mainGposeActor);
                    }
                }
                else
                {
                    if (mainGposeActor != null)
                    {
                        DalamudServices.framework.Update -= mainGposeActor.Update;
                        mainGposeActor = null;
                    }
                    
                    GPoseActors.Clear();
                }

                if (Actors.Count == 0)
                {
                    Actors.Add(playerActor);
                }
            }  
        }

        public void Dispose()
        {
            _enforceKindRestrictionsHook.Dispose();
            _calculateAndApplyOverallSpeedHook.Dispose();
            _setSpeedSlotHook.Dispose();
            _weaponAttachmentHook.Dispose();
            _weaponScaleHook.Dispose();
            _weaponStateHook.Dispose();

            DalamudServices.framework.Update -= Update;
            DalamudServices.clientState.Login -= GetLocalPlayer;

            if(!DalamudServices.clientState.IsLoggedIn) return;

            DalamudServices.framework.Update -= playerActor.Update;

            playerActor.animCancelled -= ModManager.Instance.ClearModData;


            foreach (var actorData in Actors)
            {
                DalamudServices.framework.Update -= actorData.Update;

                DalamudServices.framework.RunOnTick(() => { actorData.ResetApperance(); }, TimeSpan.FromSeconds(1));

                if (actorData.isCustom) actorData.DestroyActor();
            }

            foreach(var actorData in GPoseActors)
            {
                DalamudServices.framework.Update -= actorData.Update;

                if (actorData.isCustom) actorData.DestroyActor();
            }
        }
    }
}
