using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace GamepadTester
{
    public class GamepadTesterSettings : ObservableObject
    {
        private bool showSidebarItem = true;
        private bool showTopPanelItem = true;
        private bool useFullscreenFriendlyWindow = true;
        private bool autoResetDiagnosticsOnControllerChange = true;
        private bool showDeviceSelectorWhenSingleController = false;
        private bool enableRumbleTests = true;
        private bool enableInputLogByDefault;
        private double healthyDeadzone = 0.08d;
        private double minorDriftThreshold = 0.14d;
        private double attentionDriftThreshold = 0.20d;
        private double stickEdgeThreshold = 0.85d;
        private double triggerFullPressThreshold = 0.95d;
        private int centerCalibrationMilliseconds = 2200;

        public bool ShowSidebarItem
        {
            get { return showSidebarItem; }
            set { SetValue(ref showSidebarItem, value); }
        }

        public bool ShowTopPanelItem
        {
            get { return showTopPanelItem; }
            set { SetValue(ref showTopPanelItem, value); }
        }

        public bool UseFullscreenFriendlyWindow
        {
            get { return useFullscreenFriendlyWindow; }
            set { SetValue(ref useFullscreenFriendlyWindow, value); }
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

        public bool EnableInputLogByDefault
        {
            get { return enableInputLogByDefault; }
            set { SetValue(ref enableInputLogByDefault, value); }
        }

        public double HealthyDeadzone
        {
            get { return healthyDeadzone; }
            set { SetValue(ref healthyDeadzone, value); }
        }

        public double MinorDriftThreshold
        {
            get { return minorDriftThreshold; }
            set { SetValue(ref minorDriftThreshold, value); }
        }

        public double AttentionDriftThreshold
        {
            get { return attentionDriftThreshold; }
            set { SetValue(ref attentionDriftThreshold, value); }
        }

        public double StickEdgeThreshold
        {
            get { return stickEdgeThreshold; }
            set { SetValue(ref stickEdgeThreshold, value); }
        }

        public double TriggerFullPressThreshold
        {
            get { return triggerFullPressThreshold; }
            set { SetValue(ref triggerFullPressThreshold, value); }
        }

        public int CenterCalibrationMilliseconds
        {
            get { return centerCalibrationMilliseconds; }
            set { SetValue(ref centerCalibrationMilliseconds, value); }
        }

        public void Normalize()
        {
            HealthyDeadzone = Clamp(HealthyDeadzone, 0.02d, 0.30d);
            MinorDriftThreshold = Clamp(MinorDriftThreshold, HealthyDeadzone + 0.01d, 0.40d);
            AttentionDriftThreshold = Clamp(AttentionDriftThreshold, MinorDriftThreshold + 0.01d, 0.60d);
            StickEdgeThreshold = Clamp(StickEdgeThreshold, 0.50d, 1.00d);
            TriggerFullPressThreshold = Clamp(TriggerFullPressThreshold, 0.50d, 1.00d);
            CenterCalibrationMilliseconds = (int)Clamp(CenterCalibrationMilliseconds, 800, 6000);
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            if (value > maximum)
            {
                return maximum;
            }

            return value;
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
            Settings.Normalize();
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
