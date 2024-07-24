using IVPlugin.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.ActorData.Structs
{
    [StructLayout(LayoutKind.Explicit, Size = Count)]
    public struct CustomizeStruct
    {
        public const int Count = 0x1A;

        [FieldOffset(0x00)] public unsafe fixed byte Data[Count];
        [FieldOffset(0x00)] public Races Race;
        [FieldOffset(0x01)] public Genders Gender;
        [FieldOffset(0x02)] public Age Age;
        [FieldOffset(0x03)] public byte Height;
        [FieldOffset(0x04)] public Tribes Tribe;
        [FieldOffset(0x05)] public byte FaceType;
        [FieldOffset(0x06)] public byte HairStyle;
        [FieldOffset(0x07)] public byte HasHighlights;
        [FieldOffset(0x08)] public byte SkinTone;
        [FieldOffset(0x09)] public byte REyeColor;
        [FieldOffset(0x0A)] public byte HairColor;
        [FieldOffset(0x0B)] public byte HairHighlightColor;
        [FieldOffset(0x0C)] public FacialFeatures FaceFeatures;
        [FieldOffset(0x0D)] public byte FaceFeaturesColor;
        [FieldOffset(0x0E)] public byte Eyebrows;
        [FieldOffset(0x0F)] public byte LEyeColor;
        [FieldOffset(0x10)] public byte EyeShape;
        [FieldOffset(0x11)] public byte NoseShape;
        [FieldOffset(0x12)] public byte JawShape;
        [FieldOffset(0x13)] public byte LipStyle;
        [FieldOffset(0x14)] public byte LipColor;
        [FieldOffset(0x15)] public byte RaceFeatureSize;
        [FieldOffset(0x16)] public byte RaceFeatureType;
        [FieldOffset(0x17)] public byte BustSize;
        [FieldOffset(0x18)] public byte Facepaint;
        [FieldOffset(0x19)] public byte FacePaintColor;


        private const byte ToggleMask = 128;

        public bool HighlightsEnabled
        {
            readonly get => (HasHighlights & ToggleMask) != 0;
            set => HasHighlights = (byte)(value ? ToggleMask : 0);
        }

        public byte RealFacepaint
        {
            readonly get => (byte)(Facepaint >= ToggleMask ? Facepaint ^ ToggleMask : Facepaint);
            set => Facepaint = (byte)(FacepaintFlipped ? value | ToggleMask : value);
        }

        public bool FacepaintFlipped
        {
            readonly get => (Facepaint & ToggleMask) != 0;
            set => Facepaint = (byte)(value ? Facepaint | ToggleMask : Facepaint ^ ToggleMask);
        }

        public byte RealEyeShape
        {
            readonly get => (byte)(EyeShape >= ToggleMask ? EyeShape ^ ToggleMask : EyeShape);
            set => EyeShape = (byte)(HasSmallIris ? value | ToggleMask : value);
        }

        public bool HasSmallIris
        {
            readonly get => (EyeShape & ToggleMask) != 0;
            set => EyeShape = (byte)(value ? EyeShape | ToggleMask : EyeShape ^ ToggleMask);
        }

        public bool LipColorEnabled
        {
            readonly get => (LipStyle & ToggleMask) != 0;
            set => LipStyle = (byte)(value ? LipStyle | ToggleMask : LipStyle ^ ToggleMask);
        }

        public byte RealLipStyle
        {
            readonly get => (byte)(LipStyle >= ToggleMask ? LipStyle ^ ToggleMask : LipStyle);
            set => LipStyle = (byte)(LipColorEnabled ? value | ToggleMask : value);
        }

    }
}
