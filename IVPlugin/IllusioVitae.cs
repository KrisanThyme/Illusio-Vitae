using Dalamud.Game.Command;
using Dalamud.Plugin;
using IVPlugin.Core;
using IVPlugin.UI;
using IVPlugin.Resources;
using IVPlugin.ActorData;
using IVPlugin.Services;
using IVPlugin.Commands;
using IVPlugin.Mods;
using IVPlugin.VFX;
using System.Diagnostics;
using IVPlugin.Posing;
using IVPlugin.Log;
using IVPlugin.Gpose;
using IVPlugin.Cutscene;
using IVPlugin.Cutscene.Hooks;
using IVPlugin.Camera;
using System;

namespace IVPlugin
{
    public sealed class IllusioVitae : IDalamudPlugin
    {
        public const string Name = "Illusio Vitae";

        public static string Version = $"0.1.0 Beta";

#if DEBUG
        public const bool IsDebug = true;
#else
        public const bool IsDebug = false;
#endif

        public static Configuration configuration { get; private set; } = null!;
        public EventManager eventManager { get; private set; }
        public ResourceProcessor resourceProcessor { get; private set; }
        public GameResourceManager resourceHandler { get; private set; }
        public WindowsManager windowsManager { get; private set; }
        public CommandManager commandManager { get; private set; }
        public ActorManager actorManager { get; private set; }
        public ModManager modManager { get; private set; }
        public IllusioCutsceneManager cameraManager { get; private set; }
        public VirtualCamera virtualCamera { get; }
        public XIVCamera xivCamera { get; private set; }
        public CutsceneCamera cutsceneCamera { get; private set; }
        public VFXManager vfxManager { get; private set; }
        public WorldManager gposeManager { get; private set; }
        public PosingManager posingManager { get; private set; }

        public IllusioVitae(IDalamudPluginInterface pluginInterface)
        {
            DalamudServices.Initialize(pluginInterface);
            configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            resourceProcessor = new ResourceProcessor();
            resourceHandler = new(pluginInterface);
            commandManager = new();
            modManager = new();
            actorManager = new();
            eventManager = new();
            virtualCamera = new();
            xivCamera = new();
            cutsceneCamera = new(this);
            cameraManager = new(this);
            vfxManager = new();
            posingManager = new();
            gposeManager = new();
            windowsManager = new(pluginInterface);
        }

        public static bool InDebug()
        {
            if (IsDebug && configuration.ShowDebugData) return true;
            
            return false;
        }

        public void Dispose()
        {
            DalamudServices.PluginInterface.SavePluginConfig(configuration);
            
            commandManager.Dispose();
            actorManager.Dispose();
            modManager.Dispose();
            eventManager.Dispose();
            resourceProcessor.Dispose();
            resourceHandler.Dispose();
            xivCamera.Dispose();
            cutsceneCamera.Dispose();
            cameraManager.Dispose();
            windowsManager.Dispose();
            vfxManager.Dispose();
            posingManager.Dispose();
            gposeManager.Dispose();
        }
    }
}
