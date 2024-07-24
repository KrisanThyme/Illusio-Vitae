using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Hooking.Internal;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;
using FFXIVClientStructs.Havok.Animation.Rig;
using IVPlugin.ActorData;
using IVPlugin.Actors.SkeletonData;
using IVPlugin.Core;
using IVPlugin.Core.Extentions;
using IVPlugin.Gpose;
using IVPlugin.Log;
using IVPlugin.Services;
using IVPlugin.UI.Windows;
using IVPlugin.VFX;
using Lumina;
using Lumina.Excel.GeneratedSheets;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.CharaView.Delegates;
using StructsGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace IVPlugin.Posing
{
    public unsafe class PosingManager : IDisposable
    {
        public static PosingManager Instance { get; private set; } = null!;

        private delegate char UpdateBonePhysicsDelegate(Skeleton* a1, ushort a2, nint a3);
        private readonly Hook<UpdateBonePhysicsDelegate> _updateBonePhysicsHook = null!;

        private delegate void FinalizeSkeletonsDelegate(nint a1);
        private readonly Hook<FinalizeSkeletonsDelegate> _finalizeSkeletonsHook = null!;

        public delegate void SetPositionDelegate(StructsGameObject* gameObject, float x, float y, float z);
        private readonly Hook<SetPositionDelegate> _setPositionHook = null!;

        internal unsafe delegate byte AnimFrozenDelegate(uint* a1, int a2);
        internal static Hook<AnimFrozenDelegate> AnimFrozenHook = null!;

        internal unsafe delegate byte* LookAtIKDelegate(byte* a1, long* a2, long* a3, float a4, long* a5, long* a6);
        internal static Hook<LookAtIKDelegate> LookAtIKHook = null!;

        public bool frozen { get; private set; } = false;
        public PosingManager()
        {
            Instance = this;

            var updateBonePhysicsHook = DalamudServices.SigScanner.ScanText(XIVSigs.skeletonUpdatePhysics);
            _updateBonePhysicsHook = DalamudServices.GameInteropProvider.HookFromAddress<UpdateBonePhysicsDelegate>(updateBonePhysicsHook, UpdateBonePhysicsDetour);
            _updateBonePhysicsHook.Enable();

            var finalizeSkeletonsHook = DalamudServices.SigScanner.ScanText(XIVSigs.skeletonFinalize);
            _finalizeSkeletonsHook = DalamudServices.GameInteropProvider.HookFromAddress<FinalizeSkeletonsDelegate>(finalizeSkeletonsHook, FinalizeSkeletonsHook);

            _setPositionHook = DalamudServices.GameInteropProvider.HookFromAddress<SetPositionDelegate>((nint)StructsGameObject.Addresses.SetPosition.Value, UpdatePositionDetour);
            _setPositionHook.Enable();

            var animFrozen = DalamudServices.SigScanner.ScanText(XIVSigs.animationFreeze);
            AnimFrozenHook = DalamudServices.GameInteropProvider.HookFromAddress<AnimFrozenDelegate>(animFrozen, AnimFrozenDetour);

            var lookAtIK = DalamudServices.SigScanner.ScanText(XIVSigs.skeletonLookAtIK);
            LookAtIKHook = DalamudServices.GameInteropProvider.HookFromAddress<LookAtIKDelegate>(lookAtIK, LookAtIKDetour);

            _finalizeSkeletonsHook.Enable();
            _updateBonePhysicsHook.Enable();

            DalamudServices.framework.Update += update;
        }

        public void FreezeAnimation()
        {
            AnimFrozenHook.Enable();
            frozen = true;
        }

        public void UnfreezeAnimation()
        {
            AnimFrozenHook.Disable();
            frozen = false;
        }
        
        private void update(IFramework framework)
        {
            if(frozen) 
            {
                if(!IllusioVitae.InDebug() && !DalamudServices.clientState.IsGPosing)
                {
                    frozen = false;
                }
            }
        }

        private unsafe static byte* LookAtIKDetour(byte* a1, long* a2, long* a3, float a4, long* a5, long* a6)
        {
            return (byte*)nint.Zero;
        }

        private byte AnimFrozenDetour(uint* a1, int a2)
        {
            return 1;
        }

        private char UpdateBonePhysicsDetour(Skeleton* a1, ushort a2, nint a3)
        {
            var result = _updateBonePhysicsHook.Original(a1, a2, 3);

            if (!DalamudServices.clientState.IsLoggedIn || !EventManager.validCheck) return result;

            if (ActorManager.Instance.playerActor.IsLoaded())
            {
                var mc = (ICharacter)ActorManager.Instance.playerActor.actorObject;

                if (mc.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat)) return result;

                try
                {
                    UpdateSkeletons();
                }
                catch (Exception e)
                {
                    IllusioDebug.Log(e + "Error during skeleton update", LogType.Error, false);
                }
            }

            return result;
        }

        private void FinalizeSkeletonsHook(nint a1)
        {
            _finalizeSkeletonsHook.Original(a1);

            if (!DalamudServices.clientState.IsLoggedIn || !EventManager.validCheck) return;

            if (ActorManager.Instance.playerActor.IsLoaded())
            {
                var mc = (ICharacter)ActorManager.Instance.playerActor.actorObject;

                if (mc.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat)) return;

                try
                {
                    UpdateSkeletons();
                }
                catch (Exception e)
                {
                    IllusioDebug.Log(e + "Error during skeleton finalize", LogType.Error, false);
                }
            }
        }

        public void UpdatePositionDetour(StructsGameObject* gameObject, float x, float y, float z)
        {
            if (DalamudServices.clientState.IsGPosing)
            {
                foreach (var actor in ActorManager.Instance.GPoseActors)
                {
                    if (actor.IsLoaded() && actor.actorObject.Base() == gameObject)
                    {
                        if (actor.positionDirty)
                        {
                            actor.SetTransform();
                            return;
                        }
                    }
                }

                foreach (var actor in ActorManager.Instance.Actors)
                {
                    if (actor.IsLoaded() && actor.actorObject.Base() == gameObject)
                    {
                        if (actor.positionDirty)
                        {
                            actor.SetTransform();
                            return;
                        }
                    }
                }
            }

            _setPositionHook.Original(gameObject, x, y, z);
        }

        public void UpdateSkeletons()
        {
            if (DalamudServices.clientState.IsGPosing)
            {
                foreach(var actor in ActorManager.Instance.GPoseActors)
                {
                    if(actor.currentSkeleton != null && actor.IsLoaded(true))
                    {
                        actor.currentSkeleton.UpdateSkeletons();
                    }
                }
            }
            else
            {
                foreach (var actor in ActorManager.Instance.Actors)
                {
                    if (actor.currentSkeleton != null && actor.IsLoaded(true))
                    {
                        actor.currentSkeleton.UpdateSkeletons();
                    }
                }
            }
        }
        
        public void Dispose()
        {
            _setPositionHook.Dispose();
            AnimFrozenHook.Dispose();
            LookAtIKHook.Dispose();
            _updateBonePhysicsHook.Dispose();
            _finalizeSkeletonsHook.Dispose();
            DalamudServices.framework.Update -= update;
        }
    }
}
