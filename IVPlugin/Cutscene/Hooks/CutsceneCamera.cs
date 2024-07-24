using Dalamud.Hooking;
using IVPlugin.Camera;
using IVPlugin.Camera.Struct;
using IVPlugin.Core;
using IVPlugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;
using Matrix4x4 = FFXIVClientStructs.FFXIV.Common.Math.Matrix4x4;
using System.Runtime.InteropServices;
using RenderCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera;

namespace IVPlugin.Cutscene.Hooks
{
    public unsafe class CutsceneCamera
    {
        private IllusioVitae Plugin { get; }

        private VirtualCamera virtualCamera => Plugin.virtualCamera;

        private delegate Matrix4x4* MakeProjectionMatrix2(nint ptr, float fov, float aspect, float nearPlane, float farPlane, float a6, float a7);
        private static Hook<MakeProjectionMatrix2> ProjectionHook = null!;

        private delegate Matrix4x4* CalculateViewMatrix(SceneCamera* a1);
        private static Hook<CalculateViewMatrix> ViewHook = null!;

        //thanks to ktsis
        private unsafe delegate Matrix4x4* LoadMatrixDelegate(RenderCamera* camera, Matrix4x4* matrix, int a3, int a4);
        private static LoadMatrixDelegate LoadMatrix = null!;

        public CutsceneCamera(IllusioVitae plugin)
        {
            this.Plugin = plugin;

            var proj = DalamudServices.SigScanner.ScanText(XIVSigs.cameraProjection);
            ProjectionHook = DalamudServices.GameInteropProvider.HookFromAddress<MakeProjectionMatrix2>(proj, ProjectionDetour);

            var view = DalamudServices.SigScanner.ScanText(XIVSigs.cameraView);
            ViewHook = DalamudServices.GameInteropProvider.HookFromAddress<CalculateViewMatrix>(view, ViewMatrixDetour);

            var loadMxAddr = DalamudServices.SigScanner.ScanText(XIVSigs.cameraLoadMatrix);
            LoadMatrix = Marshal.GetDelegateForFunctionPointer<LoadMatrixDelegate>(loadMxAddr);

            ProjectionHook.Enable();
            ViewHook.Enable();
        }

        private unsafe Matrix4x4* ProjectionDetour(IntPtr ptr, float fov, float aspect, float nearPlane, float farPlane, float a6, float a7)
        {
            if (virtualCamera.IsActive)
                fov = virtualCamera.State.FoV;

            var exec = ProjectionHook.Original(ptr, fov, aspect, nearPlane, farPlane, a6, a7);

            return exec;
        }

        private unsafe Matrix4x4* ViewMatrixDetour(SceneCamera* a1)
        {
 
            if (!virtualCamera.IsActive)
                return ViewHook.Original(a1);

            try
            {
                var cam = XIVCamera.instance.GetCurrentCamera();

                var tarMatrix = &cam->Camera.SceneCamera.ViewMatrix;

                var cameraState = virtualCamera.State;

                *tarMatrix = cameraState.Matrix;

                LoadMatrix(cam->Camera.CameraBase.SceneCamera.RenderCamera, tarMatrix, 0, 0);

                return tarMatrix;
            }catch (Exception ex) { }
            

            return ViewHook.Original(a1);
        }

        public void Dispose()
        {
            ProjectionHook.Dispose();
            ViewHook.Dispose();
        }
    }
}
