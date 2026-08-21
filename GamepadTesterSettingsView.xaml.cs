using System.Windows;
using System.Windows.Controls;

namespace GamepadTester
{
    public partial class GamepadTesterSettingsView : UserControl
    {
        private readonly GamepadTester plugin;

        public GamepadTesterSettingsView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public GamepadTesterSettingsView(GamepadTester plugin) : this()
        {
            this.plugin = plugin;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (plugin == null)
            {
                return;
            }

            if (plugin.IsControllerManagerInstalled())
            {
                StatusText.Text = plugin.Loc("LOCGT_RetirementControllerManagerInstalled");
                InstallButton.IsEnabled = false;
            }
            else
            {
                StatusText.Text = plugin.Loc("LOCGT_RetirementSteps");
                InstallButton.IsEnabled = true;
            }
        }

        private void InstallControllerManager(object sender, RoutedEventArgs e)
        {
            if (plugin == null)
            {
                return;
            }

            plugin.InstallControllerManager();
            RefreshStatus();
        }

        private void UninstallGamepadTester(object sender, RoutedEventArgs e)
        {
            if (plugin == null)
            {
                return;
            }

            plugin.UninstallThisPlugin();
        }
    }
}
