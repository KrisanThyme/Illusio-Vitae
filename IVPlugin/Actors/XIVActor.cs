using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using System;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System.Numerics;
using IVPlugin.Core.Extentions;
using IVPlugin.ActorData.Structs;
using StructsCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using CustomizeData = FFXIVClientStructs.FFXIV.Client.Game.Character.CustomizeData;
using WeaponModelId = FFXIVClientStructs.FFXIV.Client.Game.Character.WeaponModelId;
using EquipmentModelId = FFXIVClientStructs.FFXIV.Client.Game.Character.EquipmentModelId;
using EquipmentSlot = FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer.EquipmentSlot;
using WeaponSlot = FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer.WeaponSlot;
using IVPlugin.Services;
using ClientObjectManager = FFXIVClientStructs.FFXIV.Client.Game.Object.ClientObjectManager;
using IVPlugin.Core;
using IVPlugin.Actors;
using System.Linq;
using IVPlugin.Resources;
using IVPlugin.Resources.Sheets;
using IVPlugin.Mods;
using IVPlugin.Actors.Structs;
using IVPlugin.Log;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using IVPlugin.Actors.SkeletonData;
using IVPlugin.UI.Windows;
using System.Runtime.InteropServices.Marshalling;
using IVPlugin.Mods.Structs;
using IVPlugin.Json;
using IVPlugin.Core.Files;
using System.IO;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System.Diagnostics;
using FFXIVClientStructs.FFXIV.Client.Game;
using static FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CharacterBase;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Dalamud.Interface.FontIdentifier;
using FFXIVClientStructs.FFXIV.Common.Lua;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using IVPlugin.Gpose;

namespace IVPlugin.ActorData
{
    public class XIVActor
    {
        public bool isCustom { get; private set; } = false;
        public IGameObject? actorObject { get; private set; } = null;

        public bool forceCustomAppearance = false;
        public CustomLoadType customLoadType = CustomLoadType.AllButWeapon;

        public int currentBaseAnimation { get; private set; } = -1;
        public float localTime { get; private set; }
        public float duration { get; private set; }

        public int changedID = 0;
        public bool loopCurrentAnimation = false;
        public bool DoNotInterupt = false;

        private Vector3 recordedPos, previousPos;
        public float animScrub, animSpeed = 1;
        public int MaxAnimFrames = 0, CurrentFrame = 0;

        public CustomizeStruct defaultData, customData;
        private ShaderParams defaultParams, customParams;
        public GearPack defaultGear, customGear;
        private int initModelID = 0;
        private bool visorEnabled = false, visorLock = false, weaponEnabled = false, weaponLock = false, visorToggleLock = false, visorToggled = false;

        private float customWetness = 0;
        private ushort defaultVoice;

        private bool storeDataLock = false, CustomAnimLock = false, CustomDataLock = false, getSkeletonLock = false, firstTimeWeaponCheck = false, usingCustomAnim = false, loopingAnim = false, usingForcedAnim = false;
        public bool lockWetness = false, shaderDirty = false;

        private shaderLockType shaderLockParams = shaderLockType.None;
        public bool scaleLock = false;
        private float customScale;

        private bool RestoreOutfit = false, RestoreWeapons = false, RevertTime = false, RevertMonth = false, RevertSkybox = false;

        private IVTracklist currentTracklist = null;

        public bool validAnim { get; private set; } = false;
        public bool animPaused { get; private set; } = false;
        private float[] slotSpeeds = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1];

        public delegate void onRedrawComplete(bool CompleteRedraw);
        public static onRedrawComplete? redrawComplete = null;

        private byte currentClass;

        private short currentTransformID = 0;

        public delegate void onCancelledAnim();
        public onCancelledAnim? animCancelled = null;

        public IllusioSkeleton currentSkeleton = null;

        public bool positionDirty = false;

        public Transform overrideTransform = new();

        private bool ogTransformLock = false;
        public Transform originalTransform = new();

        private bool attemptingToGrabSkeleton = false;

        public XIVActor(IGameObject? _actorObject, bool customActor = false)
        {
            actorObject = _actorObject;
            isCustom = customActor;


            if (isCustom)
            {
                EventManager.GPoseChange += DestroyActor;
            }
        }

        public void DestroyActor(bool inGpose = false)
        {
            if (isCustom)
                FadeOutAndDestroy();
        }

        private void FadeOutAndDestroy()
        {
            if(GetTransparency() > 0)
            {
                SetTransparency(GetTransparency() - .1f);
                DalamudServices.framework.RunOnTick(FadeOutAndDestroy, TimeSpan.FromTicks(2));
            }
            else
            {
                unsafe
                {
                    var comInstance = ClientObjectManager.Instance();

                    var idx = comInstance->GetIndexByObject(actorObject.Base());

                    comInstance->DeleteObjectByIndex((ushort)idx, 0);
                }

                ActorManager.Instance.RemoveActor(this);
            }
        }

        public void FadeOut(int tickDelay = 2)
        {
            if (GetTransparency() > 0)
            {
                SetTransparency(GetTransparency() - .1f);
                DalamudServices.framework.RunOnTick(() => FadeOut(tickDelay), TimeSpan.FromTicks(tickDelay));
            }
        }

        public void FadeIn(int tickDelay = 2)
        {
            if(GetTransparency() < 1)
            {
                SetTransparency(GetTransparency() + .1f);
                DalamudServices.framework.RunOnTick(()=> FadeIn(tickDelay), TimeSpan.FromTicks(tickDelay));
            }
        }

        public string GetName()
        {
            if(actorObject == null)
            {
                return "";
            }

            return actorObject.Name.TextValue;
        }

        public int GetModelType()
        {
            unsafe
            {
                return GetCharacter()->CharacterData.ModelCharaId;
            }
        }

        public void SetModelType(uint id, bool reload = false)
        {
            unsafe
            {
                GetCharacter()->CharacterData.ModelCharaId = (int)id;
            }

            if(reload) Reload();
        }

        public unsafe bool IsLoaded(bool checkVisibility = false)
        {
            bool check = true;

            if(!DalamudServices.clientState.IsLoggedIn) check = false;

            try
            {
                if (actorObject.Base()->DrawObject == null) check = false;
                if(checkVisibility)
                    if(!actorObject.Base()->DrawObject->IsVisible) check = false;
            }
            catch(Exception e)
            {
                return false;
            }

            if (!check)
            {
                return false;
            }

            return true;
        }

        public unsafe byte GetClass()
        {
            return GetCharacter()->CharacterData.ClassJob;
        }

        public void Update(IFramework framework)
        {
            if (!DalamudServices.clientState.IsLoggedIn) return;

            if (IsLoaded())
            {
                if(actorObject.Position != previousPos)
                {
                    validAnim = false;
                }
                else
                {
                    validAnim = true;
                }

                CheckSlotChanges();

                if (GetClass() != currentClass)
                {
                    saveGear();
                    currentClass = GetClass();
                }

                if ((this == ActorManager.Instance.playerActor))
                {
                    if (!EventManager.validCheck)
                    {

                        if (GetModelType() != initModelID)
                        {
                            SetModelType((uint)initModelID, true);
                        }
                        
                        WeaponCheck();
                    }
                    else
                    {
                        firstTimeWeaponCheck = false;
                    }
                }

                if (forceCustomAppearance)
                {
                    if (customLoadType == CustomLoadType.AllButWeapon || customLoadType == CustomLoadType.EquipmentOnly)
                    {
                        CheckCustomAppearance();
                    }
                }

                if (!storeDataLock)
                {
                    defaultData = GetCustomizeData();
                    customData = GetCustomizeData();

                    if(GetShaderParams(out var shaderParams))
                    {
                        defaultParams = shaderParams;
                        customParams = shaderParams;
                    }

                    initModelID = GetModelType();

                    defaultVoice = GetVoice();

                    saveGear();

                    storeDataLock = true;

                    customWetness = GetWetness();

                    currentClass = GetClass();

                    customScale = GetActorScale();
                    

                    redrawComplete += (_) => { if (shaderDirty) ApplyShaderparams(customParams); if (scaleLock) SetActorScale(customScale); };
                }

                if (!CustomDataLock)
                {
                    if (forceCustomAppearance) SetCustomAppearance();
                    CustomDataLock = true;
                }

                ReadUpdateData();

                if (usingCustomAnim)
                {
                    if (CustomAnimLock)
                    {
                        if (loopCurrentAnimation && !loopingAnim)
                        {
                            PlayAnimation(changedID, true, resetAnim: false);
                        }

                        if (!loopCurrentAnimation && loopingAnim)
                        {
                            StopLoop();
                        }

                        if (DoNotInterupt && !usingForcedAnim)
                        {
                            PlayAnimation(changedID);
                        }

                        checkAnimCancel();
                    }
                }
                previousPos = actorObject.Position;

                if (lockWetness)
                {
                    SetWetness(customWetness);
                }
            }
            else
            {
                currentSkeleton = null;
                CustomDataLock = false;
                visorLock = false;
                weaponLock = false;

                if (!forceCustomAppearance)
                {
                    customParams = defaultParams;
                    customData = defaultData;
                    scaleLock = false;
                }
                
            }

            if (IsLoaded(true))
            {            
                unsafe
                {
                    var info = TerritoryInfo.Instance();

                    if(currentTransformID != GetCharacter()->CharacterData.TransformationId)
                    {
                        currentSkeleton = null;
                        currentTransformID = GetCharacter()->CharacterData.TransformationId;
                    }

                    if (info != null)
                    {
                        /*
                        if (!info->InSanctuary)
                        {
                            currentSkeleton = null;
                        }
                        */

                        //if (currentSkeleton == null && info->InSanctuary)
                        if (currentSkeleton == null && !attemptingToGrabSkeleton)
                        {
                            attemptingToGrabSkeleton = true;
                            GenerateSkeleton();
                        }
                    }
                }

                if (!visorLock)
                {
                    unsafe
                    {
                        visorEnabled = GetCharacter()->DrawData.IsHatHidden;
                    }

                    if(customData.FaceFeatures.HasFlag(FacialFeatures.LegacyTattoo))
                        visorLock = true;
                }
                else
                {
                    unsafe
                    {
                        if (GetCharacter()->DrawData.IsHatHidden != visorEnabled)
                        {
                            var update = GetCustomizeData();

                            update.FaceFeatures |= FacialFeatures.LegacyTattoo;
                            ApplyCustomize(update, false, true);
                            visorEnabled = GetCharacter()->DrawData.IsHatHidden;
                        }
                    }

                    if (!customData.FaceFeatures.HasFlag(FacialFeatures.LegacyTattoo))
                        visorLock = false;
                }

                if (!weaponLock)
                {
                    unsafe
                    {
                        weaponEnabled = GetCharacter()->DrawData.IsWeaponHidden;
                    }

                    if (customData.FaceFeatures.HasFlag(FacialFeatures.LegacyTattoo))
                        weaponLock = true;
                }
                else
                {
                    unsafe
                    {
                        if (GetCharacter()->DrawData.IsWeaponHidden != weaponEnabled)
                        {
                            var update = GetCustomizeData();

                            update.FaceFeatures |= FacialFeatures.LegacyTattoo;
                            ApplyCustomize(update, false, true);
                            weaponEnabled = GetCharacter()->DrawData.IsWeaponHidden;
                        }
                    }

                    if (!customData.FaceFeatures.HasFlag(FacialFeatures.LegacyTattoo))
                        weaponLock = false;
                }

                if (!visorToggleLock)
                {
                    unsafe
                    {
                        visorToggled = GetCharacter()->DrawData.IsVisorToggled;
                    }

                    if (customData.FaceFeatures.HasFlag(FacialFeatures.LegacyTattoo))
                        visorToggleLock = true;
                }
                else
                {
                    unsafe
                    {
                        if (GetCharacter()->DrawData.IsVisorToggled != visorToggled)
                        {
                            var update = GetCustomizeData();

                            update.FaceFeatures |= FacialFeatures.LegacyTattoo;
                            ApplyCustomize(update, false, true);
                            visorToggled = GetCharacter()->DrawData.IsVisorToggled;
                        }
                    }

                    if (!customData.FaceFeatures.HasFlag(FacialFeatures.LegacyTattoo))
                        visorToggleLock = false;
                }

            }
        }

        private unsafe void CheckSlotChanges()
        {
            if (RestoreOutfit || RestoreWeapons) return;

            if (GetWeaponSlot(WeaponSlot.MainHand).Value != customGear.mainWeapon.Value && GetWeaponSlot(WeaponSlot.MainHand).Value != defaultGear.mainWeapon.Value)
            {
                defaultGear.mainWeapon = GetWeaponSlot(WeaponSlot.MainHand);
                SkeletonOverlay.Hide();
                currentSkeleton = null;
            }
                
            if(GetWeaponSlot(WeaponSlot.OffHand).Value != customGear.offhand.Value && GetWeaponSlot(WeaponSlot.OffHand).Value != defaultGear.offhand.Value)
            {
                defaultGear.offhand = GetWeaponSlot(WeaponSlot.OffHand);
                SkeletonOverlay.Hide();
                currentSkeleton = null;
                
            }
                
            if (GetEquipmentSlot(EquipmentSlot.Head).Value != customGear.head.Value)
                defaultGear.head = GetEquipmentSlot(EquipmentSlot.Head);
            if (GetEquipmentSlot(EquipmentSlot.Body).Value != customGear.body.Value)
                defaultGear.body = GetEquipmentSlot(EquipmentSlot.Body);
            if (GetEquipmentSlot(EquipmentSlot.Hands).Value != customGear.hands.Value)
                defaultGear.hands = GetEquipmentSlot(EquipmentSlot.Hands);
            if (GetEquipmentSlot(EquipmentSlot.Legs).Value != customGear.legs.Value)
                defaultGear.legs = GetEquipmentSlot(EquipmentSlot.Legs);
            if (GetEquipmentSlot(EquipmentSlot.Feet).Value != customGear.feet.Value)
                defaultGear.feet = GetEquipmentSlot(EquipmentSlot.Feet);
            if (GetEquipmentSlot(EquipmentSlot.Ears).Value != customGear.ears.Value)
                defaultGear.ears = GetEquipmentSlot(EquipmentSlot.Ears);
            if (GetEquipmentSlot(EquipmentSlot.Neck).Value != customGear.neck.Value)
                defaultGear.neck = GetEquipmentSlot(EquipmentSlot.Neck);
            if (GetEquipmentSlot(EquipmentSlot.Wrists).Value != customGear.wrists.Value)
                defaultGear.wrists = GetEquipmentSlot(EquipmentSlot.Wrists);
            if (GetEquipmentSlot(EquipmentSlot.LFinger).Value != customGear.lRIng.Value)
                defaultGear.lRIng = GetEquipmentSlot(EquipmentSlot.LFinger);
            if (GetEquipmentSlot(EquipmentSlot.RFinger).Value != customGear.rRing.Value)
                defaultGear.rRing = GetEquipmentSlot(EquipmentSlot.RFinger);
            if (GetFacewear(0) != customGear.faceWear)
                defaultGear.faceWear = GetFacewear(0);
        }

        private unsafe void waitforanimUpdate(int maxticks, int currenttick)
        {
            if(changedID != currentBaseAnimation)
            {
                if(maxticks == currenttick)
                {
                    IllusioDebug.Log("Waiting for animation failed!", LogType.Warning);
                    recordedPos = actorObject.Position;
                    CustomAnimLock = true;
                }
                else
                {
                    DalamudServices.framework.RunOnTick(() => waitforanimUpdate(maxticks, ++currenttick), TimeSpan.FromTicks(1));
                }
            }
            else
            {
                recordedPos = actorObject.Position;
                CustomAnimLock = true;
            }
        }

        private unsafe void ReadUpdateData()
        {
            var charData = (ICharacter)actorObject;
            var charStruct = (StructsCharacter*)charData.Address;
            var drawObject = charStruct->GameObject.DrawObject;

            currentBaseAnimation = charStruct->Timeline.TimelineSequencer.GetSlotTimeline(0);

            if (currentBaseAnimation != 0)
            {
                if (drawObject->Object.GetObjectType() == ObjectType.CharacterBase)
                {
                    var charaBase = (CharacterBase*)actorObject.Base()->DrawObject;
                    var skeleton = charaBase->Skeleton;

                    for (int p = 0; p < skeleton->PartialSkeletonCount; ++p)
                    {
                        var partial = (PartialSkeleton*)((nint)skeleton->PartialSkeletons + (0x220 * p));

                        var animatedSkele = partial->GetHavokAnimatedSkeleton(0);
                        if (animatedSkele == null)
                            continue;

                        for (int c = 0; c < animatedSkele->AnimationControls.Length; ++c)
                        {
                            var control = animatedSkele->AnimationControls[c].Value;
                            if (control == null)
                                continue;

                            var binding = control->hkaAnimationControl.Binding;
                            if (binding.ptr == null)
                                continue;

                            var anim = binding.ptr->Animation.ptr;
                            if (anim == null)
                                continue;

                            localTime = control->hkaAnimationControl.LocalTime;

                            CurrentFrame = (int)MathF.Round(localTime * 30);

                            duration = anim->Duration;

                            MaxAnimFrames = (int)MathF.Round(duration * 30);

                            if (CustomAnimLock && currentTracklist != null)
                            {
                                var tracks = currentTracklist.GetTracksForFrame(CurrentFrame);

                                foreach(var track in tracks)
                                {
                                    PlayTrack(track);
                                }
                            }

                            if (animPaused)
                            {
                                control->hkaAnimationControl.LocalTime = animScrub;
                            }
                            else
                            {
                                animScrub = localTime;

                                if(animSpeed != 1)
                                    control->PlaybackSpeed = animSpeed;
                            }
                        }
                    }
                }
            }
        }

        public void SetCustomAppearance()
        {
            bool applyGear = false;
            bool applyWeapon = false;
            bool applyCharacter = false;


            switch (customLoadType)
            {
                case CustomLoadType.AllButWeapon:
                    applyGear = true;
                    applyCharacter = true;
                    break;
                case CustomLoadType.CharacterOnly:
                    applyCharacter = true;
                    break;
                case CustomLoadType.EquipmentOnly:
                    applyGear = true;
                    break;
            }

            if (applyGear)
            {
                SetEquipmentSlot(EquipmentSlot.Head, customGear.head);
                SetEquipmentSlot(EquipmentSlot.Body, customGear.body);
                SetEquipmentSlot(EquipmentSlot.Hands, customGear.hands);
                SetEquipmentSlot(EquipmentSlot.Legs, customGear.legs);
                SetEquipmentSlot(EquipmentSlot.Feet, customGear.feet);
                SetEquipmentSlot(EquipmentSlot.Ears, customGear.ears);
                SetEquipmentSlot(EquipmentSlot.Neck, customGear.neck);
                SetEquipmentSlot(EquipmentSlot.Wrists, customGear.wrists);
                SetEquipmentSlot(EquipmentSlot.LFinger, customGear.lRIng);
                SetEquipmentSlot(EquipmentSlot.RFinger, customGear.rRing);
                SetFacewear(0 ,customGear.faceWear);
            }

            if (applyCharacter)
            {
                ApplyCustomize(customData, true);
            }
                 
        }
        
        public void CheckCustomAppearance()
        {
            if(GetEquipmentSlot(EquipmentSlot.Head).Value != customGear.head.Value)
                SetEquipmentSlot(EquipmentSlot.Head, customGear.head);
            if (GetEquipmentSlot(EquipmentSlot.Body).Value != customGear.body.Value)
                SetEquipmentSlot(EquipmentSlot.Body, customGear.body);
            if (GetEquipmentSlot(EquipmentSlot.Hands).Value != customGear.hands.Value)
                SetEquipmentSlot(EquipmentSlot.Hands, customGear.hands);
            if (GetEquipmentSlot(EquipmentSlot.Legs). Value != customGear.legs.Value)
                SetEquipmentSlot(EquipmentSlot.Legs, customGear.legs);
            if (GetEquipmentSlot(EquipmentSlot.Feet).Value != customGear.feet.Value)
                SetEquipmentSlot(EquipmentSlot.Feet, customGear.feet);
            if (GetEquipmentSlot(EquipmentSlot.Ears).Value != customGear.ears.Value)
                SetEquipmentSlot(EquipmentSlot.Ears, customGear.ears);
            if (GetEquipmentSlot(EquipmentSlot.Neck).Value != customGear.neck.Value)
                SetEquipmentSlot(EquipmentSlot.Neck, customGear.neck);
            if (GetEquipmentSlot(EquipmentSlot.Wrists).Value != customGear.wrists.Value)
                SetEquipmentSlot(EquipmentSlot.Wrists, customGear.wrists);
            if (GetEquipmentSlot(EquipmentSlot.LFinger).Value != customGear.lRIng.Value)
                SetEquipmentSlot(EquipmentSlot.LFinger, customGear.lRIng);
            if (GetEquipmentSlot(EquipmentSlot.RFinger).Value != customGear.rRing.Value)
                SetEquipmentSlot(EquipmentSlot.RFinger, customGear.rRing);
            if(GetFacewear(0) != customGear.faceWear)
                SetFacewear(0, customGear.faceWear);
        }
        
        public void WeaponCheck()
        {
            if (GetWeaponSlot(WeaponSlot.MainHand).Value != defaultGear.mainWeapon.Value && !GetWeaponSlot(WeaponSlot.MainHand).ValidWeapon(ActorEquipSlot.MainHand, GetClass()))
            {
                SetWeaponSlot(WeaponSlot.MainHand, defaultGear.mainWeapon, true);
            }
                
            if(GetWeaponSlot(WeaponSlot.OffHand).Value != 0)
            {
                if (GetWeaponSlot(WeaponSlot.OffHand).Value != defaultGear.offhand.Value && !GetWeaponSlot(WeaponSlot.OffHand).ValidWeapon(ActorEquipSlot.OffHand, GetClass()))
                {
                    SetWeaponSlot(WeaponSlot.OffHand, defaultGear.offhand, true);
                }
            }
        }

        public unsafe companionSlot GetCompanion()
        {
            if(GetCharacter()->CompanionObject != null)
            {
                if(GetCharacter()->OrnamentData.OrnamentObject != null)
                {
                    return new(companionType.Ornament, (uint)GetCharacter()->OrnamentData.OrnamentId);
                }
                else if (GetCharacter()->Mount.MountObject != null)
                {
                    return new(companionType.Mount, (uint)GetCharacter()->Mount.MountId);
                }
                else if (GetCharacter()->CompanionData.CompanionObject != null)
                {
                    return new(companionType.Minion, (uint)GetCharacter()->CompanionObject->Character.GameObject.BaseId);
                }
            }

            return new(0,0);
        }

        public unsafe void ClearCompanion()
        {
            switch (GetCompanion().type)
            {
                case companionType.Minion:
                    GetCharacter()->CompanionData.SetupCompanion(0, 0);
                    break;
                case companionType.Mount:
                    GetCharacter()->Mount.CreateAndSetupMount(0, 0, 0, 0, 0, 0, 0);
                    break;
                case companionType.Ornament:
                    GetCharacter()->OrnamentData.SetupOrnament(0, 0);
                    break;
            }
        }

        public unsafe ushort GetFacewear(byte slot)
        {
            if(0 == slot)
                return GetCharacter()->DrawData.GlassesIds.ToArray()[0];

            if(1 == slot)
                return GetCharacter()->DrawData.GlassesIds.ToArray()[1];

            return 0;
        }

        public unsafe void SetFacewear(byte slot,ushort data)
        {
            var drawData = (ExtendedDrawDataContainer*)&GetCharacter()->DrawData;

            customGear.faceWear = data;
            ActorManager.ChangeGlasses(drawData, slot, data);
        }

        public unsafe bool MountCheck()
        {
            return GetCharacter()->IsMounted();
        }

        public unsafe void SetCompanion(companionSlot companion)
        {
            ClearCompanion();

            switch(companion.type)
            {
                case companionType.Minion:
                    GetCharacter()->CompanionData.SetupCompanion((short)companion.id, 0);
                    break;
                case companionType.Mount:
                    GetCharacter()->Mount.CreateAndSetupMount((short)companion.id, 0, 0, 0, 0, 0, 0);
                    break;
                case companionType.Ornament:
                    GetCharacter()->OrnamentData.SetupOrnament((short)companion.id, 0);
                    break;
            }

            WaitForMinionDraw();
        }

        public unsafe Transform GetTransform()
        {
            var native = actorObject.Base();
            var drawObject = native->DrawObject;
            if (drawObject != null)
            {
                return *(Transform*)(&drawObject->Object.Position);
            }
            else
            {
                return new Transform()
                {
                    Position = native->Position
                };
            };
        }

        public unsafe void SetOverrideTransform(Transform overrideTransform)
        {
            var worldTransform = GetTransform();

            if (!ogTransformLock)
            {
                originalTransform = worldTransform;
                ogTransformLock = true;
            }

            this.overrideTransform = overrideTransform;
            positionDirty = true;


            SetTransform();
        }

        public unsafe void SetTransform()
        {
            var drawObject = actorObject.Base()->DrawObject;
            if (drawObject != null)
            {
                drawObject->Object.Position = overrideTransform.Position;
                drawObject->Object.Rotation = overrideTransform.Rotation;
                drawObject->Object.Scale = overrideTransform.Scale;

                actorObject.Base()->Position = overrideTransform.Position;
            }
        }

        public void ResetTransform()
        {
            if (positionDirty) positionDirty = false;
        }

        public unsafe float GetActorScale()
        {
            if (actorObject.Base()->DrawObject == null) return 1;

            var x = *(ExtendedCharStruct*)actorObject.Base()->DrawObject;

            return x.ScaleFactor2;
        }

        public unsafe void SetActorScale(float value)
        {
            var x = (ExtendedCharStruct*)actorObject.Base()->DrawObject;

            if(x->ScaleFactor2 != value)
            {
                x->ScaleFactor2 = value;

                customScale = value;

                scaleLock = true;
            }
        }

        public void SetSlotSpeed(int slot, float speed)
        {
            slotSpeeds[(int)slot] = speed;
        }

        public float GetSlotSpeed(int slot)
        {
            return slotSpeeds[(int)slot];
        }

        public unsafe string GetSlotAnimation(int slot)
        {
            var result = GetCharacter()->Timeline.TimelineSequencer.GetSlotTimeline((uint)slot);

            var slotName = GameResourceManager.Instance.ActionTimelines[result].Key.RawString;

            if (string.IsNullOrEmpty(slotName))
            {
                return "None";
            }
            else
            {
                return slotName;
            }
        }

        public unsafe void GenerateSkeleton(bool _ = true)
        {
            if (!IsLoaded(true))
            {
                attemptingToGrabSkeleton = false;
                return;
            }
                

            DalamudServices.framework.RunOnTick(() =>
            {
                var charObject = (CharacterBase*)actorObject.Base()->DrawObject;

                if(charObject != null)
                {
                    Skeleton* mainSkeleton = null;
                    Skeleton* mhSkeleton = null;
                    Skeleton* ohSkeleton = null;

                    if (charObject->Skeleton->PartialSkeletons[0].GetHavokPose(0) != null)
                        mainSkeleton = charObject->Skeleton;

                    if(TryGetWeapon(WeaponSlot.MainHand, out var mainhand))
                    {
                        mhSkeleton = mainhand->drawData->CharacterBase.Skeleton;
                    }

                    if(TryGetWeapon(WeaponSlot.OffHand, out var offhand))
                    {
                        ohSkeleton = offhand->drawData->CharacterBase.Skeleton;
                    }

                    currentSkeleton = new(mainSkeleton, mhSkeleton, ohSkeleton);
                }

                attemptingToGrabSkeleton = false;
            },TimeSpan.FromSeconds(.25));
        }

        public void saveGear(bool CustomOnly = false, bool defaultOnly = false)
        {
            GearPack data = new GearPack()
            {
                mainWeapon = GetWeaponSlot(WeaponSlot.MainHand),
                offhand = GetWeaponSlot(WeaponSlot.OffHand),
                head = GetEquipmentSlot(EquipmentSlot.Head),
                body = GetEquipmentSlot(EquipmentSlot.Body),
                hands = GetEquipmentSlot(EquipmentSlot.Hands),
                legs = GetEquipmentSlot(EquipmentSlot.Legs),
                feet = GetEquipmentSlot(EquipmentSlot.Feet),
                ears = GetEquipmentSlot(EquipmentSlot.Ears),
                neck = GetEquipmentSlot(EquipmentSlot.Neck),
                wrists = GetEquipmentSlot(EquipmentSlot.Wrists),
                lRIng = GetEquipmentSlot(EquipmentSlot.LFinger),
                rRing = GetEquipmentSlot(EquipmentSlot.RFinger),
                faceWear = GetFacewear(0)
            };

            if (defaultOnly) { defaultGear = data; return; }

            if (CustomOnly) { customGear = data; return; }


            defaultGear = data;
            customGear = data;
        }

        public unsafe StructsCharacter* GetCharacter()
        {
            var actorChar = (ICharacter)actorObject;
            return actorChar.Base();
        }

        public unsafe ExtendedCharStruct* GetCharacterBase()
        {
            return (ExtendedCharStruct*)actorObject.Base()->DrawObject;
        }

        public unsafe bool TryGetHuman(out ExtendedHumanStruct* humanptr)
        {
            humanptr = null;
            if (!IsLoaded()) return false;

            if(GetCharacterBase()->CharacterBase.GetModelType() == CharacterBase.ModelType.Human)
            {
                humanptr = (ExtendedHumanStruct*)GetCharacterBase();
                return true;
            }
            else
            {
                return false;
            }
        }

        public unsafe CustomizeStruct GetCustomizeData()
        {
            return *(CustomizeStruct*)&GetCharacter()->DrawData.CustomizeData;
        }

        public unsafe void SetLoopAnimation(int animID, IVTracklist tracklist = null)
        {
            CustomAnimLock = false;
            loopCurrentAnimation = true;
            loopingAnim = true;
            PlayAnimation(animID, true, tracklist);
        }

        public unsafe void LoadBNPC(uint npcID)
        {
            GetCharacter()->CharacterSetup.SetupBNpc(npcID);
        }

        public unsafe void PlayFacialAniamtion(int animID)
        {
            if (validAnim || isCustom)
            {
                IllusioDebug.Log($"Playing animation for actor {GetName()}", LogType.Debug);

                var charStruct = GetCharacter();

                charStruct->Timeline.SetLipsOverrideTimeline((ushort)animID);
            }
        }

        public unsafe void PlayExpression(int animID)
        {
            GetCharacter()->Timeline.TimelineSequencer.PlayTimeline((ushort)animID);
        }

        public unsafe void PlayAnimation(int AnimOvveride = 0, bool loop = false, IVTracklist tracklist = null, bool resetAnim = true)
        {
            var charStruct = GetCharacter();

            if (AnimOvveride == 0 && !DoNotInterupt)
            {
                IllusioDebug.Log($"Resetting animation for actor {GetName()}", LogType.Debug);

                charStruct->Timeline.BaseOverride = 0;

                if(ModManager.Instance.activeMod)
                    charStruct->Timeline.TimelineSequencer.PlayTimeline(3);
                else
                    charStruct->Timeline.TimelineSequencer.PlayTimeline(0);


                usingCustomAnim = false;
                return;
            }

            if (!DoNotInterupt)
            {
                if ((validAnim || isCustom))
                {
                    if (AnimOvveride != -1 && !GameResourceManager.Instance.BlendEmotes.Any(x => x.Value.RowId == AnimOvveride))
                        changedID = AnimOvveride;

                    if (loop)
                    {
                        IllusioDebug.Log($"Looping animation for actor {GetName()}", LogType.Debug);

                        loopingAnim = true;

                        if(resetAnim)
                            charStruct->Timeline.TimelineSequencer.PlayTimeline(3);

                        DalamudServices.framework.RunOnTick(() => 
                        { 
                            charStruct->Timeline.BaseOverride = (ushort)AnimOvveride; usingCustomAnim = true;

                            if(!GameResourceManager.Instance.BlendEmotes.Any(x => x.Value.RowId == AnimOvveride))
                            {
                                currentTracklist = tracklist;
                                waitforanimUpdate(50, 0);
                            }
                                

                        }, TimeSpan.FromSeconds(.025));

                    }
                    else
                    {

                        IllusioDebug.Log($"Playing animation for actor {GetName()}", LogType.Debug);

                        if (charStruct->Timeline.BaseOverride != 0)
                            charStruct->Timeline.BaseOverride = 0;

                        recordedPos = actorObject.Position;

                        DalamudServices.framework.RunOnTick(() => 
                        { 
                            charStruct->Timeline.TimelineSequencer.PlayTimeline((ushort)AnimOvveride); 
                            
                            usingCustomAnim = true;

                            if (!GameResourceManager.Instance.BlendEmotes.Any(x => x.Value.RowId == AnimOvveride))
                            {
                                currentTracklist = tracklist;
                                waitforanimUpdate(50, 0);
                            }
                                
                        }, TimeSpan.FromSeconds(.025));
                    }

                    
                }
                else
                {
                    IllusioDebug.Log($"Unable to play custom animation for {GetName()}", LogType.Warning);
                }
            }
            else
            {
                if(changedID != 0)
                {
                    IllusioDebug.Log($"Forcing animation for actor {GetName()}", LogType.Debug);

                    charStruct->Timeline.BaseOverride = (ushort)changedID;

                    usingForcedAnim = true;

                    usingCustomAnim = true;
                }
            }
            
        }

        public unsafe void PlayTrack(IVTrack track)
        {
            switch(track.Type)
            {
                case TrackType.Expression:
                    if (track.Value == null) return;
                    PlayExpression((int)track.Value);
                    break;
                case TrackType.Transparency:
                    if (track.Value == null) return;
                    SetTransparency((float)track.Value);
                    break;
                case TrackType.FadeIn:
                    if (track.Value == null) return;
                    FadeIn((int)track.Value);
                    break;
                case TrackType.FadeOut:
                    if (track.Value == null) return;
                    FadeOut((int)track.Value);
                    break;
                case TrackType.Outfit:
                    try
                    {
                        var filePath = Path.Combine(ModManager.Instance.ActiveModPath, track.sValue);

                        if (!File.Exists(filePath)) return;

                        RestoreOutfit = true;
                        JsonHandler.Deserialize<CharaFile>(File.ReadAllText(filePath)).Apply(this, (CharaFile.SaveModes.EquipmentGear | CharaFile.SaveModes.EquipmentAccessories));

                    }catch(Exception e)
                    {
                        //TODO
                    }
                    break;
                case TrackType.HideWeapons:
                    RestoreWeapons = true;
                    SetWeaponSlot(WeaponSlot.MainHand, new() { Id = 301, Variant = 1, Type = 31 }, temp: true);
                    SetWeaponSlot(WeaponSlot.OffHand, new() { Id = 351, Variant = 1, Type = 31 }, temp: true);
                    break;
                case TrackType.ChangeSkybox:
                    if (track.Value == null) return;
                    WorldManager.Instance.CurrentSky = (uint)track.Value;
                    RevertSkybox = true;
                    break;
                case TrackType.ChangeTime:
                    if (track.Value == null) return;
                    WorldManager.Instance.IsTimeFrozen = true;
                    WorldManager.Instance.EorzeaTime = (uint)track.Value;
                    RevertTime = true;
                    break;
                case TrackType.ChangeMonth:
                    if (track.Value == null) return;
                    WorldManager.Instance.IsTimeFrozen = true;
                    WorldManager.Instance.DayOfMonth = (int)track.Value;
                    RevertMonth = true;
                    break;
            }
        }

        public unsafe void PauseAnimation()
        {
            animPaused = !animPaused;
        }

        private unsafe void checkAnimCancel()
        {
            var selectedCharacter = (ICharacter)actorObject;

            if((recordedPos != actorObject?.Position || changedID != currentBaseAnimation) && !DoNotInterupt)
            {
                IllusioDebug.Log("Animation Cancelled", LogType.Debug);

                if (RevertSkybox)
                {
                    WorldManager.Instance.resetSky();
                    RevertSkybox = false;
                }

                if (RevertTime || RevertMonth)
                {
                    WorldManager.Instance.IsTimeFrozen = false;
                    
                    RevertTime = false;
                    RevertMonth = false;
                }

                PlayAnimation(0);

                loopCurrentAnimation = false;
                loopingAnim = false;
                CustomAnimLock = false;
                animCancelled?.Invoke();
                usingCustomAnim = false;

                
                return;
            }
        }

        public void RestoreOutfitFromAnim()
        {
            if (RestoreOutfit)
            {
                IllusioDebug.Log("Restoring Outfit", LogType.Debug);

                SetEquipmentSlot(EquipmentSlot.Head, customGear.head);
                SetEquipmentSlot(EquipmentSlot.Body, customGear.body);
                SetEquipmentSlot(EquipmentSlot.Hands, customGear.hands);
                SetEquipmentSlot(EquipmentSlot.Legs, customGear.legs);
                SetEquipmentSlot(EquipmentSlot.Feet, customGear.feet);
                SetEquipmentSlot(EquipmentSlot.Ears, customGear.ears);
                SetEquipmentSlot(EquipmentSlot.Neck, customGear.neck);
                SetEquipmentSlot(EquipmentSlot.Wrists, customGear.wrists);
                SetEquipmentSlot(EquipmentSlot.LFinger, customGear.lRIng);
                SetEquipmentSlot(EquipmentSlot.RFinger, customGear.rRing);
                SetFacewear(0, customGear.faceWear);

                RestoreOutfit = false;
            }

            if(RestoreWeapons)
            {
                SetWeaponSlot(WeaponSlot.MainHand, customGear.mainWeapon);
                SetWeaponSlot(WeaponSlot.OffHand, customGear.offhand);

                RestoreWeapons = false;
            }
        }

        private unsafe void StopLoop()
        {
            PlayAnimation(0);
            loopingAnim = false;
            loopCurrentAnimation = false;
        }

        public unsafe WeaponModelId GetWeaponSlot(WeaponSlot slot) {return GetCharacter()->DrawData.Weapon(slot).ModelId;}

        public unsafe EquipmentModelId GetEquipmentSlot(EquipmentSlot slot)
        {
            var data = GetCharacter()->DrawData;

            switch(slot)
            {
                case EquipmentSlot.Head:
                    return data.Equipment(EquipmentSlot.Head);
                case EquipmentSlot.Body:
                    return data.Equipment(EquipmentSlot.Body);
                case EquipmentSlot.Hands:
                    return data.Equipment(EquipmentSlot.Hands);
                case EquipmentSlot.Legs:
                    return data.Equipment(EquipmentSlot.Legs);
                case EquipmentSlot.Feet:
                    return data.Equipment(EquipmentSlot.Feet);
                case EquipmentSlot.Ears:
                    return data.Equipment(EquipmentSlot.Ears);
                case EquipmentSlot.Neck:
                    return data.Equipment(EquipmentSlot.Neck);
                case EquipmentSlot.Wrists:
                    return data.Equipment(EquipmentSlot.Wrists);
                case EquipmentSlot.RFinger:
                    return data.Equipment(EquipmentSlot.RFinger);
                case EquipmentSlot.LFinger:
                    return data.Equipment(EquipmentSlot.LFinger);
                default:
                    return new();
            }
        }
        
        public unsafe void SetWeaponSlot(WeaponSlot slot, WeaponModelId modelData, bool reload = false, bool temp = false)
        {
            GetCharacter()->DrawData.LoadWeapon(slot, modelData, 0, 0, 0, 0);

            if (reload) Reload();

            if (!temp)
            {
                if (slot == WeaponSlot.MainHand) customGear.mainWeapon = modelData;
                if (slot == WeaponSlot.OffHand) customGear.offhand = modelData;
            }

            currentSkeleton = null;
        }

        public unsafe void SetWeaponSlot(ActorEquipSlot slot, WeaponModelId modelData, bool reload = false)
        {
            WeaponSlot realSlot = WeaponSlot.MainHand;

            realSlot = slot.HasFlag(ActorEquipSlot.MainHand) ? realSlot : WeaponSlot.OffHand;

            GetCharacter()->DrawData.LoadWeapon(realSlot, modelData, 0, 0, 0, 0);

            if(reload) Reload();

            if (realSlot == WeaponSlot.MainHand) customGear.mainWeapon = modelData;
            if (realSlot == WeaponSlot.OffHand) customGear.offhand = modelData;

            currentSkeleton = null;
        }

        public unsafe void SetEquipmentSlot(EquipmentSlot slot, EquipmentModelId modelData)
        {
            GetCharacter()->DrawData.LoadEquipment(slot, &modelData, false);

            if (!RestoreOutfit)
            {
                if (slot == EquipmentSlot.Head) customGear.head = modelData;
                if (slot == EquipmentSlot.Body) customGear.body = modelData;
                if (slot == EquipmentSlot.Hands) customGear.hands = modelData;
                if (slot == EquipmentSlot.Legs) customGear.legs = modelData;
                if (slot == EquipmentSlot.Feet) customGear.feet = modelData;
                if (slot == EquipmentSlot.Ears) customGear.ears = modelData;
                if (slot == EquipmentSlot.Neck) customGear.neck = modelData;
                if (slot == EquipmentSlot.Wrists) customGear.wrists = modelData;
                if (slot == EquipmentSlot.LFinger) customGear.lRIng = modelData;
                if (slot == EquipmentSlot.RFinger) customGear.rRing = modelData;
            }

            currentSkeleton = null;
        }

        public unsafe void SetEquipmentSlot(ActorEquipSlot slot, EquipmentModelId modelData)
        {
            EquipmentSlot realSlot = EquipmentSlot.Neck;

            switch (slot)
            {
                case ActorEquipSlot.Head:
                    realSlot = EquipmentSlot.Head;
                    break;
                case ActorEquipSlot.Body:
                    realSlot = EquipmentSlot.Body;
                    break;
                case ActorEquipSlot.Hands:
                    realSlot = EquipmentSlot.Hands;
                    break;
                case ActorEquipSlot.Legs:
                    realSlot = EquipmentSlot.Legs;
                    break;
                case ActorEquipSlot.Feet:
                    realSlot = EquipmentSlot.Feet;
                    break;
                case ActorEquipSlot.Ears:
                    realSlot = EquipmentSlot.Ears;
                    break;
                case ActorEquipSlot.Neck:
                    realSlot = EquipmentSlot.Neck;
                    break;
                case ActorEquipSlot.Wrists:
                    realSlot = EquipmentSlot.Wrists;
                    break;
                case ActorEquipSlot.LeftRing:
                    realSlot = EquipmentSlot.LFinger;
                    break;
                case ActorEquipSlot.RightRing:
                    realSlot = EquipmentSlot.RFinger;
                    break;
                case ActorEquipSlot.All:
                    break;
                case ActorEquipSlot.AllButWeapons:
                    break;
                case ActorEquipSlot.Armor:
                    break;
                case ActorEquipSlot.Prop:
                    break;
                case ActorEquipSlot.Accessories:
                    break;
            }


            GetCharacter()->DrawData.LoadEquipment(realSlot, &modelData, false);

            if (!RestoreOutfit)
            {
                if (realSlot == EquipmentSlot.Head) customGear.head = modelData;
                if (realSlot == EquipmentSlot.Body) customGear.body = modelData;
                if (realSlot == EquipmentSlot.Hands) customGear.hands = modelData;
                if (realSlot == EquipmentSlot.Legs) customGear.legs = modelData;
                if (realSlot == EquipmentSlot.Feet) customGear.feet = modelData;
                if (realSlot == EquipmentSlot.Ears) customGear.ears = modelData;
                if (realSlot == EquipmentSlot.Neck) customGear.neck = modelData;
                if (realSlot == EquipmentSlot.Wrists) customGear.wrists = modelData;
                if (realSlot == EquipmentSlot.LFinger) customGear.lRIng = modelData;
                if (realSlot == EquipmentSlot.RFinger) customGear.rRing = modelData;
            }
            

            currentSkeleton = null;
        }

        public unsafe AnimCodes GetRaceAnimCode()
        {
            if(TryGetHuman(out var humanptr))
            {
                return (AnimCodes)humanptr->AnimPath;
            }

            return 0;
        }

        public unsafe void SetRaceAnimCode(AnimCodes code)
        {
            if (TryGetHuman(out var humanptr))
            {
                humanptr->AnimPath = (short)code;
            }
        }

        public unsafe void SetHeight(byte value)
        {
            GetCharacter()->DrawData.CustomizeData.Height = value;
        }
        
        public unsafe float GetTransparency()
        {
            return GetCharacter()->Alpha;
        }

        public unsafe void SetTransparency(float value)
        {
            GetCharacter()->Alpha = value;
        }

        public unsafe ushort GetVoice()
        {
            return GetCharacter()->Vfx.VoiceId;
        }

        public unsafe void SetVoice(ushort voice)
        {
            GetCharacter()->Vfx.VoiceId = voice;
        }

        public unsafe float GetWetness()
        {
            if(!IsLoaded()) return 0f;

            return GetCharacterBase()->Wetness;
        }

        public unsafe void SetWetness(float value)
        {
            customWetness = value;
            GetCharacterBase()->Wetness = value;

        }

        public unsafe Vector4 GetTint()
        {
            var character = (ExtendedCharStruct*)actorObject.Base()->DrawObject;

            if (character != null)
                return character->Tint;
            else
                return Vector4.One;
        }

        public unsafe Vector4 GetWeaponTint(WeaponSlot slot)
        {
            fixed (FFXIVClientStructs.FFXIV.Client.Game.Character.DrawObjectData* weaponData = &GetCharacter()->DrawData.Weapon(slot))
            {
                var weapon = (ExtendedCharStruct*)weaponData->DrawObject;

                if (weapon != null)
                    return weapon->Tint;
                else
                    return Vector4.One;
            }
        }

        public unsafe void SetWeaponTint(WeaponSlot slot, Vector4 newTint)
        {
            fixed(FFXIVClientStructs.FFXIV.Client.Game.Character.DrawObjectData* weaponData = &GetCharacter()->DrawData.Weapon(slot))
            {
                var weapon = (ExtendedCharStruct*)weaponData->DrawObject;

                if(weapon != null)
                    weapon->Tint = newTint;
            }
        }

        public unsafe bool TryGetWeapon(WeaponSlot slot, out ExtendedWeaponData* weaponBase)
        {
            fixed (DrawObjectData* weaponData = &GetCharacter()->DrawData.Weapon(slot))
            {
                if (weaponData->DrawObject == null)
                {
                    weaponBase = null;
                    return false;
                }
                else
                {
                    weaponBase = (ExtendedWeaponData*)weaponData;
                    return true;
                }
            }
        }

        public unsafe bool GetWeaponVisiblity(WeaponSlot slot)
        {
            fixed (DrawObjectData* weaponData = &GetCharacter()->DrawData.Weapon(slot))
            {
                if (weaponData->DrawObject == null) return false;
                return weaponData->DrawObject->IsVisible;
            }
        }

        public unsafe void SetWeaponVisibility(WeaponSlot slot, bool visible)
        {
            if(TryGetWeapon(slot, out ExtendedWeaponData* weaponData))
            {
                weaponData->WeaponState = 4;
            }
        }

        public unsafe void SetTint(Vector4 newTint)
        {
            var character = (ExtendedCharStruct*)actorObject.Base()->DrawObject;

            character->Tint = newTint;
        }

        public void UpdateShaderLocks(shaderLockType type, bool enable)
        {
            if (shaderLockType.None == type) 
            {
                shaderLockParams = shaderLockType.None;
                return;
            }

            if (!enable)
            {
                shaderLockParams &= ~type;
            }
            else
            {
                shaderLockParams |= type;
            }
        }

        public unsafe bool GetShaderParams(out ShaderParams shader)
        {
            if (!IsLoaded()) 
            {
                shader = new();
                return false;
            }

            if (TryGetHuman(out var human))
            {
                var x = *human->Shaders;

                shader = *x.Params;

                return true;
            }
            else
            {
                shader = new();
                return false;
            }
        }

        public unsafe void ApplyShaderparams(ShaderParams shader)
        {
            IllusioDebug.Log($"Applying Shaders for {GetName()}", LogType.Debug);

            var charaBase = (ExtendedCharStruct*)GetCharacter()->GameObject.DrawObject;

            if (charaBase == null) return;

            if (charaBase->CharacterBase.GetModelType() != CharacterBase.ModelType.Human) return;

            var human = (ExtendedHumanStruct*)charaBase;

            if(GetShaderParams(out var realShaders))
            {
                if (human->Shaders->Params->FeatureColor != shader.FeatureColor && shaderLockParams.HasFlag(shaderLockType.FeatureColor))
                    human->Shaders->Params->FeatureColor = shader.FeatureColor;
                else
                    shader.FeatureColor = realShaders.FeatureColor;

                if (human->Shaders->Params->HairColor != shader.HairColor && shaderLockParams.HasFlag(shaderLockType.HairColor))
                    human->Shaders->Params->HairColor = shader.HairColor;
                else
                    shader.HairColor = realShaders.HairColor;

                if (human->Shaders->Params->HairGloss != shader.HairGloss && shaderLockParams.HasFlag(shaderLockType.HairColor))
                    human->Shaders->Params->HairGloss = shader.HairGloss;
                else
                    shader.HairGloss = realShaders.HairGloss;

                if (human->Shaders->Params->HairHighlight != shader.HairHighlight && shaderLockParams.HasFlag(shaderLockType.HighlightColor))
                    human->Shaders->Params->HairHighlight = shader.HairHighlight;
                else
                    shader.FeatureColor = realShaders.FeatureColor;

                if (human->Shaders->Params->LeftEyeColor != shader.LeftEyeColor && shaderLockParams.HasFlag(shaderLockType.LEyeColor))
                    human->Shaders->Params->LeftEyeColor = shader.LeftEyeColor;
                else
                    shader.LeftEyeColor = realShaders.LeftEyeColor;

                if (human->Shaders->Params->RightEyeColor != shader.RightEyeColor && shaderLockParams.HasFlag(shaderLockType.REyeColor))
                    human->Shaders->Params->RightEyeColor = shader.RightEyeColor;
                else
                    shader.RightEyeColor = realShaders.RightEyeColor;

                if (human->Shaders->Params->SkinColor != shader.SkinColor && shaderLockParams.HasFlag(shaderLockType.SkinColor))
                    human->Shaders->Params->SkinColor = shader.SkinColor;
                else
                    shader.SkinColor = realShaders.SkinColor;

                if (human->Shaders->Params->SkinGloss != shader.SkinGloss && shaderLockParams.HasFlag(shaderLockType.SkinColor))
                    human->Shaders->Params->SkinGloss = shader.SkinGloss;
                else
                    shader.SkinGloss = realShaders.SkinGloss;

                if (human->Shaders->Params->MouthColor != shader.MouthColor && shaderLockParams.HasFlag(shaderLockType.LipColor))
                    human->Shaders->Params->MouthColor = shader.MouthColor;
                else
                    shader.MouthColor = realShaders.MouthColor;

                if (human->Shaders->Params->MuscleTone != shader.MuscleTone && shaderLockParams.HasFlag(shaderLockType.MuscleTone))
                    human->Shaders->Params->MuscleTone = shader.MuscleTone;
                else
                    shader.MuscleTone = realShaders.MuscleTone;


                customParams = shader;

                shaderDirty = true;
            }
        }

        public unsafe void ApplyCustomize(CustomizeStruct data, bool ReloadChar = true, bool ApplyShader = false)
        {
            var ages = data.GetValidAges();
            var genders = data.GetValidGenders();
            var tribe = data.GetValidTribes();

            if (!ages.Contains(data.Age))
            {
                data.Age = ages[0];
            }

            if (!genders.Contains(data.Gender))
            {
                data.Gender = genders[0];
            }

            if(!tribe.Contains(data.Tribe))
            {
                data.Tribe = tribe[0];
            }

            if(data.Race == Races.Hrothgar && data.LipColor > 5)
            {
                data.LipColor = 5;
            }

            if (data.Race == Races.Hrothgar && data.LipColor < 1)
            {
                data.LipColor = 1;
            }

            if(data.Race == Races.Viera && data.RaceFeatureType > 4)
            {
                data.RaceFeatureType = 4;
            }

            if (data.Race == Races.Viera && data.RaceFeatureType < 1)
            {
                data.RaceFeatureType = 1;
            }

            customData = data;

            GetCharacter()->DrawData.CustomizeData = *(CustomizeData*)&data;

            if (ReloadChar)
            {
                Reload();
            }
            else
            {
                var charaBase = (CharacterBase*)GetCharacter()->GameObject.DrawObject;

                if(charaBase->GetModelType() == ModelType.Human)
                {
                    var human = (Human*)charaBase;

                    human->UpdateDrawData((byte*)&GetCharacter()->DrawData.CustomizeData, true);

                    if (ApplyShader)
                    {
                        ApplyShaderparams(customParams);
                    }

                    if (scaleLock) SetActorScale(customScale);

                    currentSkeleton = null;
                }
            }
        }

        public void ResetApperance()
        {
            if (!IsLoaded()) return;

            shaderLockParams = shaderLockType.None;
            scaleLock = false;
            ApplyShaderparams(defaultParams);
            ApplyCustomize(defaultData, ReloadChar: false);
            SetTransparency(1);
            SetTint(new(1, 1, 1, 1));
            SetWetness(0);

            SetWeaponSlot(WeaponSlot.MainHand, defaultGear.mainWeapon);
            SetWeaponSlot(WeaponSlot.OffHand, defaultGear.offhand);
            SetEquipmentSlot(EquipmentSlot.Head, defaultGear.head);
            SetEquipmentSlot(EquipmentSlot.Body, defaultGear.body);
            SetEquipmentSlot(EquipmentSlot.Hands, defaultGear.hands);
            SetEquipmentSlot(EquipmentSlot.Legs, defaultGear.legs);
            SetEquipmentSlot(EquipmentSlot.Feet, defaultGear.feet);
            SetEquipmentSlot(EquipmentSlot.Ears, defaultGear.ears);
            SetEquipmentSlot(EquipmentSlot.Neck, defaultGear.neck);
            SetEquipmentSlot(EquipmentSlot.Wrists, defaultGear.wrists);
            SetEquipmentSlot(EquipmentSlot.LFinger, defaultGear.lRIng);
            SetEquipmentSlot(EquipmentSlot.RFinger, defaultGear.rRing);
            SetFacewear(0, defaultGear.faceWear);

            SetModelType((uint)initModelID);

            SetVoice(defaultVoice);

            shaderDirty = false;

            Reload();
        }

        public unsafe void Reload()
        {
            if (IsLoaded())
            {

                currentSkeleton = null;
                GetCharacter()->GameObject.DisableDraw();

                WaitForDraw();
            }
        }

        private unsafe void WaitForDraw()
        {
            DalamudServices.framework.RunOnTick(() =>
            {
                if (actorObject.Base()->IsReadyToDraw())
                {
                    actorObject.Base()->EnableDraw();
                    redrawComplete?.Invoke(true);
                }
                else
                {
                    WaitForDraw();
                }
            });
        }

        private unsafe void WaitForMinionDraw()
        {
            DalamudServices.framework.RunOnTick(() =>
            {
                if(GetCharacter()->CompanionObject != null)
                {
                    if (GetCharacter()->CompanionObject->Character.GameObject.IsReadyToDraw())
                    {
                        GetCharacter()->CompanionObject->Character.GameObject.EnableDraw();
                    }
                    else
                    {
                        WaitForMinionDraw();
                    }
                }
            });
        }

        public unsafe void Rename(string newName)
        {
            var currentName = GetName();

            for (var i = 0; i < currentName.Length; i++)
            {
                actorObject.Base()->Name[i] = 0;      
            }

            for(var i =0 ; i < newName.Length; i++)
            {
                actorObject.Base()->Name[i] = (byte)newName[i];
            }
        }
    }

    public class LocalPlayerActor : XIVActor
    {
        public LocalPlayerActor(IGameObject? _actorObject, bool customActor = false) : base(_actorObject, customActor)
        {
            animCancelled += ModManager.Instance.ClearModData;
        }
    }

    public enum Genders : byte
    {
        Masculine = 0,
        Feminine = 1,
    }

    public enum Age : byte
    {
        Normal = 1,
        Old = 3,
        Young = 4,
    }

    public enum Races : byte
    {
        Hyur = 1,
        Elezen = 2,
        Lalafel = 3,
        Miqote = 4,
        Roegadyn = 5,
        AuRa = 6,
        Hrothgar = 7,
        Viera = 8
    }

    public enum Tribes : byte
    {
        Midlander = 1,
        Highlander = 2,
        Wildwood = 3,
        Duskwight = 4,
        Plainsfolk = 5,
        Dunesfolk = 6,
        SeekerOfTheSun = 7,
        KeeperOfTheMoon = 8,
        SeaWolf = 9,
        Hellsguard = 10,
        Raen = 11,
        Xaela = 12,
        Helions = 13,
        TheLost = 14,
        Rava = 15,
        Veena = 16
    }

    [Flags]
    public enum FacialFeatures : byte
    {
        None = 0,
        First = 1,
        Second = 2,
        Third = 4,
        Fourth = 8,
        Fifth = 16,
        Sixth = 32,
        Seventh = 64,
        LegacyTattoo = 128
    }

    public enum CustomLoadType : byte
    {
        AllButWeapon = 0,
        CharacterOnly = 1,
        EquipmentOnly = 2,
    }

    public struct GearPack
    {
        public WeaponModelId mainWeapon;
        public WeaponModelId offhand;
        public EquipmentModelId head;
        public EquipmentModelId body;
        public EquipmentModelId hands;
        public EquipmentModelId legs;
        public EquipmentModelId feet;
        public EquipmentModelId ears;
        public EquipmentModelId neck;
        public EquipmentModelId wrists;
        public EquipmentModelId lRIng;
        public EquipmentModelId rRing;
        public ushort faceWear;
    }

    [Flags]
    public enum shaderLockType : byte
    {
        None = 0,
        SkinColor = 1,
        LipColor = 2,
        HairColor = 4,
        HighlightColor = 8,
        LEyeColor = 16,
        REyeColor = 32,
        FeatureColor = 64,
        MuscleTone = 128,

        All = SkinColor|LipColor|HairColor|HighlightColor|LEyeColor|REyeColor|FeatureColor|MuscleTone,
    }

    public struct companionSlot
    {
        public companionType type;
        public uint id;

        public companionSlot(companionType type, uint id)
        {
            this.type = type;
            this.id = id;
        }
    }

    public enum companionType { None, Minion, Mount, Ornament};

    public enum AnimCodes : short 
    {
        Hyur_Midlander_Male = 101,
        Hyur_Midlander_Female = 201,
        Hyur_Highalnder_Male = 301,
        Hyur_Highlander_Female = 401,
        Elezen_Male = 501,
        Elezen_Female = 601,
        Miqote_Male = 701,
        Miqote_Female = 801,
        Roegadyn_Male = 901,
        Roegadyn_Female = 1001,
        Lalafell_Male = 1101,
        Lalafell_Female = 1201,
        AuRa_Male = 1301,
        AuRa_Female = 1401,
        Hrothgar_Male = 1501,
        Hrothgar_Female = 1601,
        Viera_Male = 1701,
        Viera_Female = 1801
    }
}
