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
    public struct ExtendedDrawDataContainer
    {
        [FieldOffset(0)] public DrawDataContainer drawDataContainer;

        [FieldOffset(16)] public DrawObjectData weapon1;

        [FieldOffset(464)] public ushort glassesID;
        [FieldOffset(466)] public ushort unk;
    }
}
