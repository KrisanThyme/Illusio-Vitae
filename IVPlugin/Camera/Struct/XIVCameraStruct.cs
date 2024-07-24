using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using GameCam = FFXIVClientStructs.FFXIV.Client.Game.Camera;

namespace IVPlugin.Camera.Struct
{
    [StructLayout(LayoutKind.Explicit, Size = 0x2B0)]
    public struct XIVCameraStruct
    {
        [FieldOffset(0x0)]
        public GameCam Camera;

        [FieldOffset(0x60)] public Vector3 Unk1;

        [FieldOffset(0x12C)] public float FoV;

        [FieldOffset(0x130)] public Vector2 Angle;

        [FieldOffset(0x150)] public Vector2 Pan;

        [FieldOffset(0x160)] public float Rotation;

        [FieldOffset(0x208)] public Vector2 Collide;

        [FieldOffset(0x178)] public CameraType type;

        [FieldOffset(0x1C0)] public Vector3 Unk;
    }

    public enum CameraType : byte { Legacy = 1, Modern = 2}
}
