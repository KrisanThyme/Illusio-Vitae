using Dalamud.Game.ClientState.JobGauge.Enums;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using IVPlugin.Audio.Hooks;
using IVPlugin.Audio.Structs;
using IVPlugin.Services;
using Lumina.Data.Parsing.Scene;
using System;
using System.Data;
using System.Runtime.InteropServices;
using System.Threading.Tasks;


namespace IVPlugin.Audio
{
    /*
    public class BGMManager : IDisposable
    {
        public static BGMManager Instance { get; private set; }

        private unsafe delegate DisableRestart* AddDisableRestartIdPrototype(BGMScene* scene, ushort songId);
        private readonly AddDisableRestartIdPrototype _addDisableRestartId;

        private unsafe delegate int GetSpecialModeByScenePrototype(BGMPlayer* bgmPlayer, byte specialModeType);
        private readonly Hook<GetSpecialModeByScenePrototype> _getSpecialModeForSceneHook;

        public int playingSceneID, playingSongID;

        public int currentSongID, currentSceneID, secondarySongID, secondarySceneID;

        public int previousSongID, previousSceneID, previousSecondarySongID, previousSecondarySceneID;

        public unsafe BGMManager()
        {
            Instance = this;
            _addDisableRestartId = Marshal.GetDelegateForFunctionPointer<AddDisableRestartIdPrototype>(BGmAddress.AddRestartId);
            _getSpecialModeForSceneHook = DalamudServices.GameInteropProvider.HookFromAddress<GetSpecialModeByScenePrototype>(BGmAddress.GetSpecialMode, GetSpecialModeBySceneDetour);
            _getSpecialModeForSceneHook.Enable();

            DalamudServices.framework.Update += update;
        }

        private void update(IFramework framework)
        {
            if (BGmAddress.BGMSceneList != nint.Zero)
            {
                unsafe
                {
                    var bgms = (BGMScene*)BGmAddress.BGMSceneList.ToPointer();

                    for (int sceneIdx = 0; sceneIdx < 12; sceneIdx++)
                    {

                        if (bgms[sceneIdx].BgmId != 0 && bgms[sceneIdx].BgmId != 9999)
                        {
                            // Ignore the PlayingScene scene
                            if (playingSongID != 0 && sceneIdx == playingSceneID)
                            {
                                // If the game overwrote our song, play it again
                                if (bgms[playingSceneID].BgmId != playingSongID)
                                    playsong((ushort)playingSongID, playingSceneID);
                                continue;
                            }

                            if (bgms[sceneIdx].BgmReference == 0) continue;

                            if (currentSongID == 0)
                            {
                                currentSongID = bgms[sceneIdx].BgmId;
                                currentSceneID = sceneIdx;
                            }
                            else
                            {
                                secondarySongID = bgms[sceneIdx].BgmId;
                                secondarySceneID = sceneIdx;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private unsafe int GetSpecialModeBySceneDetour(BGMPlayer* player, byte specialModeType)
        {
            // Let the game do what it needs to do
            if (player->BgmScene != playingSceneID
                || player->BgmId != currentSongID
                || specialModeType == 0)
                return _getSpecialModeForSceneHook.Original(player, specialModeType);

            // Default to scene 10 behavior, but if the mode is mount mode, use the mount scene
            uint newScene = 10;

            // Trick the game into giving us the result we want for the scene our song should actually be playing on
            var tempScene = player->BgmScene;
            player->BgmScene = newScene;
            var result = _getSpecialModeForSceneHook.Original(player, specialModeType);
            player->BgmScene = tempScene;
            return result;
        }
        public void playsong(ushort songID, int priority = 0)
        {
            if (BGmAddress.BGMSceneList != nint.Zero)
            {
                unsafe
                {
                    var bgms = (BGMScene*)BGmAddress.BGMSceneList.ToPointer();

                    bgms[priority].BgmReference = songID;
                    bgms[priority].BgmId = songID;
                    bgms[priority].PreviousBgmId = songID;

                    if (songID == 0 && priority == 0)
                        bgms[priority].Flags = SceneFlags.Resume;

                    // these are probably not necessary, but clear them to be safe
                    bgms[priority].Timer = 0;
                    bgms[priority].TimerEnable = 0;

                    playingSongID = songID;
                    playingSceneID = priority;

                    bgms[priority].Flags = SceneFlags.EnableDisableRestart;
                    _addDisableRestartId(&bgms[priority], songID);
                    bgms[priority].Flags = SceneFlags.ForceAutoReset;

                    Task.Delay(500).ContinueWith(_ =>
                    {
                        bgms[priority].Flags = SceneFlags.EnableDisableRestart | SceneFlags.ForceAutoReset;
                    });

                   
                }
            }
        }

        public void Dispose()
        {
            _getSpecialModeForSceneHook.Dispose();
            DalamudServices.framework.Update -= update;
        }
    }
    */
}
