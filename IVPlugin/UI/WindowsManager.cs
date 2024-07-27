using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.System.File;
using IVPlugin.Core;
using IVPlugin.Services;
using IVPlugin.UI.Windows;
using IVPlugin.Windows;
using System;

namespace IVPlugin.UI
{
    public class WindowsManager : IDisposable
    {
        public static WindowsManager Instance { get; private set; } = null!;

        private readonly IDalamudPluginInterface pluginInterface;

        public FileDialogManager fileDialogManager;

        public WindowsManager(IDalamudPluginInterface _pluginInterface)
        {
            pluginInterface = _pluginInterface;

            fileDialogManager = new FileDialogManager();

            Instance = this;

            pluginInterface.UiBuilder.Draw += DrawUI;
            pluginInterface.UiBuilder.OpenMainUi += MainWindow.Show;
            pluginInterface.UiBuilder.OpenConfigUi += ConfigWindow.Show;
            pluginInterface.UiBuilder.DisableGposeUiHide = true;

            DalamudServices.clientState.TerritoryChanged += (_) => { ApperanceWindow.Hide(); SkeletonOverlay.Hide(); };
            EventManager.GPoseChange += (_) => { ApperanceWindow.Hide(); SkeletonOverlay.Hide(); };
        }

        private void DrawUI()
        {
            if(!DalamudServices.clientState.IsLoggedIn) return;

            fileDialogManager.Draw();
            MainWindow.Draw();
            ConfigWindow.Draw();
            ApperanceWindow.Draw();
            SkeletonOverlay.Draw();
            ChangeLogWindow.Draw();
            ASceneWarningWindow.Draw();
            WrongLocationWindow.Draw();
            OverlayHUD.Draw();
            FirstTimeWindow.Draw();
            ModCreationWindow.Draw();
        }

        public void Dispose()
        {
            pluginInterface.UiBuilder.Draw -= DrawUI;
            pluginInterface.UiBuilder.OpenMainUi -= MainWindow.Show;
            pluginInterface.UiBuilder.OpenConfigUi -= ConfigWindow.Show;
            DalamudServices.clientState.TerritoryChanged -= (_) => { ApperanceWindow.Hide(); SkeletonOverlay.Hide(); };
            EventManager.GPoseChange -= (_) => { ApperanceWindow.Hide(); SkeletonOverlay.Hide(); };
            Instance = null!;
        }
    }
}
