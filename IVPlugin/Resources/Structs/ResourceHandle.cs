using FFXIVClientStructs.FFXIV.Client.System.Resource;
using Penumbra.String;
using Penumbra.String.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.Resources.Structs
{   
    public enum LoadState : byte
    {
        Success = 0x07,
        Async = 0x03,
        Failure = 0x09,
        FailedSubResource = 0x0A,
        None = 0xFF,
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct ResourceHandle
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct DataIndirection
        {
            [FieldOffset(0x00)]
            public void** VTable;

            [FieldOffset(0x10)]
            public byte* DataPtr;

            [FieldOffset(0x28)]
            public ulong DataLength;
        }

        public const int SsoSize = 15;

        public byte* FileNamePtr()
        {
            if (FileNameLength > SsoSize)
                return FileNameData;

            fixed (byte** name = &FileNameData)
            {
                return (byte*)name;
            }
        }

        public ByteString FileName()
            => ByteString.FromByteStringUnsafe(FileNamePtr(), FileNameLength, true);

        public ReadOnlySpan<byte> FileNameAsSpan()
            => new(FileNamePtr(), FileNameLength);

        public bool GamePath(out Utf8GamePath path)
            => Utf8GamePath.FromSpan(FileNameAsSpan(), out path);

        [FieldOffset(0x00)]
        public void** VTable;

        [FieldOffset(0x08)]
        public ResourceCategory Category;

        [FieldOffset(0x0C)]
        public ResourceType FileType;

        [FieldOffset(0x10)]
        public uint Id;

        [FieldOffset(0x28)]
        public uint FileSize;

        [FieldOffset(0x2C)]
        public uint FileSize2;

        [FieldOffset(0x34)]
        public uint FileSize3;

        [FieldOffset(0x48)]
        public byte* FileNameData;

        [FieldOffset(0x58)]
        public int FileNameLength;

        [FieldOffset(0xA9)]
        public LoadState LoadState;

        [FieldOffset(0xAC)]
        public uint RefCount;
    }
}
