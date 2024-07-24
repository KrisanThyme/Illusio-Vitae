using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.Audio.Structs
{
    /*
    [StructLayout(LayoutKind.Explicit)]
    public struct BGMPlayer
    {
        [FieldOffset(0x00)] public float MaxStandbyTime;
        [FieldOffset(0x04)] public uint State;
        [FieldOffset(0x08)] public ushort BgmId;
        [FieldOffset(0x10)] public uint BgmScene;
        [FieldOffset(0x20)] public uint SpecialMode;
        [FieldOffset(0x25)] public bool IsStandby;
        [FieldOffset(0x28)] public uint FadeOutTime;
        [FieldOffset(0x2C)] public uint ResumeFadeInTime;
        [FieldOffset(0x30)] public uint FadeInStartTime;
        [FieldOffset(0x34)] public uint FadeInTime;
        [FieldOffset(0x38)] public uint ElapsedTime;
        [FieldOffset(0x40)] public float StandbyTime;
        [FieldOffset(0x4D)] public byte SpecialModeType;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BGMScene
    {
        public int SceneIndex;
        public SceneFlags Flags;
        private int Padding1;
        // often writing songId will cause songId2 and 3 to be written automatically
        // songId3 is sometimes not updated at all, and I'm unsure of its use
        // zeroing out songId2 seems to be necessary to actually cancel playback without using
        // an invalid id (which is the only way to do it with just songId1)
        public ushort BgmReference;       // Reference to sheet; BGM, BGMSwitch, BGMSituation
        public ushort BgmId;              // Actual BGM that's playing. Game will manage this if it's a switch or situation
        public ushort PreviousBgmId;      // BGM that was playing before this one; I think it only changed if the previous BGM 
        public byte TimerEnable;            // whether the timer automatically counts up
        private byte Padding2;
        public float Timer;                 // if enabled, seems to always count from 0 to 6
                                            // if 0x30 is 0, up through 0x4F are 0
                                            // in theory function params can be written here if 0x30 is non-zero but I've never seen it
        private fixed byte DisableRestartList[24]; // 'vector' of bgm ids that will be restarted - managed by game. it is 3 pointers
        private byte Unknown1;
        private uint Unknown2;
        private uint Unknown3;
        private uint Unknown4;
        private uint Unknown5;
        private uint Unknown6;
        private ulong Unknown7;
        private uint Unknown8;
        private byte Unknown9;
        private byte Unknown10;
        private byte Unknown11;
        private byte Unknown12;
    }

    public enum SceneFlags : byte
    {
        None = 0,
        Unknown = 1,
        Resume = 2,
        EnablePassEnd = 4,
        ForceAutoReset = 8,
        EnableDisableRestart = 16,
        IgnoreBattle = 32,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisableRestart
    {
        public ushort DisableRestartId;
        public bool IsTimedOut; // ?
        public byte Padding1;
        public float ResetWaitTime;
        public float ElapsedTime;
        public bool TimerEnabled;
        // 3 byte padding
    }
    */
}
