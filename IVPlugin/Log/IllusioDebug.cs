using Dalamud.Game.Text;
using IVPlugin.Services;

namespace IVPlugin.Log
{
    public static class IllusioDebug
    {
        public static void Log(string message, LogType type, bool debugOnly = true)
        {
            if (debugOnly && !IllusioVitae.InDebug()) return;

            switch (type)
            {
                case LogType.Verbose:
                    DalamudServices.log.Verbose(message); break;
                case LogType.Debug:
                    DalamudServices.log.Debug(message); break;
                case LogType.Error:
                    DalamudServices.log.Error(message); break;
                case LogType.Info:
                    DalamudServices.log.Info(message); break;
                case LogType.Warning:
                    DalamudServices.log.Warning(message); break;
            }
        }

        public static void ChatLog(string message, XivChatType type, bool debugOnly = false) 
        {
            if (debugOnly && !IllusioVitae.InDebug()) return;

            XivChatEntry chat = new()
            {
                Type = type,
                Message = message
            };

            DalamudServices.chatGui.Print(chat);
        }
    }

    public enum LogType{ Debug, Verbose, Info, Warning, Error}
}
