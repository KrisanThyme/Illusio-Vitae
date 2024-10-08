using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.Actors.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ExtendedHumanStruct
    {
        [FieldOffset(0x0)]
        public Human Human;

        [FieldOffset(0xA50)]
        public short AnimPath;

        [FieldOffset(0xBA0)]
        public unsafe ShaderManager* Shaders;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ShaderManager
    {
        [FieldOffset(0x28)]
        public unsafe ShaderParams* Params;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ShaderParams
    {
        [FieldOffset(0x00)]
        public Vector3 SkinColor;

        [FieldOffset(0x0C)]
        public float MuscleTone;

        [FieldOffset(0x10)]
        public Vector3 SkinGloss;

        [FieldOffset(0x20)]
        public Vector4 MouthColor;

        [FieldOffset(0x30)]
        public Vector3 HairColor;

        [FieldOffset(0x40)]
        public Vector3 HairGloss;

        [FieldOffset(0x50)]
        public Vector3 HairHighlight;

        [FieldOffset(0x60)]
        public Vector3 LeftEyeColor;

        [FieldOffset(0x70)]
        public Vector3 RightEyeColor;

        [FieldOffset(0x80)]
        public Vector3 FeatureColor;
    }
}
