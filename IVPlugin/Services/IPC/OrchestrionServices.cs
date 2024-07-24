using Dalamud.Game.ClientState.JobGauge.Enums;
using Dalamud.Plugin.Ipc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.Services.IPC
{
    public class OrchestrionServices
    {
        // Unused and currently out of scope.

        /*
        private ICallGateSubscriber<int> _currentSongProvider;
        private ICallGateSubscriber<int, bool> _playSongProvider;

        public OrchestrionServices()
        {
            _currentSongProvider = DalamudServices.PluginInterface.GetIpcSubscriber<int>("Orch.CurrentSong");
            _playSongProvider = DalamudServices.PluginInterface.GetIpcSubscriber<int, bool>("Orch.PlaySong");
        }
        public bool CheckAvailablity()
        {
            try
            {
                if (DalamudServices.PluginInterface.InstalledPlugins.Count(x => x.Name == "Orchestrion Plugin") > 0)
                {
                    return DalamudServices.PluginInterface.InstalledPlugins.First(x => x.Name == "Orchestrion Plugin").IsLoaded;
                }

                return false;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public int GetCurrentSongID()
        {
            return _currentSongProvider.InvokeFunc();
        }

        public void PlaySong(int songID)
        {
            _playSongProvider.InvokeFunc(songID);
        }
        */
    }
}
