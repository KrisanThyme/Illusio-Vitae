using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.Actors.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct ExtendedWeaponData
    {
        [FieldOffset(0x0)]
        public DrawObjectData WeaponData;

        [FieldOffset(0x18)]
        public ExtendedCharStruct* drawData;

        [FieldOffset(0x40)]
        public unsafe uint WeaponAttachment;

        [FieldOffset(0x4C)]
        public unsafe float WeaponScale;

        [FieldOffset(0x66)]
        public unsafe ushort WeaponAnimationID;

        [FieldOffset(0x6A)]
        public unsafe ushort WeaponState;
    }
}
