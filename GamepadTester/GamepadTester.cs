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
using System.Windows.Media;

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
                Icon = CreateSidebarIcon(),
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

        private static FrameworkElement CreateSidebarIcon()
        {
            var icon = new Viewbox
            {
                Width = 22,
                Height = 22,
                Stretch = Stretch.Uniform,
                Child = new Canvas
                {
                    Width = 24,
                    Height = 24,
                    Children =
                    {
                        new System.Windows.Shapes.Path
                        {
                            Data = Geometry.Parse("M7.2 8.2H16.8C19.7 8.2 21.8 10.4 22.2 13.8L22.6 17.1C22.9 19.4 20.4 20.7 18.8 19L16.4 16.4H7.6L5.2 19C3.6 20.7 1.1 19.4 1.4 17.1L1.8 13.8C2.2 10.4 4.3 8.2 7.2 8.2Z"),
                            Fill = Brushes.Transparent,
                            Stroke = Brushes.White,
                            StrokeThickness = 1.7,
                            StrokeStartLineCap = PenLineCap.Round,
                            StrokeEndLineCap = PenLineCap.Round,
                            StrokeLineJoin = PenLineJoin.Round
                        },
                        new System.Windows.Shapes.Path
                        {
                            Data = Geometry.Parse("M7 12V15M5.5 13.5H8.5M16.2 12.2H16.25M18.4 14.5H18.45"),
                            Fill = Brushes.Transparent,
                            Stroke = Brushes.White,
                            StrokeThickness = 1.7,
                            StrokeStartLineCap = PenLineCap.Round,
                            StrokeEndLineCap = PenLineCap.Round,
                            StrokeLineJoin = PenLineJoin.Round
                        }
                    }
                }
            };

            return icon;
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
            viewModel = new GamepadTesterViewModel(pollingService, settings.Settings, Loc, OpenGuidedTestWindow);
            var view = new GamepadTesterView
            {
                DataContext = viewModel
            };

            viewModel.Start();
            return view;
        }

        private void OpenGuidedTestWindow(GamepadTesterViewModel viewModel)
        {
            try
            {
                if (viewModel == null || !viewModel.State.IsConnected)
                {
                    return;
                }

                viewModel.StartGuidedTestCommand.Execute(null);
                var view = new GuidedTestView
                {
                    DataContext = viewModel
                };

                var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
                {
                    ShowMinimizeButton = false,
                    ShowMaximizeButton = true
                });

                window.Title = Loc("LOCGT_GuidedTest");
                window.Width = 1080;
                window.Height = 820;
                window.MinWidth = 960;
                window.MinHeight = 720;
                window.Content = view;
                window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Show();
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Failed to open Gamepad Tester guided test.");
                PlayniteApi.Dialogs.ShowErrorMessage(exception.Message, Loc("LOCGT_GuidedTest"));
            }
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
