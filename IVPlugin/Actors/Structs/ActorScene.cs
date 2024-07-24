using FFXIVClientStructs.FFXIV.Client.Graphics;
using IVPlugin.ActorData;
using IVPlugin.Camera;
using IVPlugin.Core;
using IVPlugin.Core.Extentions;
using IVPlugin.Core.Files;
using IVPlugin.Json;
using IVPlugin.Log;
using IVPlugin.Posing;
using IVPlugin.Services;
using IVPlugin.UI;
using Lumina.Excel.GeneratedSheets;
using Lumina.Excel.GeneratedSheets2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Lumina.Data.Parsing.Layer.LayerCommon;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IVPlugin.Actors.Structs
{
    [Serializable]
    public class ActorScene
    {
        public int location { get; set; } = 0;
        public List<SceneActor> sceneActors { get; set; } = new();
        public CameraData cameraData { get; set; }

        public bool changePlayerAppearance { get; set; }
        public bool onlyUseBase = false;

        public void SaveScene()
        {
            location = DalamudServices.clientState.TerritoryType;

            List<XIVActor> actors = ActorManager.Instance.Actors;

            if (DalamudServices.clientState.IsGPosing)
            {
                actors = ActorManager.Instance.GPoseActors;
            }

            foreach(var actor in actors)
            {
                SceneActor cActor = new()
                {
                    worldTransform = actor.GetTransform().toSceneTransform(),
                    offsetFromPlayer = GetOffsetFromPlayer(actor.GetTransform()),
                    actorName = actor.GetName(),
                };

                cActor.Appearance = new CharaFile();

                cActor.Appearance.WriteToFile(actor, CharaFile.SaveModes.All);

                cActor.Pose = actor.currentSkeleton.SaveSkeletonPose();

                sceneActors.Add(cActor);
            }

            var tempcameraData = new CameraData();

            unsafe
            {
                var CurrentCamera = XIVCamera.instance.GetCurrentCamera();

                tempcameraData.Offset = XIVCamera.instance.posOffset;
                tempcameraData.Fov = CurrentCamera->FoV;
                tempcameraData.Angle = CurrentCamera->Angle;
                tempcameraData.Pan = CurrentCamera->Pan;
                tempcameraData.Rotation = CurrentCamera->Rotation;
                tempcameraData.Zoom = CurrentCamera->Camera.Distance;
                tempcameraData.disableCollision = XIVCamera.instance.disableCollision;
                tempcameraData.disableZoomLimits = XIVCamera.instance.removeZoomLimits;
            }

            cameraData = tempcameraData;
           
            var data = JsonHandler.Serialize(this);

            WindowsManager.Instance.fileDialogManager.SaveFileDialog("Save Actor Scene", ".ivscene", "new scene", ".ivscene", (Confirm, Path) =>
            {
                if (!Confirm) return;
                File.WriteAllText(Path, data);
            });
        }

        public void LoadScene()
        {
            List<XIVActor> actors = ActorManager.Instance.Actors;
            XIVActor player = ActorManager.Instance.playerActor;

            if (DalamudServices.clientState.IsGPosing)
            {
                player = ActorManager.Instance.mainGposeActor;
                actors = ActorManager.Instance.GPoseActors;
            }

            for (var i = 0; i < sceneActors.Count; i++)
            {
                var sceneActor = sceneActors[i];
                XIVActor actor = null;

                if (i < actors.Count)
                {
                    actor = actors[i];

                    if(i == 0)
                    {
                        if (changePlayerAppearance)
                        {
                            sceneActor.Appearance.Apply(actor, CharaFile.SaveModes.All);
                            actor.Rename(sceneActor.actorName);
                        }
                    }
                    else
                    {
                        sceneActor.Appearance.Apply(actor, CharaFile.SaveModes.All);
                        actor.Rename(sceneActor.actorName);
                    }

                    
                }
                else
                {
                    actor = ActorManager.Instance.SetUpCustomActor(sceneActor.Appearance, sceneActor.actorName);
                }

                Transform finalTransform = sceneActor.worldTransform;

                if (IllusioVitae.configuration.ActorSceneLocalSpace)
                {
                    finalTransform.Position = new(player.GetTransform().Position.X - sceneActor.offsetFromPlayer.X, player.GetTransform().Position.Y - sceneActor.offsetFromPlayer.Y, player.GetTransform().Position.Z - sceneActor.offsetFromPlayer.Z);
                }

                DalamudServices.framework.RunOnTick(() =>
                {
                    CheckandApplyData(sceneActor.Pose, finalTransform, actor);
                }, TimeSpan.FromSeconds(1));
            }

            DalamudServices.framework.RunOnTick(() =>
            {
                unsafe
                {
                    var currentCam = XIVCamera.instance.GetCurrentCamera();

                    if(cameraData.disableZoomLimits)
                        XIVCamera.instance.RemoveZoomLimits();
                    
                    XIVCamera.instance.disableCollision = cameraData.disableCollision;

                    currentCam->FoV = cameraData.Fov;
                    currentCam->Rotation = cameraData.Rotation;
                    currentCam->Camera.Distance = cameraData.Zoom;
                    currentCam->Pan = cameraData.Pan;
                    currentCam->Camera.InterpDistance = cameraData.Zoom;

                    if (currentCam->type == Camera.Struct.CameraType.Legacy)
                    {
                        if (cameraData.Angle.X - currentCam->Angle.X > 0)
                        {
                            Vector2 tempAngle = new(cameraData.Angle.X + .079f, cameraData.Angle.Y);
                            currentCam->Angle = tempAngle;
                        }
                        else
                        {
                            Vector2 tempAngle = new(cameraData.Angle.X - .079f, cameraData.Angle.Y);
                            currentCam->Angle = tempAngle;
                        }
                    }
                    else
                    {
                        currentCam->Angle = cameraData.Angle;
                    }
                }

                XIVCamera.instance.SetCameraOffset(cameraData.Offset);
            }, TimeSpan.FromSeconds(1.25f));
        }

        private void CheckandApplyData(PoseFile pose, Transform transform, XIVActor actor)
        {
            if (actor.IsLoaded(true) && actor.currentSkeleton != null)
            {
               actor.currentSkeleton.ApplySkeletonPose(pose, onlyUseBase);
                
               actor.SetOverrideTransform(transform);
            }
            else
            {
                DalamudServices.framework.RunOnTick(() =>
                {
                    CheckandApplyData(pose, transform, actor);
                }, TimeSpan.FromTicks(1));
            }
        }

        private Vector3 GetOffsetFromPlayer(Transform transform)
        {
            Vector3 pos = new();

            if (DalamudServices.clientState.IsGPosing)
            {
                pos = ActorManager.Instance.mainGposeActor.GetTransform().Position - transform.Position;
            }
            else
            {
                pos = ActorManager.Instance.playerActor.GetTransform().Position - transform.Position;
            }


            return pos;
        }
    }

    [Serializable]
    public struct SceneActor
    {
        public SceneTransform worldTransform { get; set; }
        public Vector3 offsetFromPlayer {  get; set; }
        public string actorName { get; set; }
        public CharaFile Appearance { get; set; }
        public PoseFile Pose { get; set; }
    }

    [Serializable]
    public struct SceneTransform
    {
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 scale { get; set; }

        public static implicit operator Transform(SceneTransform transform) => new() { Position = transform.position, Rotation = transform.rotation, Scale = transform.scale };
    }

    [Serializable]

    public struct CameraData
    {
        public Vector3 Offset { get; set; }
        public float Fov { get; set; }
        public float Rotation { get; set; }
        public float Zoom { get; set; }
        public Vector2 Angle { get; set; }
        public Vector2 Pan { get; set; }
        public bool disableZoomLimits { get; set; }
        public bool disableCollision { get; set; }
    }
}
