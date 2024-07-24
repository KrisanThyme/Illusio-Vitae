using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using IVPlugin.Log;
using Penumbra.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.Services.IPC
{
    public class PenumbraServices
    {
        private const int PenumbraApiMajor = 5;
        private const int PenumbraApiMinor = 0;
        public PenumbraServices()
        {
        }

        public bool CheckAvailablity() 
        {
            try
            {
                if (DalamudServices.PluginInterface.InstalledPlugins.Count(x => x.Name == "Penumbra") > 0)
                {
                    if (!DalamudServices.PluginInterface.InstalledPlugins.First(x => x.Name == "Penumbra").IsLoaded)
                        return false;

                    var result = new Penumbra.Api.IpcSubscribers.ApiVersion(DalamudServices.PluginInterface).Invoke();

                    //IllusioDebug.Log($"Penumbra Version {result.Breaking} {result.Features}", LogType.Debug, false);

                    if (result.Breaking != PenumbraApiMajor || result.Features < PenumbraApiMinor)
                    {
                        if (!IllusioVitae.IsDebug)
                            return false;
                    }

                    return true;
                }

                return false;
                
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void Dispose()
        {

        }
    }
}
