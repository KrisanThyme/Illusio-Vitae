using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.Havok;
using IVPlugin.ActorData;
using IVPlugin.Core;
using IVPlugin.Core.Extentions;
using IVPlugin.Log;
using IVPlugin.Services;
using IVPlugin.VFX.Struct;
using Lumina.Excel.GeneratedSheets;
using Lumina.Excel.GeneratedSheets2;
using Microsoft.VisualBasic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.VFX
{
    public unsafe class VFXManager : IDisposable
    {
        // Speacial Thanks to VFXEditor and its creator 0ceal0t

        private static readonly string Pool = "Client.System.Scheduler.Instance.VfxObject";
        public static VFXManager Instance { get; private set; } = null!;

        private delegate VfxStruct* StaticVfxCreateDelegate(string path, string pool);

        private StaticVfxCreateDelegate StaticVfxCreate;

        private delegate IntPtr StaticVfxRunDelegate(IntPtr vfx, float a1, uint a2);

        private StaticVfxRunDelegate StaticVfxRun;

        private delegate IntPtr StaticVfxRemoveDelegate(IntPtr vfx);

        private StaticVfxRemoveDelegate StaticVfxRemove;

        private Hook<StaticVfxCreateDelegate> StaticVfxCreateHook;

        private Hook<StaticVfxRemoveDelegate> StaticVfxRemoveHook;

        private delegate IntPtr ActorVfxCreateDelegate(string path, IntPtr a2, IntPtr a3, float a4, char a5, ushort a6, char a7);

        private ActorVfxCreateDelegate ActorVfxCreate;

        private delegate IntPtr ActorVfxRemoveDelegate(IntPtr vfx, char a2);

        private ActorVfxRemoveDelegate ActorVfxRemove;

        private Hook<ActorVfxCreateDelegate> ActorVfxCreateHook;

        private Hook<ActorVfxRemoveDelegate> ActorVfxRemoveHook;

        public readonly ConcurrentDictionary<nint, VFXData> ActorVFX = new();
        public readonly ConcurrentDictionary<nint, VFXData> staticVFX = new();

        public VFXManager()
        {
            Instance = this;

            var staticVfxCreateAddress = DalamudServices.SigScanner.ScanText(XIVSigs.vfxStaticCreate);
            var staticVfxRemoveAddress = DalamudServices.SigScanner.ScanText(XIVSigs.vfxStaticRemove);
            var actorVfxCreateAddress = DalamudServices.SigScanner.ScanText(XIVSigs.vfxActorCreate);
            var actorVfxRemoveAddressTemp = DalamudServices.SigScanner.ScanText(XIVSigs.vfxActorRemove) + 7;
            var actorVfxRemoveAddress = Marshal.ReadIntPtr(actorVfxRemoveAddressTemp + Marshal.ReadInt32(actorVfxRemoveAddressTemp) + 4);


            ActorVfxCreate = Marshal.GetDelegateForFunctionPointer<ActorVfxCreateDelegate>(actorVfxCreateAddress);
            ActorVfxRemove = Marshal.GetDelegateForFunctionPointer<ActorVfxRemoveDelegate>(actorVfxRemoveAddress);

            StaticVfxRemove = Marshal.GetDelegateForFunctionPointer<StaticVfxRemoveDelegate>(staticVfxRemoveAddress);
            StaticVfxRun = Marshal.GetDelegateForFunctionPointer<StaticVfxRunDelegate>(DalamudServices.SigScanner.ScanText(XIVSigs.vfxStaticExecute));
            StaticVfxCreate = Marshal.GetDelegateForFunctionPointer<StaticVfxCreateDelegate>(staticVfxCreateAddress);

            StaticVfxCreateHook = DalamudServices.GameInteropProvider.HookFromAddress<StaticVfxCreateDelegate>(staticVfxCreateAddress, StaticVfxNewDetour);
            StaticVfxRemoveHook = DalamudServices.GameInteropProvider.HookFromAddress<StaticVfxRemoveDelegate>(staticVfxRemoveAddress, StaticVfxRemoveDetour);
            ActorVfxCreateHook = DalamudServices.GameInteropProvider.HookFromAddress<ActorVfxCreateDelegate>(actorVfxCreateAddress, ActorVfxNewDetour);
            ActorVfxRemoveHook = DalamudServices.GameInteropProvider.HookFromAddress<ActorVfxRemoveDelegate>(actorVfxRemoveAddress, ActorVfxRemoveDetour);

            StaticVfxCreateHook.Enable();
            StaticVfxRemoveHook.Enable();
            ActorVfxCreateHook.Enable();
            ActorVfxRemoveHook.Enable();

        }

        public nint SpawnActorVFX(string path, nint caster, nint target)
        {
            var vfx = (VfxStruct*)ActorVfxCreate(path, caster, target, -1, (char)0, 0, (char)0);

            return (nint)vfx;
        }

        public void RemoveActorVFX(nint vfx)
        {
            if (ActorVFX.TryGetValue(vfx, out var item))
            {
                if(!item.Removed)
                    ActorVfxRemove(vfx, (char)1);
            }
        }

        public nint SpawnStaticVFX(string path, Vector3 Pos, Vector3 Rot) 
        {
            var Vfx = (VfxStruct*)StaticVfxCreate(path, "Client.System.Scheduler.Instance.VfxObject");
            if (Vfx == null) return (nint)Vfx;

            StaticVfxRun((nint)Vfx, 0f, 0xFFFFFFFF);

            if (Vfx != null)
            {
                Vfx->Position = new Vector3
                {
                    X = Pos.X,
                    Y = Pos.Y,
                    Z = Pos.Z
                };

                var q = Quaternion.CreateFromYawPitchRoll(Rot.X, Rot.Y, Rot.Z);
                Vfx->Rotation = new Quat
                {
                    X = q.X,
                    Y = q.Y,
                    Z = q.Z,
                    W = q.W
                };

                Vfx->Flags |= 2;
            }

            return (nint)Vfx;
        }

        public void EnableStaticVFX(nint vfx, int maxTicks, int currentTick)
        {
            if(maxTicks == currentTick)
            {
                IllusioDebug.Log("Max ticks for vfx", LogType.Debug);
                return;
            }

            var result = StaticVfxRun((nint)vfx, 0f, 0xFFFFFFFF);

            if (result != 0)
            {
                IllusioDebug.Log($"Bad VFX Run {result}", LogType.Debug);

                DalamudServices.framework.RunOnTick(() =>
                {
                    EnableStaticVFX(vfx, maxTicks, ++currentTick);
                }, TimeSpan.FromTicks(1));
            }
        }

        public void RemoveStaticVFX(nint vfx) 
        {
            if (staticVFX.TryGetValue(vfx, out var item))
            {
                if (!item.Removed)
                    StaticVfxRemove(vfx);
            }
        }
        
        private VfxStruct* StaticVfxNewDetour(string path, string pool)
        {
            var vfx = StaticVfxCreateHook.Original(path, pool);

            AddStaticVFX(vfx, path);

            return vfx;
        }

        private IntPtr StaticVfxRemoveDetour(IntPtr vfx)
        {
            RemoveStaticVFX((VfxStruct*)vfx);
            return StaticVfxRemoveHook.Original(vfx);
        }

        private void AddStaticVFX(VfxStruct* vfx, string path)
        {
            var result = staticVFX.TryAdd(new IntPtr(vfx), new VFXData()
            {
                Path = path,
                Vfx = vfx
            });

            if (staticVFX.TryGetValue(new IntPtr(vfx), out var item))
            {
                item.Removed = false;
            }
        }

        private void RemoveStaticVFX(VfxStruct* vfx)
        {
            if (staticVFX.TryGetValue(new IntPtr(vfx), out var item))
            {
                item.Removed = true;
                item.RemovedTime = DateTime.Now;
            }
        }

        private IntPtr ActorVfxNewDetour(string path, IntPtr a2, IntPtr a3, float a4, char a5, ushort a6, char a7)
        {
            var vfx = ActorVfxCreateHook.Original(path, a2, a3, a4, a5, a6, a7);
            AddActorVFX((VfxStruct*)vfx, path);

            return vfx;
        }

        private IntPtr ActorVfxRemoveDetour(IntPtr vfx, char a2)
        {
            RemoveActorVFX((VfxStruct*)vfx);

            return ActorVfxRemoveHook.Original(vfx, a2);
        }

        private void AddActorVFX(VfxStruct* vfx, string path)
        {
            var result = ActorVFX.TryAdd(new IntPtr(vfx), new VFXData()
            {
                Path = path,
                Vfx = vfx
            });

            if (ActorVFX.TryGetValue(new IntPtr(vfx), out var item))
            {
                item.Removed = false;
            }
        }

        private void RemoveActorVFX(VfxStruct* vfx)
        {
            if (ActorVFX.TryGetValue(new IntPtr(vfx), out var item))
            {
                item.Removed = true;
                item.RemovedTime = DateTime.Now;
            }
        }

        public void Dispose()
        {
            ActorVfxCreateHook?.Dispose();
            ActorVfxRemoveHook?.Dispose();
            StaticVfxCreateHook?.Dispose();
            StaticVfxRemoveHook?.Dispose();
        }
    }
}
