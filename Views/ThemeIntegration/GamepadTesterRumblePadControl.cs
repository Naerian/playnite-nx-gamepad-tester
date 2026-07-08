using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace GamepadTester.Views.ThemeIntegration
{
    public sealed class GamepadTesterRumblePadControl : GamepadTesterThemeControlBase
    {
        private Button firstButton;

        public GamepadTesterRumblePadControl(GamepadTesterSettings settings, Func<string, string> localizer)
            : base(settings, localizer)
        {
            var panel = Panel();
            var root = new StackPanel();

            var header = Text(L("LOCGT_RumbleTests", "Rumble tests"), 14, FontWeights.SemiBold);
            header.Margin = new Thickness(0, 0, 0, 12);
            root.Children.Add(header);

            var buttons = new UniformGrid
            {
                Columns = 2
            };
            firstButton = CreateButton(L("LOCGT_RumbleLight", "Light"), "LightRumbleCommand");
            buttons.Children.Add(firstButton);
            buttons.Children.Add(CreateButton(L("LOCGT_RumbleMedium", "Medium"), "MediumRumbleCommand"));
            buttons.Children.Add(CreateButton(L("LOCGT_RumbleHeavy", "Heavy"), "HeavyRumbleCommand"));
            buttons.Children.Add(CreateButton(L("LOCGT_RumblePulse", "Pulse"), "PulseRumbleCommand"));
            root.Children.Add(buttons);

            var status = Text(string.Empty, 12, FontWeights.Normal);
            status.Margin = new Thickness(0, 12, 0, 0);
            status.Opacity = 0.72;
            status.SetBinding(TextBlock.TextProperty, Bind("RumbleStatusLabel"));
            root.Children.Add(status);

            panel.Child = root;
            Content = panel;
            Loaded += (sender, args) => FocusFirstButton();
            IsVisibleChanged += (sender, args) => FocusFirstButton();
        }

        private Button CreateButton(string text, string commandPath)
        {
            var button = new Button
            {
                Content = text,
                MinHeight = 44,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(12, 8, 12, 8),
                Cursor = Cursors.Hand,
                Foreground = DynamicBrush("TextBrush", Brushes.White),
                Background = DynamicBrush("ButtonBackgroundBrush", new SolidColorBrush(Color.FromRgb(31, 36, 47))),
                BorderBrush = DynamicBrush("ControlBorderBrush", new SolidColorBrush(Color.FromRgb(68, 77, 92))),
                BorderThickness = new Thickness(1)
            };
            button.SetBinding(Button.CommandProperty, new Binding(commandPath));
            return button;
        }

        private void FocusFirstButton()
        {
            if (!IsVisible || firstButton == null)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsVisible && firstButton.IsEnabled)
                {
                    firstButton.Focus();
                    Keyboard.Focus(firstButton);
                }
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }
    }
}
