using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System.Numerics;
using System.Runtime.InteropServices;

namespace IVPlugin.Actors.Structs
{
    [StructLayout(LayoutKind.Explicit, Size = 0x8E8)]
    public struct ExtendedCharStruct
    {
        [FieldOffset(0x0)] public CharacterBase CharacterBase;

        [FieldOffset(0x50)] public Vector3 Position;

        [FieldOffset(0x0D0)] public Attach Attach;

        [FieldOffset(0x290)] public Vector4 Tint;

        [FieldOffset(0x2A0)] public float ScaleFactor1;
        [FieldOffset(0x2A4)] public float ScaleFactor2;

        [FieldOffset(0x2E0)] public float Wetness;


        public readonly float ScaleFactor => ScaleFactor1 * ScaleFactor2;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x78)]
    public unsafe struct Attach
    {
        [FieldOffset(0x0)] public Task Task;

        [FieldOffset(0x50)] public AttachType Type;

        [FieldOffset(0x58)] public unsafe Skeleton* Target;
        [FieldOffset(0x60)] public unsafe void* Parent; // See Type

        [FieldOffset(0x68)] public uint AttachmentCount;
        [FieldOffset(0x70)] public unsafe AttachmentEntry* Attachments;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x68)]
    public struct AttachmentEntry
    {
        [FieldOffset(0x02)] public ushort BoneIdx;
    }

    public enum AttachType : uint
    {
        None = 0,
        Unknown1 = 1,
        Unknown2 = 2,
        CharacterBase = 3, // CharacterBase*
        Skeleton = 4, // Skeleton*
    }
}
