using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace GamepadTester
{
    public class GamepadTesterSettings : ObservableObject
    {
        private bool showSidebarItem = true;
        private bool autoResetDiagnosticsOnControllerChange = true;
        private bool showDeviceSelectorWhenSingleController = false;
        private bool enableRumbleTests = true;

        public bool ShowSidebarItem
        {
            get { return showSidebarItem; }
            set { SetValue(ref showSidebarItem, value); }
        }

        public bool AutoResetDiagnosticsOnControllerChange
        {
            get { return autoResetDiagnosticsOnControllerChange; }
            set { SetValue(ref autoResetDiagnosticsOnControllerChange, value); }
        }

        public bool ShowDeviceSelectorWhenSingleController
        {
            get { return showDeviceSelectorWhenSingleController; }
            set { SetValue(ref showDeviceSelectorWhenSingleController, value); }
        }

        public bool EnableRumbleTests
        {
            get { return enableRumbleTests; }
            set { SetValue(ref enableRumbleTests, value); }
        }
    }

    public class GamepadTesterSettingsViewModel : ObservableObject, ISettings
    {
        private readonly GamepadTester plugin;
        private GamepadTesterSettings editingClone;
        private GamepadTesterSettings settings;

        public GamepadTesterSettings Settings
        {
            get { return settings; }
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public GamepadTesterSettingsViewModel(GamepadTester plugin)
        {
            this.plugin = plugin;
            Settings = plugin.LoadPluginSettings<GamepadTesterSettings>() ?? new GamepadTesterSettings();
        }

        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            Settings = editingClone;
        }

        public void EndEdit()
        {
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
