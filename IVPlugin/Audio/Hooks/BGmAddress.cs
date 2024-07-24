using FFXIVClientStructs.FFXIV.Client.System.Framework;
using IVPlugin.Core;
using IVPlugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.Audio.Hooks
{
    /*
    public class BGmAddress
    {
        private static nint _baseAddress;
        private static nint _musicManager;

        public static nint AddRestartId { get; private set; }
        public static nint GetSpecialMode { get; private set; }

        public static unsafe void Init()
        {
            _baseAddress = DalamudServices.SigScanner.GetStaticAddressFromSig(XIVSigs.baseAudioAddress);
            AddRestartId = DalamudServices.SigScanner.ScanText(XIVSigs.AddRestartIdAddress);
            GetSpecialMode = DalamudServices.SigScanner.ScanText(XIVSigs.specialModeAddress);

            var musicLoc = DalamudServices.SigScanner.ScanText(XIVSigs.musicLocationAddress);
            var musicOffset = Marshal.ReadInt32(musicLoc + 3);
            _musicManager = Marshal.ReadIntPtr(new nint(Framework.Instance()) + musicOffset);
        }

        public static nint BGMSceneManager
        {
            get
            {
                var baseObject = Marshal.ReadIntPtr(_baseAddress);

                return baseObject;
            }
        }

        public static nint BGMSceneList
        {
            get
            {
                var baseObject = Marshal.ReadIntPtr(_baseAddress);

                // I've never seen this happen, but the game checks for it in a number of places
                return baseObject == nint.Zero ? nint.Zero : Marshal.ReadIntPtr(baseObject + 0xC0);
            }
        }

        public static bool StreamingEnabled
        {
            get
            {
                var ret = Marshal.ReadByte(_musicManager + 50);
                return ret == 1;
            }
        }
    }
    */
}
