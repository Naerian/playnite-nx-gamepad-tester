using GamepadTester.Commands;
using GamepadTester.Models;
using GamepadTester.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GamepadTester.ViewModels
{
    public sealed class GamepadTesterViewModel : ObservableObject, IDisposable
    {
        private const double StickRadius = 34d;
        private readonly GamepadPollingService pollingService;
        private readonly RelayCommand rumbleCommand;
        private readonly RelayCommand lightRumbleCommand;
        private readonly RelayCommand mediumRumbleCommand;
        private readonly RelayCommand heavyRumbleCommand;
        private readonly RelayCommand lowMotorRumbleCommand;
        private readonly RelayCommand highMotorRumbleCommand;
        private readonly RelayCommand pulseRumbleCommand;
        private readonly RelayCommand alternatingRumbleCommand;
        private readonly RelayCommand rampRumbleCommand;
        private readonly RelayCommand resetDiagnosticsCommand;
        private readonly StickDiagnosticsTracker leftStickDiagnostics;
        private readonly StickDiagnosticsTracker rightStickDiagnostics;
        private GamepadState state;
        private GamepadControllerInfo selectedController;
        private int controllerRefreshTick;
        private GamepadButtonState previousButtons;
        private GamepadButtonState coveredButtons;
        private double maxLeftRestDrift;
        private double maxRightRestDrift;
        private double maxLeftStickMagnitude;
        private double maxRightStickMagnitude;
        private float maxLeftTrigger;
        private float maxRightTrigger;
        private bool isControllerSelectorOpen;
        private bool isRumbleRunning;
        private string rumbleStatusLabel;

        public GamepadTesterViewModel(GamepadPollingService pollingService)
        {
            this.pollingService = pollingService;
            state = new GamepadState();
            Controllers = new ObservableCollection<GamepadControllerInfo>();
            InputHistory = new ObservableCollection<InputHistoryItem>();
            rumbleCommand = new RelayCommand(() => RunSimpleRumble("Standard rumble", 42000, 52000, 350), CanRunRumble);
            lightRumbleCommand = new RelayCommand(() => RunSimpleRumble("Light rumble", 14000, 18000, 260), CanRunRumble);
            mediumRumbleCommand = new RelayCommand(() => RunSimpleRumble("Medium rumble", 28000, 36000, 360), CanRunRumble);
            heavyRumbleCommand = new RelayCommand(() => RunSimpleRumble("Heavy rumble", 52000, 62000, 520), CanRunRumble);
            lowMotorRumbleCommand = new RelayCommand(() => RunSimpleRumble("Low-frequency motor", 52000, 0, 520), CanRunRumble);
            highMotorRumbleCommand = new RelayCommand(() => RunSimpleRumble("High-frequency motor", 0, 56000, 520), CanRunRumble);
            pulseRumbleCommand = new RelayCommand(TestPulseRumble, CanRunRumble);
            alternatingRumbleCommand = new RelayCommand(TestAlternatingRumble, CanRunRumble);
            rampRumbleCommand = new RelayCommand(TestRampRumble, CanRunRumble);
            resetDiagnosticsCommand = new RelayCommand(ResetDiagnostics);
            leftStickDiagnostics = new StickDiagnosticsTracker();
            rightStickDiagnostics = new StickDiagnosticsTracker();
            coveredButtons = new GamepadButtonState();
            rumbleStatusLabel = "Ready";
            pollingService.StateUpdated += OnStateUpdated;
        }

        public ObservableCollection<GamepadControllerInfo> Controllers { get; private set; }
        public ObservableCollection<InputHistoryItem> InputHistory { get; private set; }

        public GamepadState State
        {
            get { return state; }
            private set
            {
                state = value;
                NotifyStateChanged();
            }
        }

        public ICommand RumbleCommand
        {
            get { return rumbleCommand; }
        }

        public ICommand LightRumbleCommand
        {
            get { return lightRumbleCommand; }
        }

        public ICommand MediumRumbleCommand
        {
            get { return mediumRumbleCommand; }
        }

        public ICommand HeavyRumbleCommand
        {
            get { return heavyRumbleCommand; }
        }

        public ICommand LowMotorRumbleCommand
        {
            get { return lowMotorRumbleCommand; }
        }

        public ICommand HighMotorRumbleCommand
        {
            get { return highMotorRumbleCommand; }
        }

        public ICommand PulseRumbleCommand
        {
            get { return pulseRumbleCommand; }
        }

        public ICommand AlternatingRumbleCommand
        {
            get { return alternatingRumbleCommand; }
        }

        public ICommand RampRumbleCommand
        {
            get { return rampRumbleCommand; }
        }

        public ICommand ResetDiagnosticsCommand
        {
            get { return resetDiagnosticsCommand; }
        }

        public GamepadControllerInfo SelectedController
        {
            get { return selectedController; }
            set
            {
                if (selectedController == value)
                {
                    return;
                }

                selectedController = value;
                OnPropertyChanged("SelectedController");

                if (selectedController != null)
                {
                    pollingService.SelectController(selectedController.InstanceId);
                    ResetDiagnostics();
                }
            }
        }

        public bool IsControllerSelectorVisible
        {
            get { return Controllers.Count > 1; }
        }

        public bool IsControllerSelectorOpen
        {
            get { return isControllerSelectorOpen; }
            set
            {
                if (isControllerSelectorOpen == value)
                {
                    return;
                }

                isControllerSelectorOpen = value;
                OnPropertyChanged("IsControllerSelectorOpen");
            }
        }

        public string RumbleStatusLabel
        {
            get { return rumbleStatusLabel; }
        }

        public double LeftStickDotX
        {
            get { return State.LeftStick.X * StickRadius; }
        }

        public double LeftStickDotY
        {
            get { return -State.LeftStick.Y * StickRadius; }
        }

        public double RightStickDotX
        {
            get { return State.RightStick.X * StickRadius; }
        }

        public double RightStickDotY
        {
            get { return -State.RightStick.Y * StickRadius; }
        }

        public double LeftStickDiagnosticsDotX
        {
            get { return State.LeftStick.X * 108d; }
        }

        public double LeftStickDiagnosticsDotY
        {
            get { return -State.LeftStick.Y * 108d; }
        }

        public double RightStickDiagnosticsDotX
        {
            get { return State.RightStick.X * 108d; }
        }

        public double RightStickDiagnosticsDotY
        {
            get { return -State.RightStick.Y * 108d; }
        }

        public int LeftTriggerPercent
        {
            get { return (int)Math.Round(State.LeftTrigger * 100); }
        }

        public int RightTriggerPercent
        {
            get { return (int)Math.Round(State.RightTrigger * 100); }
        }

        public bool IsLeftTriggerActive
        {
            get { return State.LeftTrigger > 0.02f; }
        }

        public bool IsRightTriggerActive
        {
            get { return State.RightTrigger > 0.02f; }
        }

        public double LeftTriggerMapWidth
        {
            get { return State.LeftTrigger * 164d; }
        }

        public double RightTriggerMapWidth
        {
            get { return State.RightTrigger * 164d; }
        }

        public double LeftTriggerThinMapWidth
        {
            get { return State.LeftTrigger * 122d; }
        }

        public double RightTriggerThinMapWidth
        {
            get { return State.RightTrigger * 122d; }
        }

        public int LeftStickDriftPercent
        {
            get { return (int)Math.Round(State.LeftStick.Magnitude * 100); }
        }

        public int RightStickDriftPercent
        {
            get { return (int)Math.Round(State.RightStick.Magnitude * 100); }
        }

        public bool IsDpadActive
        {
            get
            {
                return State.Buttons.DpadUp || State.Buttons.DpadDown || State.Buttons.DpadLeft || State.Buttons.DpadRight;
            }
        }

        public int ActiveButtonCount
        {
            get { return CountPressedButtons(State.Buttons); }
        }

        public string LeftStickVector
        {
            get { return string.Format("X {0:0.000}  Y {1:0.000}", State.LeftStick.X, State.LeftStick.Y); }
        }

        public string RightStickVector
        {
            get { return string.Format("X {0:0.000}  Y {1:0.000}", State.RightStick.X, State.RightStick.Y); }
        }

        public string LeftStickDriftStatus
        {
            get { return GetDriftStatus(State.LeftStick.Magnitude); }
        }

        public string RightStickDriftStatus
        {
            get { return GetDriftStatus(State.RightStick.Magnitude); }
        }

        public string MaxDriftLabel
        {
            get { return string.Format("{0:0.000}", Math.Max(maxLeftRestDrift, maxRightRestDrift)); }
        }

        public PointCollection LeftStickPathPoints
        {
            get { return leftStickDiagnostics.PathPoints; }
        }

        public PointCollection RightStickPathPoints
        {
            get { return rightStickDiagnostics.PathPoints; }
        }

        public Geometry LeftStickPathGeometry
        {
            get { return leftStickDiagnostics.PathGeometry; }
        }

        public Geometry RightStickPathGeometry
        {
            get { return rightStickDiagnostics.PathGeometry; }
        }

        public Geometry LeftStickCircularCoverageGeometry
        {
            get { return leftStickDiagnostics.CoverageGeometry; }
        }

        public Geometry RightStickCircularCoverageGeometry
        {
            get { return rightStickDiagnostics.CoverageGeometry; }
        }

        public int LeftStickCircularCoveragePercent
        {
            get { return leftStickDiagnostics.CoveragePercent; }
        }

        public int RightStickCircularCoveragePercent
        {
            get { return rightStickDiagnostics.CoveragePercent; }
        }

        public int LeftStickMaxReachPercent
        {
            get { return Math.Min(100, (int)Math.Round(leftStickDiagnostics.MaxMagnitude * 100d)); }
        }

        public int RightStickMaxReachPercent
        {
            get { return Math.Min(100, (int)Math.Round(rightStickDiagnostics.MaxMagnitude * 100d)); }
        }

        public int LeftStickCurrentMagnitudePercent
        {
            get { return Math.Min(100, (int)Math.Round(State.LeftStick.Magnitude * 100d)); }
        }

        public int RightStickCurrentMagnitudePercent
        {
            get { return Math.Min(100, (int)Math.Round(State.RightStick.Magnitude * 100d)); }
        }

        public string LeftStickCircularCoverageLabel
        {
            get { return GetCircularCoverageLabel(leftStickDiagnostics); }
        }

        public string RightStickCircularCoverageLabel
        {
            get { return GetCircularCoverageLabel(rightStickDiagnostics); }
        }

        public string LeftStickPathSampleLabel
        {
            get { return GetPathSampleLabel(leftStickDiagnostics); }
        }

        public string RightStickPathSampleLabel
        {
            get { return GetPathSampleLabel(rightStickDiagnostics); }
        }

        public string LeftStickMaxReachLabel
        {
            get { return string.Format("Max reach: {0}%", Math.Min(100, (int)Math.Round(leftStickDiagnostics.MaxMagnitude * 100d))); }
        }

        public string RightStickMaxReachLabel
        {
            get { return string.Format("Max reach: {0}%", Math.Min(100, (int)Math.Round(rightStickDiagnostics.MaxMagnitude * 100d))); }
        }

        public string LeftStickCurrentMagnitudeLabel
        {
            get { return string.Format("Current: {0}%", LeftStickCurrentMagnitudePercent); }
        }

        public string RightStickCurrentMagnitudeLabel
        {
            get { return string.Format("Current: {0}%", RightStickCurrentMagnitudePercent); }
        }

        public string LeftStickAngleLabel
        {
            get { return GetAngleLabel(State.LeftStick); }
        }

        public string RightStickAngleLabel
        {
            get { return GetAngleLabel(State.RightStick); }
        }

        public string LeftStickAxisRangeLabel
        {
            get { return GetAxisRangeLabel(leftStickDiagnostics); }
        }

        public string RightStickAxisRangeLabel
        {
            get { return GetAxisRangeLabel(rightStickDiagnostics); }
        }

        public string LeftStickAverageMagnitudeLabel
        {
            get { return GetAverageMagnitudeLabel(leftStickDiagnostics); }
        }

        public string RightStickAverageMagnitudeLabel
        {
            get { return GetAverageMagnitudeLabel(rightStickDiagnostics); }
        }

        public int QuickTestProgress
        {
            get
            {
                const int totalChecks = 19;
                var completed = CountPressedButtons(coveredButtons);

                if (maxLeftTrigger >= 0.95f)
                {
                    completed++;
                }

                if (maxRightTrigger >= 0.95f)
                {
                    completed++;
                }

                if (maxLeftStickMagnitude >= 0.85d)
                {
                    completed++;
                }

                if (maxRightStickMagnitude >= 0.85d)
                {
                    completed++;
                }

                return Math.Max(0, Math.Min(100, (int)Math.Round(completed * 100d / totalChecks)));
            }
        }

        public string QuickTestLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return "Connect a controller to start.";
                }

                if (QuickTestProgress == 100)
                {
                    return "All normalized controls covered.";
                }

                return string.Format("{0}% complete", QuickTestProgress);
            }
        }

        public string ButtonCoverageLabel
        {
            get { return string.Format("{0}/15 buttons seen", CountPressedButtons(coveredButtons)); }
        }

        public string AnalogCoverageLabel
        {
            get
            {
                return string.Format("LT {0}%  RT {1}%  LS {2}%  RS {3}%",
                    (int)Math.Round(maxLeftTrigger * 100),
                    (int)Math.Round(maxRightTrigger * 100),
                    (int)Math.Round(maxLeftStickMagnitude * 100),
                    (int)Math.Round(maxRightStickMagnitude * 100));
            }
        }

        public string QuickTestMissingLabel
        {
            get
            {
                var missing = new List<string>();
                AddMissingButton(missing, coveredButtons.South, SouthLabel);
                AddMissingButton(missing, coveredButtons.East, EastLabel);
                AddMissingButton(missing, coveredButtons.West, WestLabel);
                AddMissingButton(missing, coveredButtons.North, NorthLabel);
                AddMissingButton(missing, coveredButtons.LeftShoulder, "LB / L1");
                AddMissingButton(missing, coveredButtons.RightShoulder, "RB / R1");
                AddMissingButton(missing, coveredButtons.LeftStick, "L3");
                AddMissingButton(missing, coveredButtons.RightStick, "R3");
                AddMissingButton(missing, coveredButtons.Back, "Back / Share");
                AddMissingButton(missing, coveredButtons.Start, "Start / Options");
                AddMissingButton(missing, coveredButtons.Guide, "Guide");
                AddMissingButton(missing, coveredButtons.DpadUp, "D-Up");
                AddMissingButton(missing, coveredButtons.DpadDown, "D-Down");
                AddMissingButton(missing, coveredButtons.DpadLeft, "D-Left");
                AddMissingButton(missing, coveredButtons.DpadRight, "D-Right");

                if (maxLeftTrigger < 0.95f)
                {
                    missing.Add("LT 100%");
                }

                if (maxRightTrigger < 0.95f)
                {
                    missing.Add("RT 100%");
                }

                if (maxLeftStickMagnitude < 0.85d)
                {
                    missing.Add("Left stick edge");
                }

                if (maxRightStickMagnitude < 0.85d)
                {
                    missing.Add("Right stick edge");
                }

                return missing.Count == 0 ? "Nothing missing." : string.Join(", ", missing);
            }
        }

        public int HealthScore
        {
            get
            {
                var driftPenalty = Math.Min(35d, Math.Max(maxLeftRestDrift, maxRightRestDrift) * 180d);
                return Math.Max(0, Math.Min(100, (int)Math.Round(100d - driftPenalty)));
            }
        }

        public string HealthLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return "No controller";
                }

                if (HealthScore >= 90)
                {
                    return "Excellent";
                }

                if (HealthScore >= 75)
                {
                    return "Good";
                }

                if (HealthScore >= 55)
                {
                    return "Needs review";
                }

                return "Attention required";
            }
        }

        public string ControllerSummary
        {
            get
            {
                if (!State.IsConnected)
                {
                    return "Connect a controller and press any button.";
                }

                return string.Format("{0} inputs active | LT {1}% | RT {2}% | Rest drift peak {3}",
                    ActiveButtonCount,
                    LeftTriggerPercent,
                    RightTriggerPercent,
                    MaxDriftLabel);
            }
        }

        public string DeviceIdLabel
        {
            get
            {
                if (!State.IsConnected || State.VendorId == 0)
                {
                    return string.Empty;
                }

                return string.Format("VID: {0:X4}  PID: {1:X4}", State.VendorId, State.ProductId);
            }
        }

        public string DeviceModelLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return string.Empty;
                }

                return GamepadDeviceNames.GetDisplayName(State.ControllerName, State.VendorId, State.ProductId, State.Layout, State.EightBitDoModel);
            }
        }

        public string SouthLabel
        {
            get { return State.Layout == GamepadLayout.PlayStation ? "Cross" : "A"; }
        }

        public string EastLabel
        {
            get { return State.Layout == GamepadLayout.PlayStation ? "Circle" : "B"; }
        }

        public string WestLabel
        {
            get { return State.Layout == GamepadLayout.PlayStation ? "Square" : "X"; }
        }

        public string NorthLabel
        {
            get { return State.Layout == GamepadLayout.PlayStation ? "Triangle" : "Y"; }
        }

        public string LeftShoulderLabel
        {
            get
            {
                if (State.Layout == GamepadLayout.PlayStation)
                {
                    return "L1";
                }

                return State.Layout == GamepadLayout.SwitchPro ? "L" : "LB";
            }
        }

        public string RightShoulderLabel
        {
            get
            {
                if (State.Layout == GamepadLayout.PlayStation)
                {
                    return "R1";
                }

                return State.Layout == GamepadLayout.SwitchPro ? "R" : "RB";
            }
        }

        public string LeftTriggerLabel
        {
            get
            {
                if (State.Layout == GamepadLayout.PlayStation)
                {
                    return "L2";
                }

                return State.Layout == GamepadLayout.SwitchPro ? "ZL" : "LT";
            }
        }

        public string RightTriggerLabel
        {
            get
            {
                if (State.Layout == GamepadLayout.PlayStation)
                {
                    return "R2";
                }

                return State.Layout == GamepadLayout.SwitchPro ? "ZR" : "RT";
            }
        }

        public bool IsEightBitDoLayout
        {
            get { return State.Layout == GamepadLayout.EightBitDo; }
        }

        public bool IsEightBitDo64Artwork
        {
            get { return State.Layout == GamepadLayout.EightBitDo && State.EightBitDoModel == EightBitDoModel.Controller64; }
        }

        public bool IsEightBitDoPro3Artwork
        {
            get { return State.Layout == GamepadLayout.EightBitDo && State.EightBitDoModel == EightBitDoModel.Pro3; }
        }

        public bool IsEightBitDoUltimate2CArtwork
        {
            get { return State.Layout == GamepadLayout.EightBitDo && State.EightBitDoModel == EightBitDoModel.Ultimate2CWireless; }
        }

        public bool IsEightBitDoUltimate2Artwork
        {
            get
            {
                return State.Layout == GamepadLayout.EightBitDo &&
                    (State.EightBitDoModel == EightBitDoModel.Ultimate2Wireless ||
                     State.EightBitDoModel == EightBitDoModel.Controller64 ||
                     State.EightBitDoModel == EightBitDoModel.Unknown);
            }
        }

        public bool IsSwitchProLayout
        {
            get { return State.Layout == GamepadLayout.SwitchPro; }
        }

        public bool IsXboxLayout
        {
            get { return State.Layout == GamepadLayout.Xbox; }
        }

        public bool IsPlayStationLayout
        {
            get { return State.Layout == GamepadLayout.PlayStation; }
        }

        public bool IsGenericLayout
        {
            get { return State.Layout == GamepadLayout.Generic || State.Layout == GamepadLayout.Unknown; }
        }

        public void Start()
        {
            pollingService.Start();
        }

        private void OnStateUpdated(object sender, GamepadState nextState)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                controllerRefreshTick++;
                if (!IsControllerSelectorOpen && (controllerRefreshTick == 1 || controllerRefreshTick >= 60))
                {
                    controllerRefreshTick = 0;
                    RefreshControllers();
                }

                UpdateDiagnostics(nextState);
                State = nextState;
                RaiseRumbleCanExecuteChanged();
            }));
        }

        private void UpdateDiagnostics(GamepadState nextState)
        {
            if (!nextState.IsConnected)
            {
                previousButtons = null;
                return;
            }

            if (IsResting(nextState))
            {
                maxLeftRestDrift = Math.Max(maxLeftRestDrift, nextState.LeftStick.Magnitude);
                maxRightRestDrift = Math.Max(maxRightRestDrift, nextState.RightStick.Magnitude);
            }

            leftStickDiagnostics.AddSample(nextState.LeftStick);
            rightStickDiagnostics.AddSample(nextState.RightStick);
            UpdateCoverage(nextState);

            if (previousButtons == null)
            {
                previousButtons = CopyButtons(nextState.Buttons);
                return;
            }

            TrackButtonChange("A / Cross", previousButtons.South, nextState.Buttons.South);
            TrackButtonChange("B / Circle", previousButtons.East, nextState.Buttons.East);
            TrackButtonChange("X / Square", previousButtons.West, nextState.Buttons.West);
            TrackButtonChange("Y / Triangle", previousButtons.North, nextState.Buttons.North);
            TrackButtonChange("LB / L1", previousButtons.LeftShoulder, nextState.Buttons.LeftShoulder);
            TrackButtonChange("RB / R1", previousButtons.RightShoulder, nextState.Buttons.RightShoulder);
            TrackButtonChange("Back / Share", previousButtons.Back, nextState.Buttons.Back);
            TrackButtonChange("Start / Options", previousButtons.Start, nextState.Buttons.Start);
            TrackButtonChange("Guide", previousButtons.Guide, nextState.Buttons.Guide);
            TrackButtonChange("L3", previousButtons.LeftStick, nextState.Buttons.LeftStick);
            TrackButtonChange("R3", previousButtons.RightStick, nextState.Buttons.RightStick);
            TrackButtonChange("D-Pad Up", previousButtons.DpadUp, nextState.Buttons.DpadUp);
            TrackButtonChange("D-Pad Down", previousButtons.DpadDown, nextState.Buttons.DpadDown);
            TrackButtonChange("D-Pad Left", previousButtons.DpadLeft, nextState.Buttons.DpadLeft);
            TrackButtonChange("D-Pad Right", previousButtons.DpadRight, nextState.Buttons.DpadRight);

            previousButtons = CopyButtons(nextState.Buttons);
        }

        private void TrackButtonChange(string inputName, bool previous, bool current)
        {
            if (previous == current)
            {
                return;
            }

            InputHistory.Insert(0, new InputHistoryItem
            {
                Timestamp = DateTime.Now,
                InputName = inputName,
                State = current ? "Pressed" : "Released"
            });

            while (InputHistory.Count > 80)
            {
                InputHistory.RemoveAt(InputHistory.Count - 1);
            }
        }

        private void RefreshControllers()
        {
            var controllers = pollingService.GetControllers();
            var selectedInstanceId = SelectedController != null ? (int?)SelectedController.InstanceId : null;

            Controllers.Clear();
            foreach (var controller in controllers)
            {
                Controllers.Add(controller);
            }

            if (Controllers.Count == 0)
            {
                selectedController = null;
                OnPropertyChanged("SelectedController");
                OnPropertyChanged("IsControllerSelectorVisible");
                return;
            }

            GamepadControllerInfo nextSelection = null;
            if (selectedInstanceId.HasValue)
            {
                foreach (var controller in Controllers)
                {
                    if (controller.InstanceId == selectedInstanceId.Value)
                    {
                        nextSelection = controller;
                        break;
                    }
                }
            }

            if (nextSelection == null)
            {
                nextSelection = Controllers[0];
            }

            selectedController = nextSelection;
            OnPropertyChanged("SelectedController");
            OnPropertyChanged("IsControllerSelectorVisible");
        }

        private bool CanRunRumble()
        {
            return State.IsConnected && !isRumbleRunning;
        }

        private void RunSimpleRumble(string label, ushort lowFrequency, ushort highFrequency, uint durationMs)
        {
            RunRumblePattern(label, () =>
            {
                pollingService.TryRumble(lowFrequency, highFrequency, durationMs);
                Thread.Sleep((int)durationMs + 80);
            });
        }

        private void TestPulseRumble()
        {
            RunRumblePattern("Pulse pattern", () =>
            {
                for (var index = 0; index < 3; index++)
                {
                    pollingService.TryRumble(18000, 56000, 130);
                    Thread.Sleep(210);
                }
            });
        }

        private void TestAlternatingRumble()
        {
            RunRumblePattern("Alternating motors", () =>
            {
                for (var index = 0; index < 4; index++)
                {
                    pollingService.TryRumble(56000, 0, 150);
                    Thread.Sleep(210);
                    pollingService.TryRumble(0, 56000, 150);
                    Thread.Sleep(210);
                }
            });
        }

        private void TestRampRumble()
        {
            RunRumblePattern("Ramp pattern", () =>
            {
                for (var step = 1; step <= 5; step++)
                {
                    var strength = (ushort)(step * 12000);
                    pollingService.TryRumble(strength, strength, 150);
                    Thread.Sleep(210);
                }
            });
        }

        private void RunRumblePattern(string label, Action pattern)
        {
            SetRumbleState(true, label + " running...");
            Task.Run(() =>
            {
                try
                {
                    pattern();
                    pollingService.TryRumble(0, 0, 1);
                    SetRumbleState(false, label + " complete");
                }
                catch
                {
                    SetRumbleState(false, label + " failed");
                }
            });
        }

        private void SetRumbleState(bool isRunning, string statusLabel)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                isRumbleRunning = isRunning;
                rumbleStatusLabel = statusLabel;
                OnPropertyChanged("RumbleStatusLabel");
                RaiseRumbleCanExecuteChanged();
            }));
        }

        private void RaiseRumbleCanExecuteChanged()
        {
            rumbleCommand.RaiseCanExecuteChanged();
            lightRumbleCommand.RaiseCanExecuteChanged();
            mediumRumbleCommand.RaiseCanExecuteChanged();
            heavyRumbleCommand.RaiseCanExecuteChanged();
            lowMotorRumbleCommand.RaiseCanExecuteChanged();
            highMotorRumbleCommand.RaiseCanExecuteChanged();
            pulseRumbleCommand.RaiseCanExecuteChanged();
            alternatingRumbleCommand.RaiseCanExecuteChanged();
            rampRumbleCommand.RaiseCanExecuteChanged();
        }

        private void ResetDiagnostics()
        {
            maxLeftRestDrift = 0d;
            maxRightRestDrift = 0d;
            maxLeftStickMagnitude = 0d;
            maxRightStickMagnitude = 0d;
            maxLeftTrigger = 0f;
            maxRightTrigger = 0f;
            coveredButtons = new GamepadButtonState();
            leftStickDiagnostics.Reset();
            rightStickDiagnostics.Reset();
            InputHistory.Clear();
            NotifyStateChanged();
        }

        private void UpdateCoverage(GamepadState nextState)
        {
            maxLeftTrigger = Math.Max(maxLeftTrigger, nextState.LeftTrigger);
            maxRightTrigger = Math.Max(maxRightTrigger, nextState.RightTrigger);
            maxLeftStickMagnitude = Math.Max(maxLeftStickMagnitude, nextState.LeftStick.Magnitude);
            maxRightStickMagnitude = Math.Max(maxRightStickMagnitude, nextState.RightStick.Magnitude);

            coveredButtons.South = coveredButtons.South || nextState.Buttons.South;
            coveredButtons.East = coveredButtons.East || nextState.Buttons.East;
            coveredButtons.West = coveredButtons.West || nextState.Buttons.West;
            coveredButtons.North = coveredButtons.North || nextState.Buttons.North;
            coveredButtons.LeftShoulder = coveredButtons.LeftShoulder || nextState.Buttons.LeftShoulder;
            coveredButtons.RightShoulder = coveredButtons.RightShoulder || nextState.Buttons.RightShoulder;
            coveredButtons.Back = coveredButtons.Back || nextState.Buttons.Back;
            coveredButtons.Start = coveredButtons.Start || nextState.Buttons.Start;
            coveredButtons.Guide = coveredButtons.Guide || nextState.Buttons.Guide;
            coveredButtons.LeftStick = coveredButtons.LeftStick || nextState.Buttons.LeftStick;
            coveredButtons.RightStick = coveredButtons.RightStick || nextState.Buttons.RightStick;
            coveredButtons.DpadUp = coveredButtons.DpadUp || nextState.Buttons.DpadUp;
            coveredButtons.DpadDown = coveredButtons.DpadDown || nextState.Buttons.DpadDown;
            coveredButtons.DpadLeft = coveredButtons.DpadLeft || nextState.Buttons.DpadLeft;
            coveredButtons.DpadRight = coveredButtons.DpadRight || nextState.Buttons.DpadRight;
        }

        private static void AddMissingButton(ICollection<string> missing, bool isCovered, string label)
        {
            if (!isCovered)
            {
                missing.Add(label);
            }
        }

        private static GamepadButtonState CopyButtons(GamepadButtonState buttons)
        {
            return new GamepadButtonState
            {
                South = buttons.South,
                East = buttons.East,
                West = buttons.West,
                North = buttons.North,
                LeftShoulder = buttons.LeftShoulder,
                RightShoulder = buttons.RightShoulder,
                Back = buttons.Back,
                Start = buttons.Start,
                Guide = buttons.Guide,
                LeftStick = buttons.LeftStick,
                RightStick = buttons.RightStick,
                DpadUp = buttons.DpadUp,
                DpadDown = buttons.DpadDown,
                DpadLeft = buttons.DpadLeft,
                DpadRight = buttons.DpadRight
            };
        }

        private static bool IsResting(GamepadState state)
        {
            return CountPressedButtons(state.Buttons) == 0 &&
                   state.LeftTrigger < 0.02f &&
                   state.RightTrigger < 0.02f &&
                   state.LeftStick.Magnitude < 0.25d &&
                   state.RightStick.Magnitude < 0.25d;
        }

        private static int CountPressedButtons(GamepadButtonState buttons)
        {
            var count = 0;
            foreach (var pressed in EnumerateButtonValues(buttons))
            {
                if (pressed)
                {
                    count++;
                }
            }

            return count;
        }

        private static IEnumerable<bool> EnumerateButtonValues(GamepadButtonState buttons)
        {
            yield return buttons.South;
            yield return buttons.East;
            yield return buttons.West;
            yield return buttons.North;
            yield return buttons.LeftShoulder;
            yield return buttons.RightShoulder;
            yield return buttons.Back;
            yield return buttons.Start;
            yield return buttons.Guide;
            yield return buttons.LeftStick;
            yield return buttons.RightStick;
            yield return buttons.DpadUp;
            yield return buttons.DpadDown;
            yield return buttons.DpadLeft;
            yield return buttons.DpadRight;
        }

        private static string GetDriftStatus(double magnitude)
        {
            if (magnitude < 0.01d)
            {
                return "No drift";
            }

            if (magnitude < 0.05d)
            {
                return "Safe";
            }

            if (magnitude < 0.15d)
            {
                return "Minor drift";
            }

            return "Major drift";
        }

        private static string GetCircularCoverageLabel(StickDiagnosticsTracker tracker)
        {
            return string.Format("Circular coverage: {0}% ({1}/72 sectors)", tracker.CoveragePercent, tracker.CoveredSectors);
        }

        private static string GetPathSampleLabel(StickDiagnosticsTracker tracker)
        {
            return string.Format("Path samples: {0}", tracker.PathPoints.Count);
        }

        private static string GetAxisRangeLabel(StickDiagnosticsTracker tracker)
        {
            if (tracker.SampleCount == 0)
            {
                return "Range: no samples";
            }

            return string.Format("Range X {0:0.00}..{1:0.00}  Y {2:0.00}..{3:0.00}",
                tracker.MinX,
                tracker.MaxX,
                tracker.MinY,
                tracker.MaxY);
        }

        private static string GetAverageMagnitudeLabel(StickDiagnosticsTracker tracker)
        {
            if (tracker.SampleCount == 0)
            {
                return "Average: 0%";
            }

            return string.Format("Average: {0}%", Math.Min(100, (int)Math.Round(tracker.AverageMagnitude * 100d)));
        }

        private static string GetAngleLabel(StickState stick)
        {
            if (stick.Magnitude < 0.05d)
            {
                return "Angle: center";
            }

            var angle = Math.Atan2(stick.Y, stick.X) * 180d / Math.PI;
            if (angle < 0d)
            {
                angle += 360d;
            }

            return string.Format("Angle: {0:0} deg", angle);
        }

        private void NotifyStateChanged()
        {
            OnPropertyChanged("State");
            OnPropertyChanged("LeftStickDotX");
            OnPropertyChanged("LeftStickDotY");
            OnPropertyChanged("RightStickDotX");
            OnPropertyChanged("RightStickDotY");
            OnPropertyChanged("LeftStickDiagnosticsDotX");
            OnPropertyChanged("LeftStickDiagnosticsDotY");
            OnPropertyChanged("RightStickDiagnosticsDotX");
            OnPropertyChanged("RightStickDiagnosticsDotY");
            OnPropertyChanged("LeftTriggerPercent");
            OnPropertyChanged("RightTriggerPercent");
            OnPropertyChanged("IsLeftTriggerActive");
            OnPropertyChanged("IsRightTriggerActive");
            OnPropertyChanged("LeftTriggerMapWidth");
            OnPropertyChanged("RightTriggerMapWidth");
            OnPropertyChanged("LeftTriggerThinMapWidth");
            OnPropertyChanged("RightTriggerThinMapWidth");
            OnPropertyChanged("LeftStickDriftPercent");
            OnPropertyChanged("RightStickDriftPercent");
            OnPropertyChanged("IsDpadActive");
            OnPropertyChanged("ActiveButtonCount");
            OnPropertyChanged("LeftStickVector");
            OnPropertyChanged("RightStickVector");
            OnPropertyChanged("LeftStickDriftStatus");
            OnPropertyChanged("RightStickDriftStatus");
            OnPropertyChanged("MaxDriftLabel");
            OnPropertyChanged("LeftStickPathPoints");
            OnPropertyChanged("RightStickPathPoints");
            OnPropertyChanged("LeftStickPathGeometry");
            OnPropertyChanged("RightStickPathGeometry");
            OnPropertyChanged("LeftStickCircularCoverageGeometry");
            OnPropertyChanged("RightStickCircularCoverageGeometry");
            OnPropertyChanged("LeftStickCircularCoveragePercent");
            OnPropertyChanged("RightStickCircularCoveragePercent");
            OnPropertyChanged("LeftStickMaxReachPercent");
            OnPropertyChanged("RightStickMaxReachPercent");
            OnPropertyChanged("LeftStickCurrentMagnitudePercent");
            OnPropertyChanged("RightStickCurrentMagnitudePercent");
            OnPropertyChanged("LeftStickCircularCoverageLabel");
            OnPropertyChanged("RightStickCircularCoverageLabel");
            OnPropertyChanged("LeftStickPathSampleLabel");
            OnPropertyChanged("RightStickPathSampleLabel");
            OnPropertyChanged("LeftStickMaxReachLabel");
            OnPropertyChanged("RightStickMaxReachLabel");
            OnPropertyChanged("LeftStickCurrentMagnitudeLabel");
            OnPropertyChanged("RightStickCurrentMagnitudeLabel");
            OnPropertyChanged("LeftStickAngleLabel");
            OnPropertyChanged("RightStickAngleLabel");
            OnPropertyChanged("LeftStickAxisRangeLabel");
            OnPropertyChanged("RightStickAxisRangeLabel");
            OnPropertyChanged("LeftStickAverageMagnitudeLabel");
            OnPropertyChanged("RightStickAverageMagnitudeLabel");
            OnPropertyChanged("QuickTestProgress");
            OnPropertyChanged("QuickTestLabel");
            OnPropertyChanged("ButtonCoverageLabel");
            OnPropertyChanged("AnalogCoverageLabel");
            OnPropertyChanged("QuickTestMissingLabel");
            OnPropertyChanged("HealthScore");
            OnPropertyChanged("HealthLabel");
            OnPropertyChanged("ControllerSummary");
            OnPropertyChanged("DeviceIdLabel");
            OnPropertyChanged("DeviceModelLabel");
            OnPropertyChanged("RumbleStatusLabel");
            OnPropertyChanged("SouthLabel");
            OnPropertyChanged("EastLabel");
            OnPropertyChanged("WestLabel");
            OnPropertyChanged("NorthLabel");
            OnPropertyChanged("LeftShoulderLabel");
            OnPropertyChanged("RightShoulderLabel");
            OnPropertyChanged("LeftTriggerLabel");
            OnPropertyChanged("RightTriggerLabel");
            OnPropertyChanged("IsEightBitDoLayout");
            OnPropertyChanged("IsEightBitDo64Artwork");
            OnPropertyChanged("IsEightBitDoPro3Artwork");
            OnPropertyChanged("IsEightBitDoUltimate2CArtwork");
            OnPropertyChanged("IsEightBitDoUltimate2Artwork");
            OnPropertyChanged("IsSwitchProLayout");
            OnPropertyChanged("IsXboxLayout");
            OnPropertyChanged("IsPlayStationLayout");
            OnPropertyChanged("IsGenericLayout");
        }

        public void Dispose()
        {
            pollingService.StateUpdated -= OnStateUpdated;
            pollingService.Dispose();
        }
    }
}
