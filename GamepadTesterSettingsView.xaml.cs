using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace GamepadTester
{
    public partial class GamepadTesterSettingsView : UserControl
    {
        public GamepadTesterSettingsView()
        {
            InitializeComponent();
        }

        public string InstalledVersionAuthorText
        {
            get
            {
                var version = typeof(GamepadTesterSettingsView).Assembly.GetName().Version.ToString(3);
                return "Gamepad Tester " + version + " · Narian";
            }
        }

        private void OpenExternalLink(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var url = button == null ? null : button.Tag as string;
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
    }
}
