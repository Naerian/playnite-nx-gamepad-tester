using GamepadTester.Services;
using GamepadTester.ViewModels;
using GamepadTester.Views;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace GamepadTester
{
    public class GamepadTester : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static readonly Guid pluginId = Guid.Parse("518dc982-32b5-4493-b32d-1f71de2fe4ad");

        private GamepadTesterSettingsViewModel settings { get; set; }
        private GamepadTesterViewModel sidebarViewModel;
        private ResourceDictionary englishFallbackResources;

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
            EnsureEnglishFallbackResources();
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
                MenuSection = "@" + Loc("LOCGT_PluginName"),
                Description = Loc("LOCGT_OpenGamepadTester"),
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
                Title = Loc("LOCGT_PluginName"),
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

                window.Title = Loc("LOCGT_PluginName");
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
                PlayniteApi.Dialogs.ShowErrorMessage(exception.Message, Loc("LOCGT_PluginName"));
            }
        }

        public string Loc(string key)
        {
            var value = PlayniteApi.Resources.GetString(key);
            if (!string.IsNullOrWhiteSpace(value) && value != key)
            {
                return value;
            }

            return GetEnglishFallbackString(key) ?? key;
        }

        private void EnsureEnglishFallbackResources()
        {
            try
            {
                englishFallbackResources = LoadEnglishFallbackResources();
                if (englishFallbackResources == null || Application.Current == null || Application.Current.Resources == null)
                {
                    return;
                }

                var alreadyLoaded = Application.Current.Resources.MergedDictionaries
                    .OfType<ResourceDictionary>()
                    .Any(a => ReferenceEquals(a, englishFallbackResources) ||
                        a.Contains("LOCGT_PluginName") && Equals(a["LOCGT_PluginName"], "Gamepad Tester"));
                if (!alreadyLoaded)
                {
                    Application.Current.Resources.MergedDictionaries.Insert(0, englishFallbackResources);
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load English fallback resources.");
            }
        }

        private ResourceDictionary LoadEnglishFallbackResources()
        {
            var path = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "Localization", "en_US.xaml");
            if (!File.Exists(path))
            {
                return null;
            }

            using (var stream = File.OpenRead(path))
            {
                return XamlReader.Load(stream) as ResourceDictionary;
            }
        }

        private string GetEnglishFallbackString(string key)
        {
            if (englishFallbackResources == null)
            {
                englishFallbackResources = LoadEnglishFallbackResources();
            }

            if (englishFallbackResources != null && englishFallbackResources.Contains(key))
            {
                var value = englishFallbackResources[key];
                return value == null ? null : value.ToString();
            }

            return null;
        }

        private GamepadTesterView CreateTesterView(out GamepadTesterViewModel viewModel)
        {
            var pollingService = new GamepadPollingService(new SdlGamepadProvider());
            viewModel = new GamepadTesterViewModel(pollingService, settings.Settings, Loc);
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
