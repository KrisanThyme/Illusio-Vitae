using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using Dalamud.Game.Network.Structures.InfoProxy;
using CustomizeData = FFXIVClientStructs.FFXIV.Client.Game.Character.CustomizeData;
using System.IO;
using System.Collections;
using System.Runtime.CompilerServices;
using IVPlugin.ActorData;
using System.Numerics;
using Lumina.Excel.GeneratedSheets;
using IVPlugin.Resources;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.Havok;
using IVPlugin.ActorData.Structs;
using Dalamud.Game.ClientState.Objects.Enums;
using static FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.VertexShader;
using IVPlugin.Actors.Structs;
using IVPlugin.Camera.Struct;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;
using FFXIVClientStructs.Havok.Common.Base.Math.Quaternion;
using Newtonsoft.Json.Serialization;
using FFXIVClientStructs.FFXIV.Client.Game;
using IVPlugin.Mods.Structs;

namespace IVPlugin.Core.Extentions
{
    public static class ExtentionMethods
    {

        public const float DegreesToRadians = MathF.PI / 180.0f;
        public const float RadiansToDegrees = 180.0f / MathF.PI;

        public static RaceCodes GetRaceCode(Races race, Genders gender, Tribes tribe)
        {
            switch (race)
            {
                case Races.Hyur:
                    switch (tribe)
                    {
                        case Tribes.Midlander:
                            if (gender == Genders.Masculine) return RaceCodes.C0101;
                            else return RaceCodes.C0201;
                        case Tribes.Highlander:
                            if (gender == Genders.Masculine) return RaceCodes.C0301;
                            else return RaceCodes.C0401;
                    };
                    break;
                case Races.Elezen:
                    if (gender == Genders.Masculine) return RaceCodes.C0501;
                    else return RaceCodes.C0601;
                case Races.Miqote:
                    if (gender == Genders.Masculine) return RaceCodes.C0701;
                    else return RaceCodes.C0801;
                case Races.Roegadyn:
                    if (gender == Genders.Masculine) return RaceCodes.C0901;
                    else return RaceCodes.C1001;
                case Races.Lalafel:
                    if (gender == Genders.Masculine) return RaceCodes.C1101;
                    else return RaceCodes.C1201;
                case Races.AuRa:
                    if (gender == Genders.Masculine) return RaceCodes.C1301;
                    else return RaceCodes.C1401;
                case Races.Hrothgar:
                    if (gender == Genders.Masculine) return RaceCodes.C1501;
                    else return RaceCodes.C1601;
                case Races.Viera:
                    if (gender == Genders.Masculine) return RaceCodes.C1701;
                    else return RaceCodes.C1801;
            }

            return RaceCodes.C0101;
        }
        public static string Captialize(this string s)
        {
            if (s == null) return "";

            var data = s.Split(' ');

            string finalString = "";

            for(var i = 0; i < data.Length; i++)
            {
                var sData = data[i];

                if (sData.Length < 2) continue;

                sData = string.Concat(sData[0].ToString().ToUpper(), sData.AsSpan(1));

                if (i != 0) finalString += " ";

                finalString += sData;
            }


            return finalString;
        }

        public unsafe static ObjectStruct* Base(this IGameObject go)
        {
            return (ObjectStruct*)go.Address;
        }

        public unsafe static CharacterStruct* Base(this ICharacter character)
        {
            return (CharacterStruct*)character.Address;
        }

        public static Vector3 RotatePosition(this Quaternion left, Vector3 right)
        {
            float num = left.X * 2f;
            float num2 = left.Y * 2f;
            float num3 = left.Z * 2f;
            float num4 = left.X * num;
            float num5 = left.Y * num2;
            float num6 = left.Z * num3;
            float num7 = left.X * num2;
            float num8 = left.X * num3;
            float num9 = left.Y * num3;
            float num10 = left.W * num;
            float num11 = left.W * num2;
            float num12 = left.W * num3;
            float x = ((1f - (num5 + num6)) * right.X) + ((num7 - num12) * right.Y) + ((num8 + num11) * right.Z);
            float y = ((num7 + num12) * right.X) + ((1f - (num4 + num6)) * right.Y) + ((num9 - num10) * right.Z);
            float z = ((num8 - num11) * right.X) + ((num9 + num10) * right.Y) + ((1f - (num4 + num5)) * right.Z);
            return new Vector3(x, y, z);
        }

        public static ActorEquipSlot GetEquipSlots(this EquipSlotCategory category)
        {
            ActorEquipSlot result = ActorEquipSlot.None;

            if (category.MainHand == 1)
                result |= ActorEquipSlot.MainHand;

            if (category.OffHand == 1)
                result |= ActorEquipSlot.OffHand;

            if (category.Head == 1)
                result |= ActorEquipSlot.Head;

            if (category.Body == 1)
                result |= ActorEquipSlot.Body;

            if (category.Gloves == 1)
                result |= ActorEquipSlot.Hands;

            if (category.Legs == 1)
                result |= ActorEquipSlot.Legs;

            if (category.Feet == 1)
                result |= ActorEquipSlot.Feet;

            if (category.Ears == 1)
                result |= ActorEquipSlot.Ears;

            if (category.Neck == 1)
                result |= ActorEquipSlot.Neck;

            if (category.Wrists == 1)
                result |= ActorEquipSlot.Wrists;

            if (category.FingerR == 1)
                result |= ActorEquipSlot.RightRing;

            if (category.FingerL == 1)
                result |= ActorEquipSlot.LeftRing;

            return result;
        }

        public static Vector3 ToVector3(this hkVector4f vec) => new Vector3(vec.X, vec.Y, vec.Z);
        public static Vector4 ToVector4(this hkVector4f vec) => new Vector4(vec.X, vec.Y, vec.Z, vec.W);
        public static Quaternion ToQuat(this hkQuaternionf q) => new Quaternion(q.X, q.Y, q.Z, q.W);

        public unsafe static Matrix4x4 GetProjectionMatrix(this XIVCameraStruct camera)
        {
            var cam = camera.Camera.CameraBase.SceneCamera.RenderCamera;
            var proj = cam->ProjectionMatrix;

            var far = cam->FarPlane;
            var near = cam->NearPlane;
            var clip = far / (far - near);
            proj.M43 = -(clip * near);
            proj.M33 = -((far + near) / (far - near));

            return proj;
        }

        public static Matrix4x4 ToMatrix(this Transform transform)
        {
            Matrix4x4 mat = Matrix4x4.Identity;

            mat *= Matrix4x4.CreateScale(transform.Scale);

            Quaternion normalizedRotation = Quaternion.Normalize(transform.Rotation);
            mat *= Matrix4x4.CreateFromQuaternion(normalizedRotation);

            mat.M41 = transform.Position.X;
            mat.M42 = transform.Position.Y;
            mat.M43 = transform.Position.Z;

            return mat;
        }

        public static Quaternion ToQuaternion(this Vector3 euler)
        {
            euler *= DegreesToRadians;
            Quaternion quaternion = Quaternion.CreateFromYawPitchRoll(euler.X, euler.Y, euler.Z);
            return Quaternion.Normalize(quaternion);
        }

        public static Vector3 ToEuler(this Quaternion r)
        {
            float yaw = MathF.Atan2(2.0f * (r.Y * r.W + r.X * r.Z), 1.0f - 2.0f * (r.X * r.X + r.Y * r.Y));
            float pitch = MathF.Asin(2.0f * (r.X * r.W - r.Y * r.Z));
            float roll = MathF.Atan2(2.0f * (r.X * r.Y + r.Z * r.W), 1.0f - 2.0f * (r.X * r.X + r.Z * r.Z));

            return new Vector3(yaw, pitch, roll) * RadiansToDegrees;
        }

        public static Quaternion GetQuaternion(this Transform transform)
        {
            return new() { X = transform.Rotation.X, Y = transform.Rotation.Y, Z = transform.Rotation.Z, W = transform.Rotation.W};
        }

        public static SceneTransform toSceneTransform(this Transform transform)
        {
            return new()
            {
                position = transform.Position,
                rotation = transform.Rotation,
                scale = transform.Scale,
            };
        }

        public static Transform ToTransform(this Matrix4x4 matrix)
        {
            Vector3 position = matrix.Translation;

            Vector3 scale = new(
                new Vector3(matrix.M11, matrix.M12, matrix.M13).Length(),
                new Vector3(matrix.M21, matrix.M22, matrix.M23).Length(),
                new Vector3(matrix.M31, matrix.M32, matrix.M33).Length()
            );

            scale.X = Math.Abs(scale.X) < float.Epsilon ? 0.01f : scale.X;
            scale.Y = Math.Abs(scale.Y) < float.Epsilon ? 0.01f : scale.Y;
            scale.Z = Math.Abs(scale.Z) < float.Epsilon ? 0.01f : scale.Z;

            Matrix4x4 rotationMatrix = new Matrix4x4(
                matrix.M11 / scale.X, matrix.M12 / scale.X, matrix.M13 / scale.X, 0,
                matrix.M21 / scale.Y, matrix.M22 / scale.Y, matrix.M23 / scale.Y, 0,
                matrix.M31 / scale.Z, matrix.M32 / scale.Z, matrix.M33 / scale.Z, 0,
                0, 0, 0, 1
            );

            Quaternion rotation = Quaternion.CreateFromRotationMatrix(rotationMatrix);

            Transform decomposedTransform = new()
            {
                Position = position,
                Rotation = rotation,
                Scale = scale
            };

            return decomposedTransform;
        }
    
        public static CustomizeStruct ToCharacterStruct(this BNpcCustomize customize)
        {
            CustomizeStruct customizeStruct = new CustomizeStruct()
            {
                Race = (Races)customize.Race.Row,
                Gender = (Genders)customize.Gender,
                Age = (Age)customize.BodyType,
                Height = customize.Height,
                Tribe = (Tribes)customize.Tribe.Row,
                FaceType = customize.Face,
                HairStyle = customize.HairStyle,
                HasHighlights = customize.HairHighlight,
                SkinTone = customize.SkinColor,
                REyeColor = customize.EyeColor,
                LEyeColor = customize.EyeColor,
                HairColor = customize.HairColor,
                HairHighlightColor = customize.HairHighlightColor,
                FaceFeaturesColor = customize.FacialFeatureColor,
                Eyebrows = customize.Eyebrows,
                EyeShape = customize.EyeShape,
                NoseShape = customize.Nose,
                JawShape = customize.Jaw,
                LipStyle = customize.Mouth,
                LipColor = customize.LipColor,
                BustSize = customize.BustOrTone1,
                RaceFeatureSize = customize.ExtraFeature2OrBust, 
                RaceFeatureType = customize.ExtraFeature1,
                Facepaint = customize.FacePaint,
                FacePaintColor =customize.FacePaintColor
            };

            unsafe
            {
                customizeStruct.Data[(int)CustomizeIndex.FaceFeatures] = customize.FacialFeature;
            }
            
            return customizeStruct;
        }

        public static CustomizeStruct ToCharacterStruct(this ENpcBase customize)
        {
            CustomizeStruct customizeStruct = new CustomizeStruct()
            {
                Race = (Races)customize.Race.Row,
                Gender = (Genders)customize.Gender,
                Age = (Age)customize.BodyType,
                Height = customize.Height,
                Tribe = (Tribes)customize.Tribe.Row,
                FaceType = customize.Face,
                HairStyle = customize.HairStyle,
                HasHighlights = customize.HairHighlight,
                SkinTone = customize.SkinColor,
                REyeColor = customize.EyeColor,
                LEyeColor = customize.EyeColor,
                HairColor = customize.HairColor,
                HairHighlightColor = customize.HairHighlightColor,
                FaceFeaturesColor = customize.FacialFeatureColor,
                Eyebrows = customize.Eyebrows,
                EyeShape = customize.EyeShape,
                NoseShape = customize.Nose,
                JawShape = customize.Jaw,
                LipStyle = customize.Mouth,
                LipColor = customize.LipColor,
                BustSize = customize.BustOrTone1,
                RaceFeatureSize = customize.ExtraFeature2OrBust, 
                RaceFeatureType = customize.ExtraFeature1,
                Facepaint = customize.FacePaint,
                FacePaintColor = customize.FacePaintColor
            };

            unsafe
            {
                customizeStruct.Data[(int)CustomizeIndex.FaceFeatures] = customize.FacialFeature;
            }

            return customizeStruct;
        }

        public static Transform CalculateDiff(this Transform a, Transform b)
        {
            return new Transform()
            {
                Position = b.Position - a.Position,
                Rotation = Quaternion.Normalize(Quaternion.Multiply(Quaternion.Conjugate(a.Rotation), b.Rotation)),
                Scale = b.Scale - a.Scale
            };
        }

        public static bool ValidWeapon(this FFXIVClientStructs.FFXIV.Client.Game.Character.WeaponModelId modelID, ActorEquipSlot slot, uint currentJob)
        {
            var model = GameResourceManager.Instance.EquipmentData.GetModelById(modelID, slot);

            if (model == null) return false;

            return ValidateJob(model.classJob, currentJob);


        }

        public static unsafe bool ValidateJob(ClassJobCategory job, uint currentJob)
        {
            switch (currentJob)
            {
                case 0:
                    return job.ADV;
                case 1:
                    return job.GLA;
                case 2:
                    return job.PGL;
                case 3:
                    return job.MRD;
                case 4:
                    return job.LNC;
                case 5:
                    return job.ARC;
                case 6:
                    return job.CNJ;
                case 7:
                    return job.THM;
                case 8:
                    return job.CRP;
                case 9:
                    return job.BSM;
                case 10:
                    return job.ARM;
                case 11:
                    return job.GSM;
                case 12:
                    return job.LTW;
                case 13:
                    return job.WVR;
                case 14:
                    return job.ALC;
                case 15:
                    return job.CUL;
                case 16:
                    return job.MIN;
                case 17:
                    return job.BTN;
                case 18:
                    return job.FSH;
                case 19:
                    return job.PLD;
                case 20:
                    return job.MNK;
                case 21:
                    return job.WAR;
                case 22:
                    return job.DRG;
                case 23:
                    return job.BRD;
                case 24:
                    return job.WHM;
                case 25:
                    return job.BLM;
                case 26:
                    return job.ARC;
                case 27:
                    return job.SMN;
                case 28:
                    return job.SCH;
                case 29:
                    return job.ROG;
                case 30:
                    return job.NIN;
                case 31:
                    return job.MCH;
                case 32:
                    return job.DRK;
                case 33:
                    return job.AST;
                case 34:
                    return job.SAM;
                case 35:
                    return job.RDM;
                case 36:
                    return job.BLU;
                case 37:
                    return job.GNB;
                case 38:
                    return job.DNC;
                case 39:
                    return job.RPR;
                case 40:
                    return job.SGE;
                case 41:
                    return job.VPR;
                case 42:
                    return job.PCT;
            }

            return false;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Quat
    {
        public float X;
        public float Z;
        public float Y;
        public float W;

        public static implicit operator Vector4(Quat pos) => new(pos.X, pos.Y, pos.Z, pos.W);

        //public static implicit operator SharpDX.Vector4(Quat pos) => new(pos.X, pos.Z, pos.Y, pos.W);
    }
}
