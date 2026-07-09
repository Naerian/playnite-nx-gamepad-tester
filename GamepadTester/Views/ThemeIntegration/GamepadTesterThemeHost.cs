using GamepadTester.Views;
using System;
using System.Windows;
using System.Windows.Controls;

namespace GamepadTester.Views.ThemeIntegration
{
    public static class GamepadTesterThemeHost
    {
        public static readonly DependencyProperty BlockProperty =
            DependencyProperty.RegisterAttached(
                "Block",
                typeof(string),
                typeof(GamepadTesterThemeHost),
                new PropertyMetadata(null, OnBlockChanged));

        private static readonly object sync = new object();
        private static bool classHandlerRegistered;
        private static GamepadTesterSettings settings;
        private static Func<string, string> localizer;
        private static Action openTester;

        public static void Configure(GamepadTesterSettings pluginSettings, Func<string, string> pluginLocalizer, Action openTesterAction)
        {
            settings = pluginSettings;
            localizer = pluginLocalizer;
            openTester = openTesterAction;

            EnsureClassHandler();
        }

        public static string GetBlock(DependencyObject element)
        {
            return element == null ? null : (string)element.GetValue(BlockProperty);
        }

        public static void SetBlock(DependencyObject element, string value)
        {
            if (element != null)
            {
                element.SetValue(BlockProperty, value);
            }
        }

        private static void OnBlockChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
        {
            var contentControl = element as ContentControl;
            if (contentControl == null)
            {
                return;
            }

            EnsureClassHandler();
            InitializeHost(contentControl);
        }

        private static void EnsureClassHandler()
        {
            lock (sync)
            {
                if (classHandlerRegistered)
                {
                    return;
                }

                EventManager.RegisterClassHandler(
                    typeof(ContentControl),
                    FrameworkElement.LoadedEvent,
                    new RoutedEventHandler(OnContentControlLoaded));
                classHandlerRegistered = true;
            }
        }

        private static void OnContentControlLoaded(object sender, RoutedEventArgs args)
        {
            InitializeHost(sender as ContentControl);
        }

        private static void InitializeHost(ContentControl host)
        {
            if (host == null || settings == null)
            {
                return;
            }

            if (host.Content is GamepadTesterThemeControlBase || host.Content is GamepadTesterThemeLauncherControl)
            {
                return;
            }

            if (host.Content != null)
            {
                return;
            }

            var block = NormalizeBlock(GetBlock(host));
            if (string.IsNullOrWhiteSpace(block))
            {
                block = NormalizeHostName(host.Name);
            }

            var content = CreateBlock(block);
            if (content != null)
            {
                host.Content = content;
            }
        }

        private static Control CreateBlock(string block)
        {
            if (string.IsNullOrWhiteSpace(block))
            {
                return null;
            }

            if (IsBlock(block, "GamepadTesterLauncher"))
            {
                return new GamepadTesterThemeLauncherControl(openTester, localizer);
            }

            if (IsBlock(block, "StatusBadge"))
            {
                return new GamepadTesterStatusBadgeControl(settings, localizer);
            }

            if (IsBlock(block, "ButtonMap"))
            {
                return new GamepadTesterButtonMapControl(settings, localizer);
            }

            if (IsBlock(block, "StickCheck"))
            {
                return new GamepadTesterStickCheckControl(settings, localizer);
            }

            if (IsBlock(block, "RumblePad"))
            {
                return new GamepadTesterRumblePadControl(settings, localizer);
            }

            if (IsBlock(block, "LatencyMini"))
            {
                return new GamepadTesterLatencyMiniControl(settings, localizer);
            }

            return null;
        }

        private static string NormalizeBlock(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();
            if (normalized.StartsWith("GamepadTester_", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("GamepadTester_".Length);
            }
            else if (normalized.StartsWith("GamepadTester", StringComparison.OrdinalIgnoreCase) &&
                normalized.Length > "GamepadTester".Length)
            {
                normalized = normalized.Substring("GamepadTester".Length);
            }

            return normalized;
        }

        private static string NormalizeHostName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();
            if (normalized.StartsWith("GamepadTester_", StringComparison.OrdinalIgnoreCase))
            {
                return normalized.Substring("GamepadTester_".Length);
            }

            if (normalized.StartsWith("GamepadTester", StringComparison.OrdinalIgnoreCase) &&
                normalized.Length > "GamepadTester".Length)
            {
                return normalized.Substring("GamepadTester".Length);
            }

            return null;
        }

        private static bool IsBlock(string actual, string expected)
        {
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
