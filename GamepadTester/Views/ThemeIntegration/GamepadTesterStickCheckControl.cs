using ControllerLayoutPrimitives;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GamepadTester.Views.ThemeIntegration
{
    public sealed class GamepadTesterStickCheckControl : GamepadTesterThemeControlBase
    {
        public GamepadTesterStickCheckControl(GamepadTesterSettings settings, Func<string, string> localizer)
            : base(settings, localizer)
        {
            var panel = Panel();
            var root = new StackPanel();

            var header = Text(L("LOCGT_Sticks", "Sticks"), 14, FontWeights.SemiBold);
            header.Margin = new Thickness(0, 0, 0, 12);
            root.Children.Add(header);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var left = CreateStickBlock(
                L("LOCGT_LeftStick", "Left stick"),
                "LeftStickDotX",
                "LeftStickDotY",
                new SolidColorBrush(Color.FromRgb(76, 201, 240)),
                "LeftStickVector",
                "LeftStickDriftStatus",
                "LeftStickCurrentMagnitudeLabel");
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var right = (FrameworkElement)CreateStickBlock(
                L("LOCGT_RightStick", "Right stick"),
                "RightStickDotX",
                "RightStickDotY",
                new SolidColorBrush(Color.FromRgb(247, 185, 85)),
                "RightStickVector",
                "RightStickDriftStatus",
                "RightStickCurrentMagnitudeLabel");
            right.Margin = new Thickness(18, 0, 0, 0);
            Grid.SetColumn(right, 1);
            grid.Children.Add(right);

            root.Children.Add(grid);
            panel.Child = root;
            Content = panel;
        }

        private UIElement CreateStickBlock(string title, string xPath, string yPath, Brush dotBrush, string vectorPath, string driftPath, string magnitudePath)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition());

            var stick = new StickIndicatorView
            {
                OuterSize = 116,
                DotSize = 42,
                DotFill = dotBrush,
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            stick.SetBinding(StickIndicatorView.DotXProperty, Bind(xPath));
            stick.SetBinding(StickIndicatorView.DotYProperty, Bind(yPath));
            Grid.SetColumn(stick, 0);
            row.Children.Add(stick);

            var info = new StackPanel
            {
                Margin = new Thickness(14, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            info.Children.Add(Text(title, 13, FontWeights.SemiBold));
            info.Children.Add(BoundText(vectorPath, 12, 0.72));
            info.Children.Add(BoundText(driftPath, 12, 0.72));
            info.Children.Add(BoundText(magnitudePath, 12, 0.72));
            Grid.SetColumn(info, 1);
            row.Children.Add(info);

            return row;
        }

        private static TextBlock BoundText(string path, double size, double opacity)
        {
            var text = Text(string.Empty, size, FontWeights.Normal);
            text.Opacity = opacity;
            text.SetBinding(TextBlock.TextProperty, Bind(path));
            return text;
        }
    }
}
