using GamepadTester.Commands;
using GamepadTester.Services;
using GamepadTester.ViewModels;
using GamepadTester.Views;
using GamepadTester.Views.ThemeIntegration;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
        private GamepadTesterThemeIntegration themeIntegration;
        private global::GamepadTester.Commands.RelayCommand openTesterCommand;
        private global::GamepadTester.Commands.RelayCommand openButtonTestCommand;
        private global::GamepadTester.Commands.RelayCommand openSticksCommand;
        private global::GamepadTester.Commands.RelayCommand openRumbleCommand;
        private global::GamepadTester.Commands.RelayCommand openLatencyCommand;
        private ResourceDictionary englishFallbackResources;
        private Window testerWindow;
        private GamepadTesterViewModel testerWindowViewModel;
        private bool testerBackButtonHeld;

        public GamepadTesterSettings ThemeSettings
        {
            get { return settings.Settings; }
        }

        public GamepadTesterThemeIntegration ThemeIntegration
        {
            get { return themeIntegration; }
        }

        public override Guid Id
        {
            get { return pluginId; }
        }

        public GamepadTester(IPlayniteAPI api) : base(api)
        {
            settings = new GamepadTesterSettingsViewModel(this);
            openTesterCommand = new global::GamepadTester.Commands.RelayCommand(() => OpenTesterWindow(0, false));
            openButtonTestCommand = new global::GamepadTester.Commands.RelayCommand(() => OpenTesterWindow(0, true));
            openRumbleCommand = new global::GamepadTester.Commands.RelayCommand(() => OpenTesterWindow(1, true));
            openSticksCommand = new global::GamepadTester.Commands.RelayCommand(() => OpenTesterWindow(2, true));
            openLatencyCommand = new global::GamepadTester.Commands.RelayCommand(() => OpenTesterWindow(3, true));
            themeIntegration = new GamepadTesterThemeIntegration(settings, openTesterCommand, openButtonTestCommand, openSticksCommand, openRumbleCommand, openLatencyCommand);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
            EnsureEnglishFallbackResources();
            AddCustomElementSupport(new AddCustomElementSupportArgs
            {
                SourceName = "GamepadTester",
                ElementList = new List<string>
                {
                    "GamepadTesterLauncher",
                    "StatusBadge",
                    "ButtonMap",
                    "StickCheck",
                    "RumblePad",
                    "LatencyMini"
                }
            });
            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "GamepadTester",
                SettingsRoot = "ThemeIntegration"
            });
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
            testerWindowViewModel = null;
            testerWindow = null;
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
                Action = args2 => OpenTesterWindow(0, false)
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

        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            if (!settings.Settings.ShowTopPanelItem)
            {
                yield break;
            }

            yield return new TopPanelItem
            {
                Title = Loc("LOCGT_PluginName"),
                Visible = true,
                Icon = CreateSidebarIcon(),
                Activated = () => OpenTesterWindow(0, false)
            };
        }

        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (args == null)
            {
                return null;
            }

            if (IsThemeControlName(args.Name, "GamepadTesterLauncher"))
            {
                return new GamepadTesterThemeLauncherControl(() => OpenTesterWindow(0, true), Loc);
            }

            if (IsThemeControlName(args.Name, "StatusBadge"))
            {
                return new GamepadTesterStatusBadgeControl(settings.Settings, Loc);
            }

            if (IsThemeControlName(args.Name, "ButtonMap"))
            {
                return new GamepadTesterButtonMapControl(settings.Settings, Loc);
            }

            if (IsThemeControlName(args.Name, "StickCheck"))
            {
                return new GamepadTesterStickCheckControl(settings.Settings, Loc);
            }

            if (IsThemeControlName(args.Name, "RumblePad"))
            {
                return new GamepadTesterRumblePadControl(settings.Settings, Loc);
            }

            if (IsThemeControlName(args.Name, "LatencyMini"))
            {
                return new GamepadTesterLatencyMiniControl(settings.Settings, Loc);
            }

            return null;
        }

        private static bool IsThemeControlName(string actualName, string logicalName)
        {
            return string.Equals(actualName, logicalName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actualName, "GamepadTester" + logicalName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actualName, "GamepadTester_" + logicalName, StringComparison.OrdinalIgnoreCase);
        }

        public override void OnControllerButtonStateChanged(OnControllerButtonStateChangedArgs args)
        {
            if (args == null)
            {
                return;
            }

            HandleThemeControllerInput(args.Button, args.State);

            if (testerWindow == null || testerWindowViewModel == null)
            {
                return;
            }

            testerWindow.Dispatcher.BeginInvoke(new Action(() => HandleTesterControllerInput(args.Button, args.State)));
        }

        private void HandleThemeControllerInput(ControllerInput button, ControllerInputState state)
        {
            if (state != ControllerInputState.Pressed || button != ControllerInput.A)
            {
                return;
            }

            var dispatcher = Application.Current == null ? null : Application.Current.Dispatcher;
            if (dispatcher == null)
            {
                return;
            }

            dispatcher.BeginInvoke(new Action(ActivateFocusedThemeControl));
        }

        private void OpenTesterWindow(int selectedTabIndex, bool fullscreenSimplified)
        {
            try
            {
                if (testerWindow != null)
                {
                    if (testerWindowViewModel != null)
                    {
                        testerWindowViewModel.SelectedTabIndex = selectedTabIndex;
                        testerWindowViewModel.IsFullscreenSimplifiedMode = fullscreenSimplified;
                    }

                    testerWindow.Activate();
                    return;
                }

                GamepadTesterViewModel viewModel;
                var view = CreateTesterView(out viewModel);
                viewModel.SelectedTabIndex = selectedTabIndex;
                viewModel.IsFullscreenSimplifiedMode = fullscreenSimplified && PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen;
                var fullscreenFriendly = ShouldUseFullscreenFriendlyWindow();
                var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
                {
                    ShowMinimizeButton = !fullscreenFriendly,
                    ShowMaximizeButton = !fullscreenFriendly,
                    ShowCloseButton = true
                });

                window.Title = Loc("LOCGT_PluginName");
                window.Content = view;
                window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ApplyTesterWindowSize(window, fullscreenFriendly, 1280, 820, 1180, 760);
                testerWindow = window;
                testerWindowViewModel = viewModel;
                window.Closed += (sender, eventArgs) =>
                {
                    viewModel.Dispose();
                    if (ReferenceEquals(testerWindow, window))
                    {
                        testerWindow = null;
                        testerWindowViewModel = null;
                        testerBackButtonHeld = false;
                    }
                };
                window.PreviewKeyDown += CloseWindowOnEscape;

                window.Show();
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Failed to open Gamepad Tester.");
                PlayniteApi.Dialogs.ShowErrorMessage(exception.Message, Loc("LOCGT_PluginName"));
            }
        }

        private void HandleTesterControllerInput(ControllerInput button, ControllerInputState state)
        {
            if (testerWindow == null || testerWindowViewModel == null || !testerWindow.IsVisible)
            {
                return;
            }

            if (button == ControllerInput.Back)
            {
                testerBackButtonHeld = state == ControllerInputState.Pressed;
                return;
            }

            if (state != ControllerInputState.Pressed)
            {
                return;
            }

            if (testerBackButtonHeld && button == ControllerInput.A && testerWindowViewModel.IsFullscreenSimplifiedMode && testerWindowViewModel.SelectedTabIndex == 3)
            {
                if (testerWindowViewModel.StartLatencyTestCommand.CanExecute(null))
                {
                    testerWindowViewModel.StartLatencyTestCommand.Execute(null);
                }

                return;
            }

            if (button == ControllerInput.B)
            {
                testerWindow.Close();
                return;
            }

            if (button == ControllerInput.LeftShoulder)
            {
                testerWindowViewModel.MoveSelectedTab(-1);
                FocusFirstTesterControl();
                return;
            }

            if (button == ControllerInput.RightShoulder)
            {
                testerWindowViewModel.MoveSelectedTab(1);
                FocusFirstTesterControl();
                return;
            }

            if (button == ControllerInput.A)
            {
                ActivateFocusedControl();
                return;
            }

            if (button == ControllerInput.DPadUp)
            {
                MoveFocus(FocusNavigationDirection.Up);
                return;
            }

            if (button == ControllerInput.DPadDown)
            {
                MoveFocus(FocusNavigationDirection.Down);
                return;
            }

            if (button == ControllerInput.DPadLeft)
            {
                MoveFocus(FocusNavigationDirection.Left);
                return;
            }

            if (button == ControllerInput.DPadRight)
            {
                MoveFocus(FocusNavigationDirection.Right);
            }
        }

        private void ActivateFocusedControl()
        {
            var button = FindButtonFromFocus(Keyboard.FocusedElement as DependencyObject);
            if (button == null || !button.IsEnabled)
            {
                return;
            }

            ActivateButton(button);
        }

        private void ActivateFocusedThemeControl()
        {
            var focused = Keyboard.FocusedElement as DependencyObject;
            var button = FindButtonFromFocus(focused);
            if (button != null && button.IsEnabled && IsInsideGamepadTesterThemeControl(button))
            {
                ActivateButton(button);
                return;
            }

            var themeControl = FindThemeControlFromFocus(focused);
            if (themeControl == null)
            {
                return;
            }

            var firstButton = FindFirstEnabledButton(themeControl);
            if (firstButton != null)
            {
                ActivateButton(firstButton);
            }
        }

        private static ButtonBase FindButtonFromFocus(DependencyObject focused)
        {
            var current = focused;
            while (current != null)
            {
                var button = current as ButtonBase;
                if (button != null)
                {
                    return button;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private static void ActivateButton(ButtonBase button)
        {
            if (button.Command != null)
            {
                var parameter = button.CommandParameter;
                if (button.Command.CanExecute(parameter))
                {
                    button.Command.Execute(parameter);
                }

                return;
            }

            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        private static bool IsInsideGamepadTesterThemeControl(DependencyObject element)
        {
            var current = element;
            while (current != null)
            {
                if (current is GamepadTesterThemeControlBase || current is GamepadTesterThemeLauncherControl)
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private static GamepadTesterThemeControlBase FindThemeControlFromFocus(DependencyObject focused)
        {
            var current = focused;
            while (current != null)
            {
                var themeControl = current as GamepadTesterThemeControlBase;
                if (themeControl != null)
                {
                    return themeControl;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return FindDescendant<GamepadTesterThemeControlBase>(focused);
        }

        private static ButtonBase FindFirstEnabledButton(DependencyObject root)
        {
            if (root == null)
            {
                return null;
            }

            var rootButton = root as ButtonBase;
            if (rootButton != null && rootButton.IsEnabled)
            {
                return rootButton;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < childCount; i++)
            {
                var button = FindFirstEnabledButton(VisualTreeHelper.GetChild(root, i));
                if (button != null)
                {
                    return button;
                }
            }

            var contentControl = root as ContentControl;
            if (contentControl != null)
            {
                var content = contentControl.Content as DependencyObject;
                if (content != null)
                {
                    return FindFirstEnabledButton(content);
                }
            }

            return null;
        }

        private static T FindDescendant<T>(DependencyObject root)
            where T : DependencyObject
        {
            if (root == null)
            {
                return null;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var match = child as T;
                if (match != null)
                {
                    return match;
                }

                match = FindDescendant<T>(child);
                if (match != null)
                {
                    return match;
                }
            }

            var contentControl = root as ContentControl;
            if (contentControl != null)
            {
                var content = contentControl.Content as DependencyObject;
                if (content != null)
                {
                    return FindDescendant<T>(content);
                }
            }

            return null;
        }

        private void FocusFirstTesterControl()
        {
            MoveFocus(FocusNavigationDirection.First);
        }

        private void MoveFocus(FocusNavigationDirection direction)
        {
            if (testerWindow == null)
            {
                return;
            }

            var focused = Keyboard.FocusedElement as UIElement;
            if (focused == null)
            {
                focused = testerWindow;
            }

            focused.MoveFocus(new TraversalRequest(direction));
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
                            Data = Geometry.Parse("M17.32 5H6.68A4 4 0 0 0 2.702 8.59C2.696 8.642 2.692 8.691 2.685 8.742C2.604 9.416 2 14.456 2 16A3 3 0 0 0 5 19C6 19 6.5 18.5 7 18L8.414 16.586A2 2 0 0 1 9.828 16H14.172A2 2 0 0 1 15.586 16.586L17 18C17.5 18.5 18 19 19 19A3 3 0 0 0 22 16C22 14.455 21.396 9.416 21.315 8.742C21.308 8.692 21.304 8.642 21.298 8.591A4 4 0 0 0 17.32 5Z"),
                            Fill = Brushes.Transparent,
                            Stroke = Brushes.White,
                            StrokeThickness = 2,
                            StrokeStartLineCap = PenLineCap.Round,
                            StrokeEndLineCap = PenLineCap.Round,
                            StrokeLineJoin = PenLineJoin.Round
                        },
                        new System.Windows.Shapes.Path
                        {
                            Data = Geometry.Parse("M6 11H10M8 9V13M15 12H15.01M18 10H18.01"),
                            Fill = Brushes.Transparent,
                            Stroke = Brushes.White,
                            StrokeThickness = 2,
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

                var fullscreenFriendly = ShouldUseFullscreenFriendlyWindow();
                var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
                {
                    ShowMinimizeButton = false,
                    ShowMaximizeButton = !fullscreenFriendly,
                    ShowCloseButton = true
                });

                window.Title = Loc("LOCGT_GuidedTest");
                window.Content = view;
                window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ApplyTesterWindowSize(window, fullscreenFriendly, 1080, 820, 960, 720);
                window.PreviewKeyDown += CloseWindowOnEscape;
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

        private bool ShouldUseFullscreenFriendlyWindow()
        {
            return settings.Settings.UseFullscreenFriendlyWindow &&
                PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen;
        }

        private static void ApplyTesterWindowSize(Window window, bool fullscreenFriendly, double width, double height, double minWidth, double minHeight)
        {
            window.MinWidth = minWidth;
            window.MinHeight = minHeight;

            if (fullscreenFriendly)
            {
                window.WindowState = WindowState.Maximized;
                return;
            }

            window.Width = width;
            window.Height = height;
        }

        private static void CloseWindowOnEscape(object sender, KeyEventArgs eventArgs)
        {
            if (eventArgs.Key != Key.Escape)
            {
                return;
            }

            var window = sender as Window;
            if (window != null)
            {
                window.Close();
                eventArgs.Handled = true;
            }
        }
    }

    public class GamepadTesterThemeIntegration
    {
        private readonly GamepadTesterSettingsViewModel settings;

        public ICommand OpenTesterCommand { get; private set; }
        public ICommand OpenButtonTestCommand { get; private set; }
        public ICommand OpenSticksCommand { get; private set; }
        public ICommand OpenRumbleCommand { get; private set; }
        public ICommand OpenLatencyCommand { get; private set; }

        public bool ShowTopPanelItem
        {
            get { return settings.Settings.ShowTopPanelItem; }
        }

        public bool UseFullscreenFriendlyWindow
        {
            get { return settings.Settings.UseFullscreenFriendlyWindow; }
        }

        public GamepadTesterThemeIntegration(
            GamepadTesterSettingsViewModel settings,
            ICommand openTesterCommand,
            ICommand openButtonTestCommand,
            ICommand openSticksCommand,
            ICommand openRumbleCommand,
            ICommand openLatencyCommand)
        {
            this.settings = settings;
            OpenTesterCommand = openTesterCommand;
            OpenButtonTestCommand = openButtonTestCommand;
            OpenSticksCommand = openSticksCommand;
            OpenRumbleCommand = openRumbleCommand;
            OpenLatencyCommand = openLatencyCommand;
        }
    }
}
