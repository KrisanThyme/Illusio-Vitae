using Dalamud.Configuration;
using Dalamud.Game.Network.Structures.InfoProxy;
using Dalamud.Plugin;
using IVPlugin.ActorData;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;

namespace IVPlugin.Core
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public bool IsWorking { get; set; } = false;

        public string ModLocation { get; set; } = string.Empty;
        public bool installIVCS { get; set; } = true;

        public bool UseNPCHack { get; set; } = true;
        public bool UseSkeletonColors { get; set; } = true;
        public bool ShowDebugData { get; set; } = false;
        public bool ActorSceneLocalSpace { get; set; } = true;
        public bool ActorSceneWarningShow {  get; set; } = true;
        public bool FadeInOnAnimation { get; set; } = true;


        public CustomActorInfo[] PresetActors { get; set; } = new CustomActorInfo[0];

        public int LastSeenVersion = -1;

        public bool firstTimeCheck { get; set; } = true;

        public Guid SelectedCollection = Guid.Empty;
        public int defaultPriority = 1;
    }
}
