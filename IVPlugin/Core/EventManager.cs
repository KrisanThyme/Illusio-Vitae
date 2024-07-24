using Dalamud.Plugin.Services;
using IVPlugin.ActorData;
using IVPlugin.Services;
using IVPlugin.UI;
using System;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using IVPlugin.Core.Extentions;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using IVPlugin.Windows;
using Dalamud.Game.ClientState.Objects.Types;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Dalamud.Game.ClientState.Conditions;

namespace IVPlugin.Core
{
    public unsafe class EventManager: IDisposable
    {
        public static EventManager instance {  get; private set; }

        public delegate void onGPoseChange(bool GposeEnabled);
        public static onGPoseChange? GPoseChange = null;
        public static bool validCheck { get; private set; }
        public static bool ValidAudioCheck { get; private set; }

        public static bool InCutscene { get; private set; }

        TerritoryInfo* territoryInfo;

        private bool gposeLock = false;

        int[] housingIDs = {282, 283, 284, 384, 608, 342, 343, 344, 385, 609, 345, 
                            346, 347, 386, 610, 980, 981, 982, 983, 999, 649, 650, 
                            651, 652, 655, 177, 179, 178, 429, 629, 843, 990 };

        public EventManager()
        {
            instance = this;

            DalamudServices.framework.Update += Update;

            territoryInfo = TerritoryInfo.Instance();
            
        }

        public void Update(IFramework framework)
        {
            if (!DalamudServices.clientState.IsLoggedIn) return; 

            if(DalamudServices.clientState.IsGPosing)
            {
                if (!gposeLock)
                {
                    gposeLock = true;
                    GPoseChange?.Invoke(true);
                    init();

                    validCheck = true;
                }
            }
            else
            {
                if (gposeLock)
                {
                    gposeLock = false;
                    GPoseChange?.Invoke(false);
                }

                if (IllusioVitae.InDebug())
                {
                    validCheck = true;
                    ValidAudioCheck = true;
                    return;
                }
                else
                {
                    if (ActorManager.Instance.playerActor != null && ActorManager.Instance.playerActor.IsLoaded())
                    {
                        ValidityCheck();
                    }
                }
            }

            if (DalamudServices.condition[ConditionFlag.OccupiedInCutSceneEvent] || DalamudServices.condition[ConditionFlag.OccupiedInCutSceneEvent] || DalamudServices.condition[ConditionFlag.OccupiedInCutSceneEvent]){
                InCutscene = true;
            }else
            {
                InCutscene = false;
            }
        }

        public void init()
        {
            MainWindow.Show();
        }

        public void ValidityCheck()
        {
            var player = (ICharacter)ActorManager.Instance.playerActor.actorObject;

            var target = DalamudServices.TargetManager.Target;

            if (player.StatusFlags.HasFlag(StatusFlags.InCombat) || DalamudServices.clientState.IsPvP || !territoryInfo->InSanctuary ||
                    player.Base()->Mode.HasFlag(CharacterModes.Crafting) || player.Base()->Mode.HasFlag(CharacterModes.AnimLock) ||
                    player.Base()->Mode.HasFlag(CharacterModes.RidingPillion) || player.Base()->Mode.HasFlag(CharacterModes.Performance) ||
                    player.Base()->Mode.HasFlag(CharacterModes.Carrying) || player.Base()->Mode.HasFlag(CharacterModes.Mounted))
            {
                validCheck = false;
            }
            else
            {
                validCheck = true;
            }
        }

        public void Dispose()
        {
            DalamudServices.framework.Update -= Update;
        }
    }
}
