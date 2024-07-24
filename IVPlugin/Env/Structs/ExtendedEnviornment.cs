using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.Env.Structs
{
    [StructLayout(LayoutKind.Explicit, Size = 0x900)]
    public struct ExtendedEnviornmentManager
    {
        [FieldOffset(0x10)] public float Time;

        [FieldOffset(0x27)] public byte ActiveWeather;

        [FieldOffset(0x60)] public uint SkyId;
    }
}
