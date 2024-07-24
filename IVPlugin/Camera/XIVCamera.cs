using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using IVPlugin.Camera.Struct;
using IVPlugin.Core;
using IVPlugin.Cutscene;
using IVPlugin.Log;
using IVPlugin.Services;
using IVPlugin.UI.Windows.Tabs;
using static Dalamud.Plugin.Services.IFramework;


namespace IVPlugin.Camera
{
    public unsafe class XIVCamera : IDisposable
    {
        //Special Thanks to XAT/Asgard for camera

        public static XIVCamera instance = null!;

        private delegate nint CameraCollisionDelegate(XIVCameraStruct* a1, Vector3* a2, Vector3* a3, float a4, nint a5, float a6);
        private readonly Hook<CameraCollisionDelegate> _cameraCollisionHook = null!;

        private delegate nint CameraUpdateDelegate(nint camera);
        private readonly Hook<CameraUpdateDelegate> _cameraUpdateHook = null!;

        public Vector3 posOffset = Vector3.Zero;
        public bool disableCollision = false;
        public bool removeZoomLimits = false;
        public bool forceCameraPositon = false;
        public float originalMinDistance = 0;
        public float originalMaxDistance = 0;

        private Vector3 forcedPos = Vector3.Zero, forceLookAt = Vector3.Zero;

        public XIVCamera()
        {
            instance = this;

            _cameraCollisionHook = DalamudServices.GameInteropProvider.HookFromAddress<CameraCollisionDelegate>(DalamudServices.SigScanner.ScanText(XIVSigs.cameraCollision), CameraCollisionDetour);
            _cameraUpdateHook = DalamudServices.GameInteropProvider.HookFromAddress<CameraUpdateDelegate>(DalamudServices.SigScanner.ScanText(XIVSigs.cameraUpdate), CameraUpdateDetour);

            _cameraCollisionHook.Enable();
            _cameraUpdateHook.Enable();



            EventManager.GPoseChange += (_) => resetBools();
        }

        private void resetBools() 
        {
            disableCollision = false;
            if(removeZoomLimits)
            {
                ReinstateZoomLimits();
            }
            
            forceCameraPositon = false;

            WorldTab.ResetCameraControls();
        }

        public void ForcePositions(Vector3 forcedPos, Vector3 forceLookAt)
        {
            this.forcedPos = forcedPos;
            this.forceLookAt = forceLookAt;

            this.forceCameraPositon = true;
        }

        public void RemoveZoomLimits()
        {
            originalMinDistance = GetCurrentCamera()->Camera.MinDistance;
            originalMaxDistance = GetCurrentCamera()->Camera.MaxDistance;
            GetCurrentCamera()->Camera.MinDistance = 0;
            GetCurrentCamera()->Camera.MaxDistance = 1000;

            removeZoomLimits = true;
        }

        public void ReinstateZoomLimits()
        {
            GetCurrentCamera()->Camera.MaxDistance = originalMaxDistance;
            GetCurrentCamera()->Camera.MinDistance = originalMinDistance;
            removeZoomLimits = false;
        }

        public XIVCameraStruct* GetCurrentCamera()
        {
            return (XIVCameraStruct*)CameraManager.Instance()->GetActiveCamera();
        }

        public void SetCameraOffset(Vector3 offset)
        {
            posOffset = offset;

            forceCameraPositon = false;
        }

        private nint CameraUpdateDetour(nint camera)
        {
            var result = _cameraUpdateHook.Original(camera);

            if (DalamudServices.clientState.IsGPosing || IllusioVitae.InDebug())
            {
                var currentCam = GetCurrentCamera();

                Vector3 currentPos = currentCam->Camera.CameraBase.SceneCamera.Object.Position;
                Vector3 newPos = posOffset + currentPos;
                currentCam->Camera.CameraBase.SceneCamera.Object.Position = newPos;

                Vector3 currentLookAt = currentCam->Camera.CameraBase.SceneCamera.LookAtVector;
                currentCam->Camera.CameraBase.SceneCamera.LookAtVector = currentLookAt + (newPos - currentPos);       
            }

            return result;
        }

        private nint CameraCollisionDetour(XIVCameraStruct* camera, Vector3* a2, Vector3* a3, float a4, nint a5, float a6)
        {
            if (DalamudServices.clientState.IsGPosing || IllusioVitae.InDebug())
            {
                if (GetCurrentCamera() == camera && disableCollision)
                {
                    camera->Collide = new Vector2(camera->Camera.MaxDistance);
                    return 0;
                }
            }

            return _cameraCollisionHook.Original(camera, a2, a3, a4, a5, a6);
        }

        public void Dispose()
        {
            _cameraCollisionHook.Dispose();
            _cameraUpdateHook.Dispose();

            EventManager.GPoseChange -= (_) => resetBools();
        }
    }
}
