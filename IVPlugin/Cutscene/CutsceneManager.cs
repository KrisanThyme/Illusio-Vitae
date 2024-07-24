using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using IVPlugin.ActorData;
using IVPlugin.Core.Extentions;
using IVPlugin.Core.Files;
using IVPlugin.Services;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.Cutscene
{
    public class CameraSettings
    {
        public Vector3 Scale = Vector3.One;
        public Vector3 Offset = Vector3.Zero;
        public bool Loop = false;
    }

    public class IllusioCutsceneManager
    {
        //Special Thanks to XAT/Asgard for camera

        private IllusioVitae Plugin { get; }

        private VirtualCamera virtualCamera => Plugin.virtualCamera;

        private const double FRAME_STEP = 33.33333333333333;

        public static IllusioCutsceneManager Instance { get; private set; }

        public CameraSettings CameraSettings { get; set; } = new();

        public XATCameraPathFile? CameraPath { get; set; }

        public bool IsRunning => stopwatch.IsRunning;
        private Stopwatch stopwatch = new();

        private Vector3 BasePosition { get; set; }
        private Quaternion BaseRotation { get; set; }

        public IllusioCutsceneManager(IllusioVitae plugin)
        {
            Plugin = plugin;

            Instance = this;

            DalamudServices.framework.Update += Update;
        }

        public void StartPlayback(bool useCharaHeight)
        {
            unsafe
            {
                XIVActor actor = ActorManager.Instance.playerActor;

                if (DalamudServices.clientState.IsGPosing)
                {
                    actor = ActorManager.Instance.mainGposeActor;
                }


                var gameObject = actor.actorObject.Base();
                var data = actor.GetCustomizeData();

                if (gameObject == null)
                    return;

                if (useCharaHeight)
                {
                    CameraSettings.Scale = SettingPresets.presets.FirstOrDefault(x => x.race == data.Race && x.gender == data.Gender).settings.Scale;
                    CameraSettings.Scale *= actor.GetActorScale();
                }



                BasePosition = new Vector3(gameObject->DrawObject->Object.Position.X, gameObject->DrawObject->Object.Position.Y, gameObject->DrawObject->Object.Position.Z);
                BaseRotation = new Quaternion(gameObject->DrawObject->Object.Rotation.X, gameObject->DrawObject->Object.Rotation.Y, gameObject->DrawObject->Object.Rotation.Z, gameObject->DrawObject->Object.Rotation.W);
            }

            stopwatch.Reset();
            stopwatch.Start();
            virtualCamera.IsActive = true;

        }

        public void StopPlayback()
        {
            if (IsRunning)
            {
                stopwatch.Reset();
                virtualCamera.IsActive = false;
            }

        }

        public void Update(IFramework framework)
        {
            if (!IsRunning || CameraPath == null)
                return;

            if (!DalamudServices.clientState.IsGPosing) StopPlayback();

            double totalMillis = stopwatch.ElapsedMilliseconds;

            var previousKey = CameraPath.CameraFrames[0];
            XATCameraPathFile.CameraKeyframe? nextKey = null;

            foreach (var key in CameraPath.CameraFrames)
            {
                var frameStart = key.Frame * FRAME_STEP;
                if (frameStart > totalMillis)
                {
                    nextKey = key;
                    break;
                }
                else
                {
                    previousKey = key;
                }
            }

            if (previousKey == null || nextKey == null)
            {
                if (CameraSettings.Loop)
                {
                    stopwatch.Restart();
                    Update(framework);
                }
                else
                {
                    StopPlayback();
                }

                return;
            }

            var previousFrameStart = previousKey.Frame * FRAME_STEP;
            var nextFrameStart = nextKey.Frame * FRAME_STEP;
            var blendLength = nextFrameStart - previousFrameStart;
            var pastPreviousKey = totalMillis - previousFrameStart;
            var frameProgress = (float)(pastPreviousKey / blendLength);

            // First we calculate the raw position/rotation/fov based on the frame progress
            var rawPosition = Vector3.Lerp(previousKey.Position, nextKey.Position, frameProgress);
            var rawRotation = Quaternion.Lerp(previousKey.Rotation, nextKey.Rotation, frameProgress);
            var rawFoV = previousKey.FoV + (nextKey.FoV - previousKey.FoV) * frameProgress;

            // Apply the user adjustmenets for the position 
            var adjustedPosition = rawPosition * CameraSettings.Scale + CameraSettings.Offset;

            // Now we apply the rotation from the base to the raw values and get a matrix for each
            var rotatedLocalPosition = BaseRotation.RotatePosition(adjustedPosition);
            var localRotation = BaseRotation * rawRotation;
            var localRotationMatrix = Matrix4x4.CreateFromQuaternion(localRotation);
            Matrix4x4.Invert(localRotationMatrix, out var invertedLocalRotationMatrix);
            var localTranslationMatrix = Matrix4x4.CreateTranslation(-rotatedLocalPosition);

            // Create a matrix with the base position
            var basePositionMatrix = Matrix4x4.CreateTranslation(-BasePosition);

            // Create the final matrix
            var finalMat = basePositionMatrix * (localTranslationMatrix * invertedLocalRotationMatrix);

            virtualCamera.State = new
            (
                Matrix: finalMat,
                FoV: rawFoV
            );

        }

        public void Dispose()
        {
            DalamudServices.framework.Update -= Update;
        }
    }
}
