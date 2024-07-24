using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;
using FFXIVClientStructs.Havok.Animation.Mapper;
using FFXIVClientStructs.Havok.Animation.Rig;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using FFXIVClientStructs.Havok.Common.Base.Math.Quaternion;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;
using IVPlugin.Actors.Structs;
using IVPlugin.Core.Extentions;
using IVPlugin.Log;
using IVPlugin.Posing;
using IVPlugin.Services;
using IVPlugin.UI.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static FFXIVClientStructs.Havok.Animation.Rig.hkaPose;
using static IVPlugin.Core.Files.PoseFile;

namespace IVPlugin.Actors.SkeletonData
{
    public unsafe class IllusioBone
    {
        public IllusioSkeleton illusioSkeleton;

        public Skeleton* skelePtr;
        public int idx { get; private set; }
        public int partialIDX { get; private set; }
        public int parentIDX => GetParent();
        public SkeletonType boneType { get; private set; }
        public unsafe string Name => GetName();

        public bool isDirty { get; private set; } = false;
        public PropagateOrNot propagate { get; set; }

        public Transform newTransform;

        public Transform originalTransform;

        public unsafe hkaPose* havokPose { get; private set; }

        public IllusioBone(IllusioSkeleton skeleManager, Skeleton* skele, hkaPose* pose, int boneIDX, int partialIDX, SkeletonType type)
        {
            illusioSkeleton = skeleManager;
            skelePtr = skele;
            idx = boneIDX;
            this.partialIDX = partialIDX;
            this.boneType = type;

            havokPose = pose;
        }

        private unsafe string GetName()
        {
            if (idx < havokPose->Skeleton->Bones.Length) return havokPose->Skeleton->Bones[idx].Name.String ?? "unknown";
            else return "unknown";
        }

        private unsafe int GetParent()
        {
            if(havokPose != null)
            {
                if(havokPose->Skeleton->Bones.Length > 0 && havokPose->Skeleton->ParentIndices.Length > 0)
                {
                    return havokPose->Skeleton->ParentIndices[idx];
                }
            }

            return 0;
            
        }

        public void Reset()
        {
            newTransform = new();

            if(isDirty)
            {
                if (havokPose != null && havokPose->Skeleton != null)
                {
                    var modelSpace = havokPose->AccessBoneModelSpace(idx, propagate);

                    var translation = new hkVector4f() { X = originalTransform.Position.X, Y = originalTransform.Position.Y, Z = originalTransform.Position.Z };
                    var rotation = new hkQuaternionf() { X = originalTransform.Rotation.X, Y = originalTransform.Rotation.Y, Z = originalTransform.Rotation.Z, W = originalTransform.Rotation.W };
                    var scale = new hkVector4f() { X = originalTransform.Scale.X, Y = originalTransform.Scale.Y, Z = originalTransform.Scale.Z };

                    modelSpace->Translation = translation;

                    modelSpace->Rotation = rotation;
                    modelSpace->Scale = scale;
                }
            }

            isDirty = false;
        }

        public unsafe Vector3 GetLocalPos()
        {
            if (havokPose == null) return new();
            if (havokPose->Skeleton == null) return new();

            var translation = havokPose->AccessBoneModelSpace(idx, PropagateOrNot.DontPropagate)->Translation;

            return new Vector3(translation.X, translation.Y, translation.Z);
        }

        public unsafe Vector3 GetWorldPos()
        {
            try
            {
                var charaBase = (ExtendedCharStruct*)skelePtr->Owner;

                if (charaBase == null) return new();

                if (&charaBase->CharacterBase.DrawObject.Object == null) return new();

                var modelMatrix = new Transform()
                {
                    Position = (Vector3)charaBase->CharacterBase.DrawObject.Object.Position,
                    Rotation = (Quaternion)charaBase->CharacterBase.DrawObject.Object.Rotation,
                    Scale = (Vector3)charaBase->CharacterBase.DrawObject.Object.Scale * charaBase->ScaleFactor,
                }.ToMatrix();

                var bonePos = GetLocalPos();

                return Vector3.Transform(bonePos, modelMatrix);
            }
            catch(Exception e)
            {
                IllusioDebug.Log("Bad bone", LogType.Warning);
                SkeletonOverlay.Reset();

                return new();
            }
        }

        public unsafe Transform GetTransformData()
        {
            if (havokPose == null) return new();

            if(havokPose->Skeleton == null) return new();

            var translation = havokPose->AccessBoneModelSpace(idx, PropagateOrNot.DontPropagate);

            var temp = new Transform()
            {
                Position = new(translation->Translation.X, translation->Translation.Y, translation->Translation.Z),
                Rotation = new(translation->Rotation.X, translation->Rotation.Y, translation->Rotation.Z, translation->Rotation.W),
                Scale = new(translation->Scale.X, translation->Scale.Y, translation->Scale.Z)
            };

            return temp;
        }

        public void SetTransformData(Transform transform, MirrorModes mirror, bool chain)
        {
            propagate = chain ? PropagateOrNot.Propagate : PropagateOrNot.DontPropagate;

            if (mirror != MirrorModes.None)
            {
                if (Name.EndsWith("_r") || Name.EndsWith("_l"))
                {
                    var mBoneName = GetName();

                    if (mBoneName.EndsWith("_r"))
                    {
                        mBoneName = mBoneName.Replace("_r", "_l");
                    }
                    else
                    {
                        mBoneName = mBoneName.Replace("_l", "_r");
                    }

                    if (illusioSkeleton.GetBoneByName(mBoneName, partialIDX, boneType, out var mirrorBone))
                    {
                        var offset = CalculateDiff(transform);

                        var boneTransform = mirrorBone.GetTransformData();

                        var rot = Quaternion.Conjugate(offset.GetQuaternion());

                        switch (mirror)
                        {
                            case MirrorModes.Full:
                                //do nothing
                                break;
                            case MirrorModes.Copy:
                                offset.Position = -offset.Position;
                                offset.Rotation = -offset.Rotation;
                                break;
                            case MirrorModes.MirrorX:
                                offset.Position.Y = -offset.Position.Y;
                                offset.Position.Z = -offset.Position.Z;
                                break;
                            case MirrorModes.MirrorY:
                                offset.Position.X = -offset.Position.X;
                                offset.Position.Z = -offset.Position.Z;
                                break;
                            case MirrorModes.MirrorZ:
                                offset.Position.X = -offset.Position.X;
                                offset.Position.Y = -offset.Position.Y;
                                break;
                        }

                        boneTransform.Position += offset.Position;
                        boneTransform.Rotation *= new FFXIVClientStructs.FFXIV.Common.Math.Quaternion(rot.X, rot.Y, rot.Z, rot.W);
                        boneTransform.Scale += offset.Scale;

                        illusioSkeleton.UpdateBone(boneTransform, mirrorBone, boneType, MirrorModes.None, chain);
                    }
                    else
                    {
                        IllusioDebug.Log($"Unable to find bone {mBoneName}", LogType.Warning);
                    }
                }
            }

            newTransform = transform;

            if (!isDirty)
                originalTransform = GetTransformData();

            isDirty = true;

            if (propagate == PropagateOrNot.Propagate)
            {
                PropagateChildren(havokPose->AccessBoneModelSpace(idx, PropagateOrNot.DontPropagate), newTransform.Position, newTransform.Rotation);
            }
        }

        public unsafe void ApplyTransformdata()
        {
            try
            {
                if(havokPose != null && havokPose->Skeleton != null)
                {
                    var modelSpace = havokPose->AccessBoneModelSpace(idx, PropagateOrNot.DontPropagate);

                    

                    var translation = new hkVector4f() { X = newTransform.Position.X, Y = newTransform.Position.Y, Z = newTransform.Position.Z };
                    var rotation = new hkQuaternionf() { X = newTransform.Rotation.X, Y = newTransform.Rotation.Y, Z = newTransform.Rotation.Z, W = newTransform.Rotation.W };
                    var scale = new hkVector4f() { X = newTransform.Scale.X, Y = newTransform.Scale.Y, Z = newTransform.Scale.Z };

                    modelSpace->Translation = translation;

                    modelSpace->Rotation = rotation;
                    modelSpace->Scale = scale;
                }
            }
            catch (Exception e)
            {
                IllusioDebug.Log($"{e} uh-oh", LogType.Error, false);
            }
        }

        private Transform CalculateDiff(Transform other)
        {
            var boneTransform = GetTransformData();

            return new Transform()
            {
                Position = boneTransform.Position - other.Position,
                Rotation = Quaternion.Normalize(Quaternion.Conjugate(other.GetQuaternion()) * boneTransform.GetQuaternion()),
                Scale = boneTransform.Scale - other.Scale
            };
        }

        public unsafe List<IllusioBone> GetChildren(bool includePartials = true, bool usePartialRoot = false)
        {
            var result = new List<IllusioBone>();

            if (havokPose == null || havokPose->Skeleton == null)
                return result;

            for (var i = idx + 1; i < havokPose->Skeleton->ParentIndices.Length; i++)
            {
                var child = new IllusioBone(illusioSkeleton, skelePtr, havokPose, i, partialIDX, boneType);
                if (child.parentIDX != idx) continue;
                result.Add(child);
                result.AddRange(child.GetChildren(includePartials, usePartialRoot));
            }

            if (includePartials && partialIDX == 0)
            {
                for (var p = 0; p < skelePtr->PartialSkeletonCount; p++)
                {
                    if (p == partialIDX) continue;
                    var partial = skelePtr->PartialSkeletons[p];
                    if (partial.ConnectedParentBoneIndex == idx)
                    {
                        var partialRoot = new IllusioBone(illusioSkeleton, skelePtr, skelePtr->PartialSkeletons[p].GetHavokPose(0), 0, p, boneType);
                        if (usePartialRoot)
                        {
                            result.Add(partialRoot);
                            var children = partialRoot.GetChildren();
                            foreach (var child in children)
                            {
                                result.Add(child);
                            }
                        }
                        else
                        {
                            var children = partialRoot.GetChildren();
                            foreach (var child in children)
                            {
                                result.Add(child);
                            }
                                
                        }
                    }
                }
            }

            return result;
        }

        public unsafe void PropagateChildren(hkQsTransformf* transform, Vector3 initialPos, Quaternion initialRot, bool includePartials = true)
        {
            var sourcePos = transform->Translation.ToVector3();
            var deltaRot = transform->Rotation.ToQuat() / initialRot;
            var deltaPos = sourcePos - initialPos;

            deltaPos = -deltaPos;
            deltaRot = Quaternion.Inverse(deltaRot);

            var descendants = GetChildren(includePartials, usePartialRoot: true);

            foreach (var child in descendants)
            {
                if(illusioSkeleton.GetBoneByIDX(child.idx, child.partialIDX, child.boneType, out var childBone))
                {
                    var childTransform = childBone.GetTransformData();

                    var offset = Vector3.Subtract(childTransform.Position, sourcePos);
                    offset = Vector3.Transform(offset, deltaRot);

                    var matrix = child.GetTransformData().ToMatrix();
                    matrix *= Matrix4x4.CreateFromQuaternion(-deltaRot);
                    matrix.Translation = deltaPos + sourcePos + offset;

                    childBone.SetTransformData(matrix.ToTransform(), MirrorModes.None, false);
                }
            }
        }
    }

    [Flags]
    public enum MirrorModes
    {
        None = 0,
        Copy = 1,
        Full = 2,
        MirrorX = 4,
        MirrorY = 8,
        MirrorZ = 16,
    }
}
