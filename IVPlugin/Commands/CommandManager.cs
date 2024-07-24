using Dalamud.Game.Command;
using IVPlugin.UI;
using IVPlugin.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Penumbra.Api;
using FFXIVClientStructs.FFXIV.Client.Game;
using IVPlugin.ActorData;
using IVPlugin.Services;
using System.IO;
using IVPlugin.UI.Windows;
using IVPlugin.Core;
using System.Diagnostics;
using IVPlugin.Json;
using IVPlugin.Mods.Structs;
using IVPlugin.Mods;
using IVPlugin.Core.Files;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using IVPlugin.Camera;
using Dalamud.Game.Text;
using IVPlugin.Log;

namespace IVPlugin.Commands
{
    public class CommandManager : IDisposable
    {
        public const string MainCommand = "/illusio";

        public List<string> CommandList = new List<string>();
        public static CommandManager Instance { get; private set; } = null!;

        public Configuration configuration { get; private set; }

        public CommandManager()
        {
            configuration = IllusioVitae.configuration;

            DalamudServices.CommandManager.AddHandler(MainCommand, new CommandInfo(OnMainCommand)
            {
                HelpMessage = "Opens Illusio Vitae's GUI. Use help to get more information"
            });

            Instance = this;
        }

        private void OnMainCommand(string command, string args)
        {
            switch (args)
            {
                case string a when a.Contains("help", StringComparison.OrdinalIgnoreCase):
                    DisplayHelp();
                    break;
                case string b when b.Contains("overlay", StringComparison.OrdinalIgnoreCase):
                    ToggleOverlay();
                    break;
                default:
                    MainWindow.Show();
                    break;
            }
        }

        private void DisplayHelp()
        {
            IllusioDebug.ChatLog("Illusio Vitae Command Help:", XivChatType.Notice, false);
            IllusioDebug.ChatLog("[/command] -npc: Spawns Actors for multiperson dances", XivChatType.Notice, false);
            //IllusioDebug.ChatLog("[/command] -target: Forces target to do custom animation", XivChatType.Notice, false);
        }

        private void ToggleOverlay()
        {
            OverlayHUD.IsOpen = !OverlayHUD.IsOpen;
        }

        public void RegisterNewCommand(IVMod mod)
        {
            IllusioDebug.Log($"Importing {mod.emote.emoteData.Count} emotes", LogType.Debug);

            for (int i = 0; i < mod.emote.emoteData.Count; i++)
            {
                var currentEmote = mod.emote.emoteData[i];

                if (CommandList.Contains(currentEmote.GetCommand()))
                {
                    IllusioDebug.Log($"Multiple commands with same name detected! Disabling {mod.emote.Name}.", LogType.Warning);
                    return;
                }

                addCommand(mod, currentEmote.GetCommand(), i);

                CommandList.Add(currentEmote.GetCommand());
            } 
        }

        private void addCommand(IVMod mod, string command, int modID)
        {
            DalamudServices.CommandManager.AddHandler(command, new CommandInfo((command, args) =>
            {
                ModManager.Instance.PlayMod(mod, modID, args.Contains("-npc", StringComparison.OrdinalIgnoreCase), false);
            })
            {
                ShowInHelp = false
            });
        }

        public void RemoveCommands()
        {
            foreach (var command in CommandList)
            {
                DalamudServices.CommandManager.RemoveHandler(command);

                IllusioDebug.Log($"Remove Command {command}", LogType.Debug);
            }

            CommandList.Clear();
        }

        public void Dispose()
        {
            DalamudServices.CommandManager.RemoveHandler(MainCommand);

            RemoveCommands();

            Instance = null!;
        }
    }
}
