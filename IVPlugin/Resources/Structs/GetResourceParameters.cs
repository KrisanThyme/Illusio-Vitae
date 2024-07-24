using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.Resources.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct GetResourceParameters
    {
        [FieldOffset(16)]
        public uint SegmentOffset;

        [FieldOffset(20)]
        public uint SegmentLength;

        public readonly bool IsPartialRead
            => SegmentLength != 0;
    }
}
