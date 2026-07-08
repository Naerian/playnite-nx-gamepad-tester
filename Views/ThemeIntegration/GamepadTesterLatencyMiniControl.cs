using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace GamepadTester.Views.ThemeIntegration
{
    public sealed class GamepadTesterLatencyMiniControl : GamepadTesterThemeControlBase
    {
        public GamepadTesterLatencyMiniControl(GamepadTesterSettings settings, Func<string, string> localizer)
            : base(settings, localizer)
        {
            var panel = Panel();
            var root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition());
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var info = new StackPanel();
            var header = Text(L("LOCGT_LatencyTitle", "Polling latency"), 14, FontWeights.SemiBold);
            header.Margin = new Thickness(0, 0, 0, 8);
            info.Children.Add(header);

            var rate = Text(string.Empty, 40, FontWeights.Bold);
            rate.SetBinding(TextBlock.TextProperty, Bind("PollingRateCurrentLabel"));
            info.Children.Add(rate);

            var stats = Text(string.Empty, 12, FontWeights.Normal);
            stats.Opacity = 0.72;
            stats.SetBinding(TextBlock.TextProperty, Bind("PollingRateAverageValueLabel"));
            info.Children.Add(stats);

            var samples = Text(string.Empty, 12, FontWeights.Normal);
            samples.Opacity = 0.72;
            samples.SetBinding(TextBlock.TextProperty, Bind("LatencySampleCountLabel"));
            info.Children.Add(samples);

            root.Children.Add(info);

            var button = new Button
            {
                MinWidth = 140,
                MinHeight = 48,
                Margin = new Thickness(18, 0, 0, 0),
                Padding = new Thickness(14, 8, 14, 8),
                Cursor = Cursors.Hand,
                Foreground = DynamicBrush("TextBrush", Brushes.White),
                Background = DynamicBrush("ButtonBackgroundBrush", new SolidColorBrush(Color.FromRgb(31, 36, 47))),
                BorderBrush = DynamicBrush("ControlBorderBrush", new SolidColorBrush(Color.FromRgb(68, 77, 92))),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center
            };
            button.SetBinding(Button.CommandProperty, new Binding("StartLatencyTestCommand"));
            button.SetBinding(ContentControl.ContentProperty, new Binding("StartLatencyButtonLabel"));
            Grid.SetColumn(button, 1);
            root.Children.Add(button);

            panel.Child = root;
            Content = panel;
        }
    }
}
