using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace GamepadTester
{
    public class GamepadTester : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static readonly Guid pluginId = Guid.Parse("518dc982-32b5-4493-b32d-1f71de2fe4ad");

        private readonly GamepadTesterSettingsViewModel settings;
        private ResourceDictionary englishFallbackResources;
        private bool retirementPromptShown;

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

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GamepadTesterSettingsView(this);
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem
            {
                MenuSection = "@" + Loc("LOCGT_PluginName"),
                Description = Loc("LOCGT_OpenRetirementNotice"),
                Action = itemArgs => ShowRetirementDialog()
            };
            yield return new MainMenuItem
            {
                MenuSection = "@" + Loc("LOCGT_PluginName"),
                Description = Loc("LOCGT_RetirementInstallControllerManager"),
                Action = itemArgs => InstallControllerManager()
            };
            yield return new MainMenuItem
            {
                MenuSection = "@" + Loc("LOCGT_PluginName"),
                Description = Loc("LOCGT_RetirementUninstallThis"),
                Action = itemArgs => UninstallThisPlugin()
            };
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            yield return new SidebarItem
            {
                Type = SiderbarItemType.View,
                Title = Loc("LOCGT_PluginName"),
                Visible = true,
                Icon = CreateSidebarIcon(),
                Opened = () => new GamepadTesterSettingsView(this)
            };
        }

        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            yield return new TopPanelItem
            {
                Title = Loc("LOCGT_RetirementHeadline"),
                Visible = true,
                Icon = CreateSidebarIcon(),
                Activated = ShowRetirementDialog
            };
        }

        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            return null;
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            PlayniteApi.Notifications.Add(new NotificationMessage(
                PluginRetirement.NotificationId,
                Loc("LOCGT_RetirementNotification"),
                NotificationType.Error,
                ShowRetirementDialog));

            if (retirementPromptShown)
            {
                return;
            }

            retirementPromptShown = true;
            ShowRetirementDialog();
        }

        public bool IsControllerManagerInstalled()
        {
            try
            {
                return PluginRetirement.IsControllerManagerInstalled(PlayniteApi.Addons.Addons);
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to detect Controller Manager.");
                return false;
            }
        }

        public void ShowRetirementDialog()
        {
            var install = new MessageBoxOption(Loc("LOCGT_RetirementInstallControllerManager"), !IsControllerManagerInstalled());
            var uninstall = new MessageBoxOption(Loc("LOCGT_RetirementUninstallThis"), IsControllerManagerInstalled());
            var later = new MessageBoxOption(Loc("LOCGT_RetirementLater"), false, true);
            var selected = PlayniteApi.Dialogs.ShowMessage(
                BuildRetirementMessage(),
                Loc("LOCGT_RetirementTitle"),
                MessageBoxImage.Warning,
                new List<MessageBoxOption> { install, uninstall, later });

            if (selected == install)
            {
                InstallControllerManager();
            }
            else if (selected == uninstall)
            {
                UninstallThisPlugin();
            }
        }

        public void InstallControllerManager()
        {
            if (IsControllerManagerInstalled())
            {
                PlayniteApi.Dialogs.ShowMessage(
                    Loc("LOCGT_RetirementControllerManagerInstalled"),
                    Loc("LOCGT_RetirementTitle"));
                return;
            }

            string error;
            if (!PluginRetirement.TryOpenControllerManagerInstall(out error))
            {
                PlayniteApi.Dialogs.ShowErrorMessage(
                    string.IsNullOrWhiteSpace(error) ? Loc("LOCGT_RetirementInstallFailed") : error,
                    Loc("LOCGT_RetirementTitle"));
            }
        }

        public void UninstallThisPlugin()
        {
            string error;
            if (!PluginRetirement.TryQueueUninstall(
                    PlayniteApi.Paths.ConfigurationPath,
                    Path.GetDirectoryName(GetType().Assembly.Location),
                    out error))
            {
                PlayniteApi.Dialogs.ShowErrorMessage(
                    Loc("LOCGT_RetirementQueueFailed") +
                    Environment.NewLine + Environment.NewLine +
                    Loc("LOCGT_RetirementManualUninstallHint") +
                    (string.IsNullOrWhiteSpace(error) ? string.Empty : Environment.NewLine + error),
                    Loc("LOCGT_RetirementTitle"));
                return;
            }

            if (PlayniteApi.Dialogs.ShowMessage(
                    Loc("LOCGT_RetirementQueuedRestart"),
                    Loc("LOCGT_RetirementTitle"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            RestartPlayniteApplication();
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

        private string BuildRetirementMessage()
        {
            var parts = new List<string>
            {
                Loc("LOCGT_RetirementHeadline"),
                string.Empty,
                Loc("LOCGT_RetirementBody"),
                string.Empty,
                Loc("LOCGT_RetirementConflict"),
                string.Empty,
                Loc("LOCGT_RetirementSteps")
            };

            if (IsControllerManagerInstalled())
            {
                parts.Add(string.Empty);
                parts.Add(Loc("LOCGT_RetirementControllerManagerInstalled"));
            }

            parts.Add(string.Empty);
            parts.Add(Loc("LOCGT_RetirementManualUninstallHint"));
            return string.Join(Environment.NewLine, parts);
        }

        private void RestartPlayniteApplication()
        {
            try
            {
                var appType = Type.GetType("Playnite.PlayniteApplication, Playnite", false);
                if (appType == null)
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (!string.Equals(assembly.GetName().Name, "Playnite", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        appType = assembly.GetType("Playnite.PlayniteApplication", false);
                        if (appType != null)
                        {
                            break;
                        }
                    }
                }

                if (appType == null)
                {
                    logger.Error("PlayniteApplication type was not found; cannot restart.");
                    return;
                }

                var current = appType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
                var instance = current == null ? null : current.GetValue(null, null);
                if (instance == null)
                {
                    logger.Error("PlayniteApplication.Current was null; cannot restart.");
                    return;
                }

                var restartWithBool = instance.GetType().GetMethod(
                    "Restart",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(bool) },
                    null);
                if (restartWithBool != null)
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke(
                        new Action(() => restartWithBool.Invoke(instance, new object[] { true })),
                        DispatcherPriority.ApplicationIdle);
                    return;
                }

                var restart = instance.GetType().GetMethod(
                    "Restart",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null);
                if (restart != null)
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke(
                        new Action(() => restart.Invoke(instance, null)),
                        DispatcherPriority.ApplicationIdle);
                    return;
                }

                logger.Error("PlayniteApplication.Restart method was not found.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to restart Playnite after queuing Gamepad Tester uninstall.");
            }
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
                        (a.Contains("LOCGT_PluginName") && Equals(a["LOCGT_PluginName"], "Gamepad Tester")));
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

        private static FrameworkElement CreateSidebarIcon()
        {
            var iconPath = new System.Windows.Shapes.Path
            {
                Data = LoadSidebarIconGeometry(),
                Fill = Brushes.White
            };

            var themeForegroundBinding = new System.Windows.Data.Binding("Foreground")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(
                    System.Windows.Data.RelativeSourceMode.FindAncestor,
                    typeof(Control),
                    1),
                FallbackValue = Brushes.White
            };

            iconPath.SetBinding(System.Windows.Shapes.Shape.FillProperty, themeForegroundBinding);

            var canvas = new Canvas
            {
                Width = 511.983,
                Height = 511.983
            };
            canvas.Children.Add(iconPath);

            return new Viewbox
            {
                Width = 22,
                Height = 22,
                Stretch = Stretch.Uniform,
                Child = canvas
            };
        }

        private static Geometry LoadSidebarIconGeometry()
        {
            const string resourceName = "GamepadTester.Icons.gamepad-2.svg";

            try
            {
                using (var stream = typeof(GamepadTester).Assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new InvalidOperationException("Embedded sidebar icon was not found.");
                    }

                    var document = System.Xml.Linq.XDocument.Load(stream);
                    var geometry = new GeometryGroup { FillRule = FillRule.Nonzero };
                    foreach (var pathElement in document.Descendants().Where(element => element.Name.LocalName == "path"))
                    {
                        var data = pathElement.Attribute("d");
                        if (data != null && !string.IsNullOrWhiteSpace(data.Value))
                        {
                            geometry.Children.Add(Geometry.Parse(data.Value));
                        }
                    }

                    if (geometry.Children.Count == 0)
                    {
                        throw new InvalidOperationException("Embedded sidebar icon has no path geometry.");
                    }

                    geometry.Freeze();
                    return geometry;
                }
            }
            catch (Exception exception)
            {
                logger.Warn(exception, "Failed to load the embedded Gamepad Tester sidebar icon.");
                var fallback = Geometry.Parse("M17.32 5H6.68A4 4 0 0 0 2.702 8.59C2.604 9.416 2 14.456 2 16A3 3 0 0 0 5 19C6 19 6.5 18.5 7 18L8.414 16.586A2 2 0 0 1 9.828 16H14.172A2 2 0 0 1 15.586 16.586L17 18C17.5 18.5 18 19 19 19A3 3 0 0 0 22 16C22 14.455 21.396 9.416 21.298 8.591A4 4 0 0 0 17.32 5Z").Clone();
                fallback.Transform = new ScaleTransform(21.332625, 21.332625);
                fallback.Freeze();
                return fallback;
            }
        }
    }
}
