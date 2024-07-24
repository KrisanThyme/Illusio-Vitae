using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Penumbra.Api;
using IVPlugin.Services.IPC;
using Penumbra.Api.Api;
using Dalamud.Configuration;

namespace IVPlugin.Services
{
    public class DalamudServices
    {
        [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static IDataManager DataManager { get; private set; } = null!;
        [PluginService] public static ITargetManager TargetManager { get; private set; } = null!;
        [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
        [PluginService] public static IFramework framework { get; private set; } = null!;
        [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
        [PluginService] public static IClientState clientState { get; private set; } = null!;
        [PluginService] public static ICondition condition { get; private set; } = null!;
        [PluginService] public static IPluginLog log { get; private set; } = null!;
        [PluginService] public static IChatGui chatGui { get; private set; } = null!;
        [PluginService] public static IObjectTable objectTables { get; private set; } = null!;
        [PluginService] public static ITextureProvider textureProvider { get; private set; } = null!;


        public static PenumbraServices penumbraServices { get; private set; } = null!;

        public static void Initialize(IDalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<DalamudServices>();
            penumbraServices = new();
        }
    }
}
