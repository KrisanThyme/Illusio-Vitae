using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Component.GUI;
using IVPlugin.ActorData;
using IVPlugin.ActorData.Structs;
using IVPlugin.Actors.Structs;
using IVPlugin.Core.Extentions;
using IVPlugin.Log;
using IVPlugin.Services;
using Lumina.Data.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer;
using StructsCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace IVPlugin.Core.Files
{
    public class CharaFile
    {
        [Flags]
        public enum SaveModes
        {
            None = 0,

            EquipmentGear = 1,
            EquipmentAccessories = 2,
            EquipmentWeapons = 4,
            AppearanceHair = 8,
            AppearanceFace = 16,
            AppearanceBody = 32,
            AppearanceExtended = 64,

            Equipment = EquipmentGear | EquipmentAccessories,
            Appearance = AppearanceHair | AppearanceFace | AppearanceBody | AppearanceExtended,

            All = EquipmentGear | EquipmentAccessories | EquipmentWeapons | AppearanceHair | AppearanceFace | AppearanceBody | AppearanceExtended
        }

        public string FileExtension { get; set; } = ".chara";
        public string TypeName { get; set; } = "Illusio Vitae/Character Files";

        public const int CurrentVersion = 1;

        public int FileVersion { get; set; } = 1;

        public SaveModes SaveMode { get; set; } = SaveModes.All;

        public string? Nickname { get; set; } = null;
        public uint ModelType { get; set; } = 0;
        public ObjectKind ObjectKind { get; set; } = ObjectKind.None;

        // appearance
        public Races? Race { get; set; }
        public Genders? Gender { get; set; }
        public Age? Age { get; set; }
        public byte? Height { get; set; }
        public Tribes? Tribe { get; set; }
        public byte? Head { get; set; }
        public byte? Hair { get; set; }
        public bool? EnableHighlights { get; set; }
        public byte? Skintone { get; set; }
        public byte? REyeColor { get; set; }
        public byte? HairTone { get; set; }
        public byte? Highlights { get; set; }
        public FacialFeatures? FacialFeatures { get; set; }
        public byte? LimbalEyes { get; set; } // facial feature color
        public byte? Eyebrows { get; set; }
        public byte? LEyeColor { get; set; }
        public byte? Eyes { get; set; }
        public byte? Nose { get; set; }
        public byte? Jaw { get; set; }
        public byte? Mouth { get; set; }
        public byte? LipsToneFurPattern { get; set; }
        public byte? EarMuscleTailSize { get; set; }
        public byte? TailEarsType { get; set; }
        public byte? Bust { get; set; }
        public byte? FacePaint { get; set; }
        public byte? FacePaintColor { get; set; }

        public ushort? Glasses { get; set; }

        // weapons
        public WeaponSave? MainHand { get; set; }
        public WeaponSave? OffHand { get; set; }

        // equipment
        public ItemSave? HeadGear { get; set; }
        public ItemSave? Body { get; set; }
        public ItemSave? Hands { get; set; }
        public ItemSave? Legs { get; set; }
        public ItemSave? Feet { get; set; }
        public ItemSave? Ears { get; set; }
        public ItemSave? Neck { get; set; }
        public ItemSave? Wrists { get; set; }
        public ItemSave? LeftRing { get; set; }
        public ItemSave? RightRing { get; set; }

        // extended appearance
        // NOTE: extended weapon values are stored in the WeaponSave
        public Vector3? SkinColor { get; set; }
        public Vector3? SkinGloss { get; set; }
        public Vector3? LeftEyeColor { get; set; }
        public Vector3? RightEyeColor { get; set; }
        public Vector3? LimbalRingColor { get; set; }
        public Vector3? HairColor { get; set; }
        public Vector3? HairGloss { get; set; }
        public Vector3? HairHighlight { get; set; }
        public Vector4? MouthColor { get; set; }
        public Vector3? BustScale { get; set; }
        public float? Transparency { get; set; }
        public float? MuscleTone { get; set; }
        public float? HeightMultiplier { get; set; }
        public ushort? Voice { get; set; }

        public void WriteToFile(XIVActor actor, SaveModes mode)
        {
            unsafe
            {
                ModelType = (uint)actor.GetModelType();
            }

            SaveMode = mode;

            var custom = actor.GetCustomizeData();

            if (IncludeSection(SaveModes.EquipmentWeapons, mode))
            {
                MainHand = new WeaponSave(actor.GetWeaponSlot(WeaponSlot.MainHand));

                OffHand = new WeaponSave(actor.GetWeaponSlot(WeaponSlot.OffHand));
            }

            if (IncludeSection(SaveModes.EquipmentGear, mode))
            {
                HeadGear = GetItemSave(actor, EquipmentSlot.Head);
                Body = GetItemSave(actor, EquipmentSlot.Body);
                Hands = GetItemSave(actor, EquipmentSlot.Hands);
                Legs = GetItemSave(actor, EquipmentSlot.Legs);
                Feet = GetItemSave(actor, EquipmentSlot.Feet);
            }

            if (IncludeSection(SaveModes.EquipmentAccessories, mode))
            {
                Ears = GetItemSave(actor, EquipmentSlot.Ears);
                Neck = GetItemSave(actor, EquipmentSlot.Neck);
                Wrists = GetItemSave(actor, EquipmentSlot.Wrists);
                LeftRing = GetItemSave(actor, EquipmentSlot.LFinger);
                RightRing = GetItemSave(actor, EquipmentSlot.RFinger);
                Glasses = actor.GetFacewear(0);
            }

            if (IncludeSection(SaveModes.AppearanceHair, mode))
            {
                Hair = custom.HairStyle;
                EnableHighlights = custom.HighlightsEnabled;
                HairTone = custom.HairColor;
                Highlights = custom.HairHighlightColor;
            }

            if (IncludeSection(SaveModes.AppearanceFace, mode) || IncludeSection(SaveModes.AppearanceBody, mode))
            {
                Race = custom.Race;
                Gender = custom.Gender;
                Tribe = custom.Tribe;
                Age = custom.Age;
            }

            if (IncludeSection(SaveModes.AppearanceFace, mode))
            {
                Head = custom.FaceType;
                REyeColor = custom.REyeColor;
                LimbalEyes = custom.FaceFeaturesColor;
                FacialFeatures = custom.FaceFeatures;
                Eyebrows = custom.Eyebrows;
                LEyeColor = custom.LEyeColor;
                Eyes = custom.EyeShape;
                Nose = custom.NoseShape;
                Jaw = custom.JawShape;
                Mouth = custom.LipStyle;
                LipsToneFurPattern = custom.LipColor;
                FacePaint = custom.Facepaint;
                FacePaintColor = custom.FacePaintColor;
            }

            if (IncludeSection(SaveModes.AppearanceBody, mode))
            {
                Height = custom.Height;
                Skintone = custom.SkinTone;
                EarMuscleTailSize = custom.RaceFeatureSize;
                TailEarsType = custom.RaceFeatureType;
                Bust = custom.BustSize;

                HeightMultiplier = actor.GetActorScale();

                Transparency = actor.GetTransparency();
            }

            if(IncludeSection(SaveModes.AppearanceExtended, mode))
            {
                if (actor.GetShaderParams(out var shader))
                {
                    SkinColor = shader.SkinColor;
                    SkinGloss = shader.SkinGloss;
                    LeftEyeColor = shader.LeftEyeColor;
                    RightEyeColor = shader.RightEyeColor;
                    LimbalRingColor = shader.FeatureColor;
                    HairColor = shader.HairColor;
                    HairGloss = shader.HairGloss;
                    HairHighlight = shader.HairHighlight;
                    MouthColor = shader.MouthColor;
                    MuscleTone = shader.MuscleTone;
                    Voice = actor.GetVoice();
                }    
            }
        }

        private ItemSave GetItemSave(XIVActor actor, EquipmentSlot slot)
            => new ItemSave(actor.GetEquipmentSlot(slot));

        public unsafe void Apply(XIVActor actor, SaveModes mode)
        {
            if (IncludeSection(SaveModes.EquipmentWeapons, mode))
            {
                MainHand?.Write(actor, true);
                OffHand?.Write(actor, false);
            }

            if (IncludeSection(SaveModes.EquipmentGear, mode))
            {
                HeadGear?.Write(actor, EquipmentSlot.Head);
                Body?.Write(actor, EquipmentSlot.Body);
                Hands?.Write(actor, EquipmentSlot.Hands);
                Legs?.Write(actor, EquipmentSlot.Legs);
                Feet?.Write(actor, EquipmentSlot.Feet);
            }

            if (IncludeSection(SaveModes.EquipmentAccessories, mode))
            {
                Ears?.Write(actor, EquipmentSlot.Ears);
                Neck?.Write(actor, EquipmentSlot.Neck);
                Wrists?.Write(actor, EquipmentSlot.Wrists);
                RightRing?.Write(actor, EquipmentSlot.RFinger);
                LeftRing?.Write(actor, EquipmentSlot.LFinger);
                
                if(Glasses != null)
                    actor.SetFacewear(0, (ushort)Glasses);
            }

            var custom = actor.GetCustomizeData();

            if (IncludeSection(SaveModes.AppearanceHair, mode))
            {
                if (Hair != null)
                    custom.HairStyle = (byte)Hair;

                if (EnableHighlights != null)
                    custom.HasHighlights = (byte)((bool)EnableHighlights ? 0x80 : 0);

                if (HairTone != null)
                    custom.HairColor = (byte)HairTone;

                if (Highlights != null)
                    custom.HairHighlightColor = (byte)Highlights;
            }


            if (IncludeSection(SaveModes.AppearanceFace, mode) || IncludeSection(SaveModes.AppearanceBody, mode))
            {
                if (Race != null)
                    custom.Race = (Races)Race;

                if (Gender != null)
                    custom.Gender = (Genders)Gender;

                if (Tribe != null)
                    custom.Tribe = (Tribes)Tribe;

                if (Age != null)
                    custom.Age = (Age)Age;
            }

            if (IncludeSection(SaveModes.AppearanceFace, mode))
            {
                if (Head != null)
                    custom.FaceType = (byte)Head;

                if (REyeColor != null)
                    custom.REyeColor = (byte)REyeColor;

                if (FacialFeatures != null)
                    custom.FaceFeatures = (FacialFeatures)FacialFeatures;

                if (LimbalEyes != null)
                    custom.FaceFeaturesColor = (byte)LimbalEyes;

                if (Eyebrows != null)
                    custom.Eyebrows = (byte)Eyebrows;

                if (LEyeColor != null)
                    custom.LEyeColor = (byte)LEyeColor;

                if (Eyes != null)
                    custom.EyeShape = (byte)Eyes;

                if (Nose != null)
                    custom.NoseShape = (byte)Nose;

                if (Jaw != null)
                    custom.JawShape = (byte)Jaw;

                if (Mouth != null)
                    custom.LipStyle = (byte)Mouth;

                if (LipsToneFurPattern != null)
                    custom.LipColor = (byte)LipsToneFurPattern;

                if (FacePaint != null)
                    custom.Facepaint = (byte)FacePaint;

                if (FacePaintColor != null)
                    custom.FacePaintColor = (byte)FacePaintColor;
            }

            if (IncludeSection(SaveModes.AppearanceBody, mode))
            {
                if (Height != null)
                    custom.Height = (byte)Height;

                if (Skintone != null)
                    custom.SkinTone = (byte)Skintone;

                if (EarMuscleTailSize != null)
                    custom.RaceFeatureSize = (byte)EarMuscleTailSize;

                if (TailEarsType != null)
                    custom.RaceFeatureType = (byte)TailEarsType;

                if (Bust != null)
                    custom.BustSize = (byte)Bust;

                actor.SetTransparency(Transparency ?? 1.0f);
            }

            actor.SetModelType(ModelType);

            bool applyShaders = false;

            if (IncludeSection(SaveModes.AppearanceExtended, mode))
            {
                if(actor.GetShaderParams(out var shader))
                {
                    if (SkinColor != null)
                    {
                        shader.SkinColor = (Vector3)SkinColor;
                        actor.UpdateShaderLocks(shaderLockType.SkinColor, true);
                    }
                        
                    if (SkinGloss != null)
                    {
                        shader.SkinGloss = (Vector3)SkinGloss;
                        actor.UpdateShaderLocks(shaderLockType.SkinColor, true);
                    }
                    if (LeftEyeColor != null)
                    {
                        shader.LeftEyeColor = (Vector3)LeftEyeColor;
                        actor.UpdateShaderLocks(shaderLockType.LEyeColor, true);
                    }
                    if (RightEyeColor != null)
                    {
                        shader.RightEyeColor = (Vector3)RightEyeColor;
                        actor.UpdateShaderLocks(shaderLockType.REyeColor, true);
                    }
                    if (LimbalRingColor != null)
                    {
                        shader.FeatureColor = (Vector3)LimbalRingColor;
                        actor.UpdateShaderLocks(shaderLockType.FeatureColor, true);
                    }
                    if (HairColor != null)
                    {
                        shader.HairColor = (Vector3)HairColor;
                        actor.UpdateShaderLocks(shaderLockType.HairColor, true);
                    }
                    if (HairGloss != null)
                    {
                        shader.HairGloss = (Vector3)HairGloss;
                        actor.UpdateShaderLocks(shaderLockType.HairColor, true);
                    }
                    if (HairHighlight != null)
                    {
                        shader.HairHighlight = (Vector3)HairHighlight;
                        actor.UpdateShaderLocks(shaderLockType.HighlightColor, true);
                    }
                    if (MouthColor != null)
                    {
                        shader.MouthColor = (Vector4)MouthColor;
                        actor.UpdateShaderLocks(shaderLockType.LipColor, true);
                    }
                    if (MuscleTone != null)
                        shader.MuscleTone = (float)MuscleTone;
                    if (Voice != null)
                        actor.SetVoice((ushort)Voice);
                    if (HeightMultiplier != null)
                        actor.SetActorScale((float)HeightMultiplier);

                    actor.ApplyShaderparams(shader);

                    applyShaders = true;
                }          
            }

            if(mode.HasFlag(SaveModes.AppearanceFace) || mode.HasFlag(SaveModes.AppearanceBody))
                actor.ApplyCustomize(custom, ApplyShader: applyShaders);
        }

        public unsafe void ApplyRaw(StructsCharacter* chara, SaveModes mode, out ShaderParams shader)
        {
            GearPack gear = new();
            if (IncludeSection(SaveModes.EquipmentWeapons, mode))
            {
                if (MainHand != null)
                    chara->DrawData.LoadWeapon(WeaponSlot.MainHand, new() { Id = MainHand.ModelSet, Variant = MainHand.ModelVariant, Stain0 = MainHand.DyeId, Type = MainHand.ModelBase }, 0, 0, 0, 0); 
                if (OffHand != null)
                    chara->DrawData.LoadWeapon(WeaponSlot.OffHand, new() { Id = OffHand.ModelSet, Variant = OffHand.ModelVariant, Stain0 = OffHand.DyeId, Type = OffHand.ModelBase }, 0, 0, 0, 0);
                
            }

            if (IncludeSection(SaveModes.EquipmentGear, mode))
            {
                if (HeadGear != null)
                    chara->DrawData.Equipment(EquipmentSlot.Head) = new() { Id = HeadGear.ModelBase, Variant = HeadGear.ModelVariant, Stain0 = HeadGear.DyeId, Stain1 = HeadGear.DyeId2};
                if (Body != null)
                    chara->DrawData.Equipment(EquipmentSlot.Body) = new() { Id = Body.ModelBase, Variant = Body.ModelVariant, Stain0 = Body.DyeId, Stain1 = Body.DyeId2 };
                if (Hands != null)
                    chara->DrawData.Equipment(EquipmentSlot.Hands) = new() { Id = Hands.ModelBase, Variant = Hands.ModelVariant, Stain0 = Hands.DyeId, Stain1 = Hands.DyeId2 };
                if (Legs != null)
                    chara->DrawData.Equipment(EquipmentSlot.Legs) = new() { Id = Legs.ModelBase, Variant = Legs.ModelVariant, Stain0 = Legs.DyeId, Stain1 = Legs.DyeId2 };
                if (Feet != null)
                    chara->DrawData.Equipment(EquipmentSlot.Feet) = new() { Id = Feet.ModelBase, Variant = Feet.ModelVariant, Stain0 = Feet.DyeId, Stain1 = Feet.DyeId2 };
            }

            if (IncludeSection(SaveModes.EquipmentAccessories, mode))
            {
                if (Ears != null)
                    chara->DrawData.Equipment(EquipmentSlot.Ears) = new() { Id = Ears.ModelBase, Variant = Ears.ModelVariant, Stain0 = Ears.DyeId, Stain1 = Ears.DyeId2 };
                if (Neck != null)
                    chara->DrawData.Equipment(EquipmentSlot.Neck) = new() { Id = Neck.ModelBase, Variant = Neck.ModelVariant, Stain0 = Neck.DyeId, Stain1 = Neck.DyeId2 };
                if (Wrists != null)
                    chara->DrawData.Equipment(EquipmentSlot.Wrists) = new() { Id = Wrists.ModelBase, Variant = Wrists.ModelVariant, Stain0 = Wrists.DyeId, Stain1 = Wrists.DyeId2 };
                if (LeftRing != null)
                    chara->DrawData.Equipment(EquipmentSlot.LFinger) = new() { Id = LeftRing.ModelBase, Variant = LeftRing.ModelVariant, Stain0 = LeftRing.DyeId, Stain1 = LeftRing.DyeId2 };
                if (RightRing != null)
                    chara->DrawData.Equipment(EquipmentSlot.RFinger) = new() { Id = RightRing.ModelBase, Variant = RightRing.ModelVariant, Stain0 = RightRing.DyeId, Stain1 = RightRing.DyeId2 };

                if (Glasses != null)
                {
                    var x = (ExtendedDrawDataContainer*)&chara->DrawData;

                    x->glassesID = (ushort)Glasses;
                };
            }

            CustomizeStruct custom = new();

            if (IncludeSection(SaveModes.AppearanceHair, mode))
            {
                if (Hair != null)
                    custom.HairStyle = (byte)Hair;

                if (EnableHighlights != null)
                    custom.HasHighlights = (byte)((bool)EnableHighlights ? 0x80 : 0);

                if (HairTone != null)
                    custom.HairColor = (byte)HairTone;

                if (Highlights != null)
                    custom.HairHighlightColor = (byte)Highlights;
            }


            if (IncludeSection(SaveModes.AppearanceFace, mode) || IncludeSection(SaveModes.AppearanceBody, mode))
            {
                if (Race != null)
                    custom.Race = (Races)Race;

                if (Gender != null)
                    custom.Gender = (Genders)Gender;

                if (Tribe != null)
                    custom.Tribe = (Tribes)Tribe;

                if (Age != null)
                    custom.Age = (Age)Age;
            }

            if (IncludeSection(SaveModes.AppearanceFace, mode))
            {
                if (Head != null)
                    custom.FaceType = (byte)Head;

                if (REyeColor != null)
                    custom.REyeColor = (byte)REyeColor;

                if (FacialFeatures != null)
                    custom.FaceFeatures = (FacialFeatures)FacialFeatures;

                if (LimbalEyes != null)
                    custom.FaceFeaturesColor = (byte)LimbalEyes;

                if (Eyebrows != null)
                    custom.Eyebrows = (byte)Eyebrows;

                if (LEyeColor != null)
                    custom.LEyeColor = (byte)LEyeColor;

                if (Eyes != null)
                    custom.EyeShape = (byte)Eyes;

                if (Nose != null)
                    custom.NoseShape = (byte)Nose;

                if (Jaw != null)
                    custom.JawShape = (byte)Jaw;

                if (Mouth != null)
                    custom.LipStyle = (byte)Mouth;

                if (LipsToneFurPattern != null)
                    custom.LipColor = (byte)LipsToneFurPattern;

                if (FacePaint != null)
                    custom.Facepaint = (byte)FacePaint;

                if (FacePaintColor != null)
                    custom.FacePaintColor = (byte)FacePaintColor;
            }

            if (IncludeSection(SaveModes.AppearanceBody, mode))
            {
                if (Height != null)
                    custom.Height = (byte)Height;

                if (Skintone != null)
                    custom.SkinTone = (byte)Skintone;

                if (EarMuscleTailSize != null)
                    custom.RaceFeatureSize = (byte)EarMuscleTailSize;

                if (TailEarsType != null)
                    custom.RaceFeatureType = (byte)TailEarsType;

                if (Bust != null)
                    custom.BustSize = (byte)Bust;

                chara->Alpha = (Transparency ?? 1.0f);
            }

            chara->DrawData.CustomizeData = *(CustomizeData*)&custom;

            chara->CharacterData.ModelCharaId = (int)ModelType;

            ShaderParams tempShader = new();

            if (IncludeSection(SaveModes.AppearanceExtended, mode) && chara->CharacterData.ModelCharaId == 0)
            {
                if (SkinColor != null)
                    tempShader.SkinColor = (Vector3)SkinColor;
                if (SkinGloss != null)
                    tempShader.SkinGloss = (Vector3)SkinGloss;
                if (LeftEyeColor != null)
                    tempShader.LeftEyeColor = (Vector3)LeftEyeColor;
                if (RightEyeColor != null)
                    tempShader.RightEyeColor = (Vector3)RightEyeColor;
                if (LimbalRingColor != null)
                    tempShader.FeatureColor = (Vector3)LimbalRingColor;
                if (HairColor != null)
                    tempShader.HairColor = (Vector3)HairColor;
                if (HairGloss != null)
                    tempShader.HairGloss = (Vector3)HairGloss;
                if (HairHighlight != null)
                    tempShader.HairHighlight = (Vector3)HairHighlight;
                if (MouthColor != null)
                    tempShader.MouthColor = (Vector4)MouthColor;
                if (MuscleTone != null)
                    tempShader.MuscleTone = (float)MuscleTone;
                //if (HeightMultiplier != null)
                //    extendedChara->ScaleFactor2 = (float)HeightMultiplier;
            }

            shader = tempShader;
        }

        private bool IncludeSection(SaveModes section, SaveModes mode)
        {
            return SaveMode.HasFlag(section) && mode.HasFlag(section);
        }

        [Serializable]
        public class WeaponSave
        {
            public WeaponSave()
            {
            }

            public WeaponSave(WeaponModelId from)
            {
                ModelSet = from.Id;
                ModelBase = from.Type;
                ModelVariant = from.Variant;
                DyeId = from.Stain0;
                DyeId2 = from.Stain1;
            }

            public Vector3 Color { get; set; }
            public Vector3 Scale { get; set; }
            public ushort ModelSet { get; set; }
            public ushort ModelBase { get; set; }
            public ushort ModelVariant { get; set; }
            public byte DyeId { get; set; }
            public byte DyeId2 { get; set; }

            public unsafe void Write(XIVActor actor, bool isMainHand)
            {
                var wep = new WeaponModelId()
                {
                    Id = ModelSet
                };

                if (wep.Id != 0)
                {
                    wep.Type = ModelBase;
                    wep.Variant = ModelVariant;
                    wep.Stain0 = DyeId;
                    wep.Stain1 = DyeId2;
                }

                string result = isMainHand ? "Main Hand" : "Offhand";

                if(actor.GetWeaponSlot(isMainHand ? WeaponSlot.MainHand : WeaponSlot.OffHand).Id == 0 && wep.Id == 0)
                {
                    return;
                }

                if ((actor.isCustom || wep.ValidWeapon(isMainHand ? Resources.ActorEquipSlot.MainHand : Resources.ActorEquipSlot.OffHand, actor.GetClass())) || DalamudServices.clientState.IsGPosing)
                    actor.SetWeaponSlot(isMainHand ? WeaponSlot.MainHand : WeaponSlot.OffHand, wep);
                else
                    IllusioDebug.Log($"Invalid {result} Weapon Type for job!", LogType.Warning, false);
            }
        }

        [Serializable]
        public class ItemSave
        {
            public ItemSave()
            {
            }

            public ItemSave(EquipmentModelId from)
            {
                ModelBase = from.Id;
                ModelVariant = from.Variant;
                DyeId = from.Stain0;
                DyeId2 = from.Stain1;
            }

            public ushort ModelBase { get; set; }
            public byte ModelVariant { get; set; }
            public byte DyeId { get; set; }
            public byte DyeId2 { get; set; }

            public unsafe void Write(XIVActor actor, EquipmentSlot index)
            {
                var item = new EquipmentModelId()
                {
                    Id = ModelBase,
                    Variant = ModelVariant,
                    Stain0 = DyeId,
                    Stain1 = DyeId2

                };

                actor.SetEquipmentSlot(index, item);
            }
        }
    }
}
