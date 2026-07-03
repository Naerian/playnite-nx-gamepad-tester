using GamepadTester.Services;
using GamepadTester.ViewModels;
using GamepadTester.Views;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GamepadTester
{
    public class GamepadTester : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static readonly Guid pluginId = Guid.Parse("518dc982-32b5-4493-b32d-1f71de2fe4ad");

        private GamepadTesterSettingsViewModel settings { get; set; }

        public override Guid Id
        {
            get { return pluginId; }
        }

        public GamepadTester(IPlayniteAPI api) : base(api)
        {
            settings = new GamepadTesterSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GamepadTesterSettingsView();
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem
            {
                MenuSection = "@Gamepad Tester",
                Description = "Open Gamepad Tester",
                Action = args2 => OpenTesterWindow()
            };
        }

        private void OpenTesterWindow()
        {
            try
            {
                var pollingService = new GamepadPollingService(new SdlGamepadProvider());
                var viewModel = new GamepadTesterViewModel(pollingService);
                var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
                {
                    ShowMinimizeButton = true,
                    ShowMaximizeButton = true
                });

                window.Title = "Gamepad Tester";
                window.Width = 1280;
                window.Height = 820;
                window.MinWidth = 1180;
                window.MinHeight = 760;
                window.Content = new GamepadTesterView();
                window.DataContext = viewModel;
                window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Closed += (sender, eventArgs) => viewModel.Dispose();

                viewModel.Start();
                window.Show();
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Failed to open Gamepad Tester.");
                PlayniteApi.Dialogs.ShowErrorMessage(exception.Message, "Gamepad Tester");
            }
        }
    }
}
