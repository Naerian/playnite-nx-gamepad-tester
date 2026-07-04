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
        private GamepadTesterViewModel sidebarViewModel;

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
            DisposeSidebarView();
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

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            if (!settings.Settings.ShowSidebarItem)
            {
                yield break;
            }

            yield return new SidebarItem
            {
                Type = SiderbarItemType.View,
                Title = "Gamepad Tester",
                Visible = true,
                Icon = new TextBlock
                {
                    Text = "GT",
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                Opened = () =>
                {
                    DisposeSidebarView();
                    GamepadTesterViewModel viewModel;
                    var view = CreateTesterView(out viewModel);
                    sidebarViewModel = viewModel;
                    return view;
                },
                Closed = DisposeSidebarView
            };
        }

        private void OpenTesterWindow()
        {
            try
            {
                GamepadTesterViewModel viewModel;
                var view = CreateTesterView(out viewModel);
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
                window.Content = view;
                window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Closed += (sender, eventArgs) => viewModel.Dispose();

                window.Show();
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Failed to open Gamepad Tester.");
                PlayniteApi.Dialogs.ShowErrorMessage(exception.Message, "Gamepad Tester");
            }
        }

        private GamepadTesterView CreateTesterView(out GamepadTesterViewModel viewModel)
        {
            var pollingService = new GamepadPollingService(new SdlGamepadProvider());
            viewModel = new GamepadTesterViewModel(pollingService, settings.Settings);
            var view = new GamepadTesterView
            {
                DataContext = viewModel
            };

            viewModel.Start();
            return view;
        }

        private void DisposeSidebarView()
        {
            if (sidebarViewModel == null)
            {
                return;
            }

            sidebarViewModel.Dispose();
            sidebarViewModel = null;
        }
    }
}
