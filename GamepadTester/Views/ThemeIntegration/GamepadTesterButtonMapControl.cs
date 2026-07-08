using GamepadTester.Views.ControllerLayouts;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GamepadTester.Views.ThemeIntegration
{
    public sealed class GamepadTesterButtonMapControl : GamepadTesterThemeControlBase
    {
        public GamepadTesterButtonMapControl(GamepadTesterSettings settings, Func<string, string> localizer)
            : base(settings, localizer)
        {
            var panel = Panel();
            var root = new Grid
            {
                MinWidth = 420,
                MinHeight = 280
            };

            var content = new StackPanel();
            var header = Text(L("LOCGT_TestDetails", "Test details"), 14, FontWeights.SemiBold);
            header.Margin = new Thickness(0, 0, 0, 10);
            content.Children.Add(header);

            var host = new Grid
            {
                Width = 620,
                Height = 390,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            AddLayout(host, new DualSenseControllerTesterView(), "IsDualSenseLayout");
            AddLayout(host, new XboxSeriesControllerTesterView(), "IsXboxSeriesVisualScheme");
            AddLayout(host, new XboxControllerTesterView(), "IsXboxOneVisualScheme");
            AddLayout(host, new SteamControllerTesterView(), "IsSteamControllerVisualScheme");
            AddLayout(host, new PlayStationControllerTesterView(), "IsPlayStationVisualScheme");
            AddLayout(host, new SwitchProControllerTesterView(), "IsSwitchProVisualScheme");
            AddLayout(host, new EightBitDoUltimateTesterView(), "IsEightBitDoUltimateVisualScheme");
            AddLayout(host, new EightBitDoProTesterView(), "IsEightBitDoProVisualScheme");
            AddLayout(host, new UniversalControllerTesterView(), "IsUniversalControllerArtwork");

            content.Children.Add(new Viewbox
            {
                Stretch = Stretch.Uniform,
                MaxHeight = 380,
                Child = host
            });

            root.Children.Add(content);

            var noController = CreateEmptyState();
            noController.SetBinding(VisibilityProperty, Bind("HasController", InverseBoolToVisibility()));
            root.Children.Add(noController);

            panel.Child = root;
            Content = panel;
        }

        private static void AddLayout(Panel host, FrameworkElement layout, string visibilityPath)
        {
            layout.HorizontalAlignment = HorizontalAlignment.Center;
            layout.VerticalAlignment = VerticalAlignment.Center;
            layout.SetBinding(WidthProperty, Bind("ControllerVisualWidth"));
            layout.SetBinding(HeightProperty, Bind("ControllerVisualHeight"));
            layout.SetBinding(VisibilityProperty, Bind(visibilityPath, BoolToVisibility()));
            host.Children.Add(layout);
        }

        private Border CreateEmptyState()
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 14, 17, 22)),
                CornerRadius = new CornerRadius(8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            border.Child = new TextBlock
            {
                Text = L("LOCGT_NoControllerDetected", "No controller detected"),
                Foreground = DynamicBrush("TextBrush", Brushes.White),
                FontSize = 22,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            return border;
        }
    }
}
