using Dalamud.Game.Network.Structures.InfoProxy;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;
using IVPlugin.Actors.Structs;
using IVPlugin.Core;
using IVPlugin.Core.Files;
using IVPlugin.Log;
using IVPlugin.Posing;
using IVPlugin.Services;
using IVPlugin.UI.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dalamud.Interface.Utility.Raii.ImRaii;
using static IVPlugin.Core.Files.PoseFile;

namespace IVPlugin.Actors.SkeletonData
{
    public unsafe class IllusioSkeleton
    {
        public bool isComplete = false;

        public Skeleton* mainSkelePtr, mainHandSkelePtr, offHandSkelePtr;

        public List<illusioPartialSkeleton> skeletons = new();

        public List<illusioPartialSkeleton> mhSkeletons = new();

        public List<illusioPartialSkeleton> ohSkeletons = new();

        public IllusioSkeleton(Skeleton* skeleton, Skeleton* mhSkeleton, Skeleton* ohSkeleton)
        {
            mainSkelePtr = skeleton;
            mainHandSkelePtr = mhSkeleton;
            offHandSkelePtr = ohSkeleton;

            SetUpSkeletons();
        }

        public void UpdateSkeletons()
        {
            if (!EventManager.validCheck) return;
            if (mainSkelePtr->Owner == null) return;
            if (!mainSkelePtr->Owner->DrawObject.IsVisible) return;

            foreach(var s in skeletons)
            {
                foreach(var bone in s.boneList)
                {
                    if(bone.isDirty)
                    {
                        bone.ApplyTransformdata();
                    }
                }
            }

            foreach (var s in mhSkeletons)
            {
                foreach (var bone in s.boneList)
                {
                    if (bone.isDirty)
                    {
                        bone.ApplyTransformdata();
                    }
                }
            }

            foreach (var s in ohSkeletons)
            {
                foreach (var bone in s.boneList)
                {
                    if (bone.isDirty)
                    {
                        bone.ApplyTransformdata();
                    }
                }
            }

            //UpdateConnectedBones();
            //UpdateAttached();
        }

        private void UpdateAttached()
        {

        }

        private void UpdateConnectedBones()
        {
            for (var i = 1; i < skeletons.Count; i++)
            {
                var currentSkele = skeletons[i];

                var connectedBone = skeletons[0].boneList.FirstOrDefault(x => x.idx == currentSkele.connectedBoneIDX);

                if (connectedBone != null)
                {
                    connectedBone.GetTransformData();

                    currentSkele.boneList[0].SetTransformData(connectedBone.GetTransformData(), MirrorModes.None, true);
                }
            }
        }

        public bool GetBoneByName(string name, int partialIDX, SkeletonType type, out IllusioBone outBone)
        {
            var currentSkeles = skeletons;

            if (SkeletonType.Mainhand == type) currentSkeles = mhSkeletons;

            if (SkeletonType.Offhand == type) currentSkeles = ohSkeletons;

            foreach (var bone in currentSkeles[partialIDX].boneList)
            {
                if (bone.Name == name)
                {
                    outBone = bone;
                    return true;
                }
            }

            outBone = null;
            return false;
        }

        public bool GetBoneByIDX(int idx, int partialIDX, SkeletonType type, out IllusioBone outBone)
        {
            var currentSkeles = skeletons;            

            if (SkeletonType.Mainhand == type) currentSkeles = mhSkeletons;

            if(SkeletonType.Offhand == type) currentSkeles = ohSkeletons;

            if (currentSkeles.Count - 1 < partialIDX)
            {
                outBone = null;
                return false;
            }

            foreach (var bone in currentSkeles[partialIDX].boneList)
            {
                if (bone.idx == idx)
                {
                    outBone = bone;
                    return true;
                }
            }

            outBone = null;
            return false;
        }

        public void UpdateBone(Transform transform, IllusioBone bone, SkeletonType type, MirrorModes mirror, bool chain = false)
        {
            if(GetBoneByIDX(bone.idx, bone.partialIDX, type, out var actualBone))
            {
                actualBone.SetTransformData(transform, mirror, chain);
            }
            else
            {
                IllusioDebug.Log($"Unable to find bone {bone.Name}", LogType.Warning);
            }
        }

        private void SetUpSkeletons()
        {
            if ((nint)mainSkelePtr == nint.Zero) return;

            for (var i = 0; i < mainSkelePtr->PartialSkeletonCount; i++)
            {
                illusioPartialSkeleton skeleton = new();

                var currentPSkele = (PartialSkeleton*)((nint)mainSkelePtr->PartialSkeletons + (0x220 * i));

                IllusioDebug.Log($"Partial Skeleton {i} Address: {((nint)currentPSkele).ToString("X")}", LogType.Verbose);

                skeleton.partialIDX = i;

                skeleton.connectedBoneIDX = currentPSkele->ConnectedParentBoneIndex;

                var pose = currentPSkele->GetHavokPose(0);

                if (pose == null) 
                {
                    skeletons.Add(skeleton);
                    continue;
                } 

                for(var b = 0; b < pose->Skeleton->Bones.Length; b++)
                {
                    IllusioBone tempBone = new(this, mainSkelePtr, pose, b, i, SkeletonType.Main);

                    skeleton.boneList.Add(tempBone);
                }

                skeletons.Add(skeleton);
            }

            if(mainHandSkelePtr != null)
            {
                for (var i = 0; i < mainHandSkelePtr->PartialSkeletonCount; i++)
                {
                    illusioPartialSkeleton skeleton = new();

                    var currentPSkele = mainHandSkelePtr->PartialSkeletons[i];

                    skeleton.partialIDX = i;

                    skeleton.connectedBoneIDX = currentPSkele.ConnectedParentBoneIndex;

                    var pose = currentPSkele.GetHavokPose(0);

                    if (pose == null)
                    {
                        skeletons.Add(skeleton);
                        continue;
                    }

                    for (var b = 0; b < pose->Skeleton->Bones.Length; b++)
                    {
                        IllusioBone tempBone = new(this, mainHandSkelePtr, pose, b, i, SkeletonType.Mainhand);

                        skeleton.boneList.Add(tempBone);
                    }

                    mhSkeletons.Add(skeleton);
                }
            }

            if(offHandSkelePtr != null)
            {
                for (var i = 0; i < offHandSkelePtr->PartialSkeletonCount; i++)
                {
                    illusioPartialSkeleton skeleton = new();

                    var currentPSkele = offHandSkelePtr->PartialSkeletons[i];

                    skeleton.partialIDX = i;

                    skeleton.connectedBoneIDX = currentPSkele.ConnectedParentBoneIndex;

                    var pose = currentPSkele.GetHavokPose(0);

                    if (pose == null)
                    {
                        skeletons.Add(skeleton);
                        continue;
                    }

                    for (var b = 0; b < pose->Skeleton->Bones.Length; b++)
                    {
                        IllusioBone tempBone = new(this, offHandSkelePtr, pose, b, i, SkeletonType.Offhand);

                        skeleton.boneList.Add(tempBone);
                    }

                    ohSkeletons.Add(skeleton);
                }
            }

            SetUpAttachedSkeletons();

            SkeletonOverlay.Reset();
            isComplete = true;
        }

        private void SetUpAttachedSkeletons()
        {
            var charData = (ExtendedCharStruct*)mainSkelePtr->Owner;

            var attach = &charData->Attach;

            if(attach->AttachmentCount > 0)
            {
                IllusioDebug.Log("Found Attachments", LogType.Debug);
                
                var attachedPtr = attach->Parent;

                if (attachedPtr != null)
                {
                    Skeleton* attachedSkeleton = attach->Type switch
                    {
                        AttachType.CharacterBase => ((ExtendedCharStruct*)attachedPtr)->CharacterBase.Skeleton,
                        AttachType.Skeleton => (Skeleton*)attachedPtr,
                        _ => null
                    };

                    if (attachedSkeleton != null)
                    {
                        for (var i = 0; i < mainSkelePtr->PartialSkeletonCount; i++)
                        {
                            illusioPartialSkeleton skeleton = new();

                            var currentPSkele = mainSkelePtr->PartialSkeletons[i];

                            skeleton.partialIDX = i;

                            skeleton.connectedBoneIDX = currentPSkele.ConnectedParentBoneIndex;

                            var pose = currentPSkele.GetHavokPose(0);

                            if (pose == null)
                            {
                                skeletons.Add(skeleton);
                                continue;
                            }

                            for (var b = 0; b < pose->Skeleton->Bones.Length; b++)
                            {
                                IllusioBone tempBone = new(this, mainSkelePtr, pose, b, i, SkeletonType.Main);

                                skeleton.boneList.Add(tempBone);
                            }

                            skeletons.Add(skeleton);
                        }
                    }
                }
            }

            
        }

        public void ApplySkeletonPose(PoseFile poseFile, bool OnlyBase = false)
        {
            if (OnlyBase)
            {
                foreach (var bone in skeletons[0].boneList)
                {
                    if (poseFile.Bones.TryGetValue(bone.Name, out var result))
                    {
                        Transform newTransform = new()
                        {
                            Position = result.Position,
                            Rotation = result.Rotation,
                            Scale = result.Scale,
                        };

                        bone.SetTransformData(newTransform, MirrorModes.None, false);
                    }
                }

                for(var i = 1 ; i < skeletons.Count; i++)
                {
                    if (skeletons[i].boneList.Count > 0)
                    {
                        if (poseFile.Bones.TryGetValue(skeletons[i].boneList[0].Name, out var result))
                        {
                            Transform newTransform = new()
                            {
                                Position = result.Position,
                                Rotation = result.Rotation,
                                Scale = result.Scale,
                            };

                            skeletons[i].boneList[0].SetTransformData(newTransform, MirrorModes.None, true);
                        }
                    }
                    
                }
            }
            else
            {
                foreach (var skeleton in skeletons)
                {
                    foreach (var bone in skeleton.boneList)
                    {
                        if (poseFile.Bones.TryGetValue(bone.Name, out var result))
                        {
                            Transform newTransform = new()
                            {
                                Position = result.Position,
                                Rotation = result.Rotation,
                                Scale = result.Scale,
                            };

                            bone.SetTransformData(newTransform, MirrorModes.None, false);
                        }
                    }
                }
            }
        }

        public PoseFile SaveSkeletonPose()
        {
            PoseFile poseFile = new PoseFile();

            foreach (var skeleton in skeletons)
            {
                foreach (var bone in skeleton.boneList)
                {
                    if (skeleton.partialIDX == 0 && bone.Name == "j_ago") continue;

                    if (!poseFile.Bones.ContainsKey(bone.Name))
                        poseFile.Bones.Add(bone.Name, bone.GetTransformData());
                }
            }

            return poseFile;
        }

        public void ResetSkeleton()
        {
            foreach (var skeleton in skeletons)
            {
                foreach (var bone in skeleton.boneList)
                {
                    bone.Reset();
                }
            }

            foreach (var skeleton in mhSkeletons)
            {
                foreach (var bone in skeleton.boneList)
                {
                    bone.Reset();
                }
            }

            foreach (var skeleton in ohSkeletons)
            {
                foreach (var bone in skeleton.boneList)
                {
                    bone.Reset();
                }
            }
        }
    }

    public enum SkeletonType { Main, Mainhand, Offhand }

    public struct illusioPartialSkeleton
    {
        public List<IllusioBone> boneList;
        public int partialIDX;
        public int connectedBoneIDX;

        public illusioPartialSkeleton()
        {
            boneList = new List<IllusioBone>();
        }
    }
}
