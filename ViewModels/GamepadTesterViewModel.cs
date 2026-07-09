using GamepadTester.Commands;
using GamepadTester.Models;
using GamepadTester.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace GamepadTester.ViewModels
{
    public sealed class GamepadTesterViewModel : ObservableObject, IDisposable
    {
        private const double StickRadius = 34d;
        private const double RestStabilityDelta = 0.015d;
        private const double RestObservationMilliseconds = 450d;
        private const double MaximumRestDriftCandidate = 0.35d;
        private const int LatencyGraphMaxSamples = 48;
        private readonly GamepadPollingService pollingService;
        private readonly GamepadTesterSettings settings;
        private readonly Func<string, string> localizer;
        private readonly Action<GamepadTesterViewModel> openGuidedTest;
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
        private readonly RelayCommand startCenterCalibrationCommand;
        private readonly RelayCommand resetCalibrationCommand;
        private readonly RelayCommand resetStickRangeCommand;
        private readonly RelayCommand resetLatencyCommand;
        private readonly RelayCommand startLatencyTestCommand;
        private readonly RelayCommand openGuidedTestCommand;
        private readonly RelayCommand startGuidedTestCommand;
        private readonly RelayCommand exportReportCommand;
        private readonly RelayCommand exportInputLogCommand;
        private readonly RelayCommand exportLatencyCommand;
        private readonly RelayCommand exportSticksCommand;
        private readonly RelayCommand resetInputLogCommand;
        private readonly StickDiagnosticsTracker leftStickDiagnostics;
        private readonly StickDiagnosticsTracker rightStickDiagnostics;
        private GamepadState state;
        private GamepadControllerInfo selectedController;
        private int controllerRefreshTick;
        private GamepadButtonState previousButtons;
        private List<ExtraButtonState> previousExtraButtons;
        private GamepadButtonState coveredButtons;
        private double maxLeftRestDrift;
        private double maxRightRestDrift;
        private DateTime? restCandidateStartedAt;
        private double lastRestCandidateLeftX;
        private double lastRestCandidateLeftY;
        private double lastRestCandidateRightX;
        private double lastRestCandidateRightY;
        private double maxLeftStickMagnitude;
        private double maxRightStickMagnitude;
        private float maxLeftTrigger;
        private float maxRightTrigger;
        private bool isControllerSelectorOpen;
        private bool isInputLogEnabled;
        private bool isGuidedTestRunning;
        private int guidedTestStepIndex;
        private bool isRumbleRunning;
        private string rumbleStatusLabel;
        private bool isCenterCalibrationRunning;
        private DateTime centerCalibrationEndsAt;
        private int centerCalibrationSamples;
        private double leftCenterXSum;
        private double leftCenterYSum;
        private double rightCenterXSum;
        private double rightCenterYSum;
        private double leftCenterMaxNoise;
        private double rightCenterMaxNoise;
        private double calibratedLeftCenterX;
        private double calibratedLeftCenterY;
        private double calibratedRightCenterX;
        private double calibratedRightCenterY;
        private double calibratedLeftCenterNoise;
        private double calibratedRightCenterNoise;
        private DateTime? lastStateSampleAt;
        private DateTime? lastInputEventAt;
        private double currentPollingIntervalMs;
        private double pollingIntervalSumMs;
        private double pollingIntervalMinMs;
        private double pollingIntervalMaxMs;
        private int pollingIntervalSamples;
        private double inputEventIntervalSumMs;
        private double inputEventIntervalMinMs;
        private double inputEventIntervalMaxMs;
        private int inputEventIntervalSamples;
        private double currentInputEventIntervalMs;
        private readonly Queue<double> latencyRateHistory = new Queue<double>();
        private string latencyStatusLabel;
        private bool hasLatencyTestStarted;
        private bool isLatencyTestRunning;
        private DateTime latencyTestStartedAt;
        private double lastLatencyMs;
        private double bestLatencyMs;
        private double latencyTestSumMs;
        private int latencyTestSamples;
        private string exportReportStatusLabel;
        private string inputLogExportStatusLabel;
        private string selectedVisualSchemeKey;
        private bool isVisualSchemeManuallySelected;
        private int selectedTabIndex;
        private bool isFullscreenSimplifiedMode;

        public GamepadTesterViewModel(GamepadPollingService pollingService, GamepadTesterSettings settings = null, Func<string, string> localizer = null, Action<GamepadTesterViewModel> openGuidedTest = null)
        {
            this.pollingService = pollingService;
            this.settings = settings ?? new GamepadTesterSettings();
            this.localizer = localizer;
            this.openGuidedTest = openGuidedTest;
            state = new GamepadState();
            Controllers = new ObservableCollection<GamepadControllerInfo>();
            InputHistory = new ObservableCollection<InputHistoryItem>();
            GuidedTestInputs = new ObservableCollection<GuidedTestInputItem>();
            VisualSchemeOptions = new ObservableCollection<ControllerVisualSchemeOption>();
            InitializeVisualSchemeOptions();
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
            startCenterCalibrationCommand = new RelayCommand(StartCenterCalibration, () => State.IsConnected && !isCenterCalibrationRunning);
            resetCalibrationCommand = new RelayCommand(ResetCalibration);
            resetStickRangeCommand = new RelayCommand(ResetStickRangeDiagnostics);
            resetLatencyCommand = new RelayCommand(ResetLatency, () => State.IsConnected && !isLatencyTestRunning);
            startLatencyTestCommand = new RelayCommand(ToggleLatencyTest, () => State.IsConnected);
            openGuidedTestCommand = new RelayCommand(OpenGuidedTest, () => State.IsConnected && this.openGuidedTest != null);
            startGuidedTestCommand = new RelayCommand(StartGuidedTest, () => State.IsConnected);
            exportReportCommand = new RelayCommand(ExportReport);
            exportInputLogCommand = new RelayCommand(ExportInputLog, () => InputHistory.Count > 0);
            exportLatencyCommand = new RelayCommand(ExportLatencyData, () => !isLatencyTestRunning && inputEventIntervalSamples > 0);
            exportSticksCommand = new RelayCommand(ExportStickData, () => State.IsConnected);
            resetInputLogCommand = new RelayCommand(ClearInputHistory, () => InputHistory.Count > 0);
            leftStickDiagnostics = new StickDiagnosticsTracker();
            rightStickDiagnostics = new StickDiagnosticsTracker();
            coveredButtons = new GamepadButtonState();
            InitializeGuidedTestInputs();
            rumbleStatusLabel = L("LOCGT_Ready", "Ready");
            latencyStatusLabel = L("LOCGT_LatencyWaiting", "Waiting for input changes.");
            exportReportStatusLabel = L("LOCGT_ReportReady", "Report ready to export.");
            inputLogExportStatusLabel = L("LOCGT_InputLogExportReady", "Enable input log and press buttons to collect entries.");
            isInputLogEnabled = this.settings.EnableInputLogByDefault;
            pollingIntervalMinMs = double.MaxValue;
            inputEventIntervalMinMs = double.MaxValue;
            pollingService.StateUpdated += OnStateUpdated;
        }

        public ObservableCollection<GamepadControllerInfo> Controllers { get; private set; }
        public ObservableCollection<InputHistoryItem> InputHistory { get; private set; }
        public ObservableCollection<GuidedTestInputItem> GuidedTestInputs { get; private set; }
        public ObservableCollection<ControllerVisualSchemeOption> VisualSchemeOptions { get; private set; }

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

        public ICommand StartCenterCalibrationCommand
        {
            get { return startCenterCalibrationCommand; }
        }

        public ICommand ResetCalibrationCommand
        {
            get { return resetCalibrationCommand; }
        }

        public ICommand ResetStickRangeCommand
        {
            get { return resetStickRangeCommand; }
        }

        public ICommand ResetLatencyCommand
        {
            get { return resetLatencyCommand; }
        }

        public ICommand StartLatencyTestCommand
        {
            get { return startLatencyTestCommand; }
        }

        public ICommand OpenGuidedTestCommand
        {
            get { return openGuidedTestCommand; }
        }

        public ICommand StartGuidedTestCommand
        {
            get { return startGuidedTestCommand; }
        }

        public ICommand ExportReportCommand
        {
            get { return exportReportCommand; }
        }

        public ICommand ExportInputLogCommand
        {
            get { return exportInputLogCommand; }
        }

        public ICommand ExportLatencyCommand
        {
            get { return exportLatencyCommand; }
        }

        public ICommand ExportSticksCommand
        {
            get { return exportSticksCommand; }
        }

        public ICommand ResetInputLogCommand
        {
            get { return resetInputLogCommand; }
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
                    isVisualSchemeManuallySelected = false;
                    pollingService.SelectController(selectedController.InstanceId);
                    if (this.settings.AutoResetDiagnosticsOnControllerChange)
                    {
                        ResetDiagnostics();
                    }
                }
            }
        }

        public bool IsControllerSelectorVisible
        {
            get { return !isFullscreenSimplifiedMode && (Controllers.Count > 1 || settings.ShowDeviceSelectorWhenSingleController); }
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

        public int SelectedTabIndex
        {
            get { return selectedTabIndex; }
            set
            {
                var next = Math.Max(0, Math.Min(4, value));
                if (selectedTabIndex == next)
                {
                    return;
                }

                selectedTabIndex = next;
                OnPropertyChanged("SelectedTabIndex");
            }
        }

        public bool IsFullscreenSimplifiedMode
        {
            get { return isFullscreenSimplifiedMode; }
            set
            {
                if (isFullscreenSimplifiedMode == value)
                {
                    return;
                }

                isFullscreenSimplifiedMode = value;
                OnPropertyChanged("IsFullscreenSimplifiedMode");
                OnPropertyChanged("IsControllerSelectorVisible");
                OnPropertyChanged("IsVisualSchemeSelectorVisible");
                OnPropertyChanged("IsFullTesterMode");
                if (isFullscreenSimplifiedMode && selectedTabIndex > 2)
                {
                    SelectedTabIndex = 0;
                }
            }
        }

        public bool IsVisualSchemeSelectorVisible
        {
            get { return !isFullscreenSimplifiedMode; }
        }

        public bool IsFullTesterMode
        {
            get { return !isFullscreenSimplifiedMode; }
        }

        public string FullscreenNavigationHint
        {
            get { return L("LOCGT_FullscreenNavigationHint", "LB/RB sections  D-pad move  A select  Back+A latency  B close"); }
        }

        public void MoveSelectedTab(int direction)
        {
            var tabCount = isFullscreenSimplifiedMode ? 3 : 5;
            var next = SelectedTabIndex + direction;
            if (next < 0)
            {
                next = tabCount - 1;
            }
            else if (next >= tabCount)
            {
                next = 0;
            }

            SelectedTabIndex = next;
        }

        public bool HasController
        {
            get { return State.IsConnected; }
        }

        public bool IsNoControllerVisible
        {
            get { return !State.IsConnected; }
        }

        public bool IsInputLogEnabled
        {
            get { return isInputLogEnabled; }
            set
            {
                if (isInputLogEnabled == value)
                {
                    return;
                }

                isInputLogEnabled = value;
                if (!isInputLogEnabled)
                {
                    ClearInputHistory();
                }

                OnPropertyChanged("IsInputLogEnabled");
                OnPropertyChanged("IsInputLogDisabled");
                OnPropertyChanged("InputLogStatusLabel");
            }
        }

        public bool IsInputLogDisabled
        {
            get { return !isInputLogEnabled; }
        }

        public string InputLogStatusLabel
        {
            get
            {
                return isInputLogEnabled
                    ? L("LOCGT_InputLogEnabledHelp", "Input history is recording button changes for this session.")
                    : L("LOCGT_InputLogDisabledHelp", "Input history is paused. Enable it only when you need a detailed event log.");
            }
        }

        public string InputLogExportStatusLabel
        {
            get { return inputLogExportStatusLabel; }
        }

        public string BackendLabel
        {
            get { return L("LOCGT_PlayniteSdlBackend", "Playnite SDL2 / SDL GameController"); }
        }

        public string MappingStatusLabel
        {
            get
            {
                if (State.IsConnected)
                {
                    return L("LOCGT_MappingRecognized", "Mapped by SDL GameController");
                }

                return Controllers.Count == 0
                    ? L("LOCGT_MappingNoController", "No mapped controller detected")
                    : L("LOCGT_MappingWaiting", "Waiting for selected controller");
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

        public double CompactLeftStickDotX
        {
            get { return State.LeftStick.X * 13d; }
        }

        public double CompactLeftStickDotY
        {
            get { return -State.LeftStick.Y * 13d; }
        }

        public double CompactRightStickDotX
        {
            get { return State.RightStick.X * 13d; }
        }

        public double CompactRightStickDotY
        {
            get { return -State.RightStick.Y * 13d; }
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
            get { return CountPressedButtons(State.Buttons) + ExtraActiveButtonCount + (IsLeftTriggerActive ? 1 : 0) + (IsRightTriggerActive ? 1 : 0); }
        }

        public int ExtraActiveButtonCount
        {
            get { return CountPressedExtraButtons(State.ExtraButtons); }
        }

        public bool HasExtraButtons
        {
            get { return State.ExtraButtons != null && State.ExtraButtons.Count > 0; }
        }

        public bool IsFavoriteButtonActive
        {
            get { return State.ExtraButtons != null && State.ExtraButtons.Count > 0 && State.ExtraButtons[0].IsPressed; }
        }

        public string ExtraButtonSummaryLabel
        {
            get
            {
                if (!HasExtraButtons)
                {
                    return L("LOCGT_NoExtraButtons", "No additional buttons exposed by SDL.");
                }

                return string.Format(L("LOCGT_ExtraButtonsFormat", "{0} additional controls exposed by SDL"), State.ExtraButtons.Count);
            }
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
            get { return string.Format("{0:0.000}", CurrentCenterDrift); }
        }

        public string SessionRestDriftLabel
        {
            get { return string.Format("{0:0.000}", Math.Max(maxLeftRestDrift, maxRightRestDrift)); }
        }

        private double CurrentCenterDrift
        {
            get { return Math.Max(State.LeftStick.Magnitude, State.RightStick.Magnitude); }
        }

        private double EvaluatedRestDrift
        {
            get { return Math.Max(maxLeftRestDrift, maxRightRestDrift); }
        }

        private double HealthyDeadzoneThreshold
        {
            get { return Clamp(settings.HealthyDeadzone, 0.02d, 0.30d); }
        }

        private double MinorDriftThreshold
        {
            get { return Clamp(settings.MinorDriftThreshold, HealthyDeadzoneThreshold + 0.01d, 0.40d); }
        }

        private double AttentionDriftThreshold
        {
            get { return Clamp(settings.AttentionDriftThreshold, MinorDriftThreshold + 0.01d, 0.60d); }
        }

        private double StickEdgeThreshold
        {
            get { return Clamp(settings.StickEdgeThreshold, 0.50d, 1.00d); }
        }

        private float TriggerFullPressThreshold
        {
            get { return (float)Clamp(settings.TriggerFullPressThreshold, 0.50d, 1.00d); }
        }

        private int CenterCalibrationDurationMilliseconds
        {
            get { return (int)Clamp(settings.CenterCalibrationMilliseconds, 800d, 6000d); }
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

        public string CalibrationStatusLabel
        {
            get
            {
                if (isCenterCalibrationRunning)
                {
                    var remaining = Math.Max(0d, (centerCalibrationEndsAt - DateTime.UtcNow).TotalSeconds);
                    return string.Format(L("LOCGT_CalibrationRunningFormat", "Keep sticks released. Capturing center for {0:0.0}s."), remaining);
                }

                if (centerCalibrationSamples <= 0)
                {
                    return L("LOCGT_CalibrationNotRun", "Center calibration has not been captured yet.");
                }

                return string.Format(L("LOCGT_CalibrationSamplesFormat", "Center captured from {0} samples."), centerCalibrationSamples);
            }
        }

        public int CalibrationProgress
        {
            get
            {
                if (!isCenterCalibrationRunning)
                {
                    return centerCalibrationSamples > 0 ? 100 : 0;
                }

                var remaining = Math.Max(0d, (centerCalibrationEndsAt - DateTime.UtcNow).TotalMilliseconds);
                return Math.Max(0, Math.Min(100, 100 - (int)Math.Round(remaining * 100d / CenterCalibrationDurationMilliseconds)));
            }
        }

        public string LeftCalibrationCenterLabel
        {
            get { return string.Format(L("LOCGT_CenterFormat", "Center X {0:0.000}  Y {1:0.000}"), calibratedLeftCenterX, calibratedLeftCenterY); }
        }

        public string RightCalibrationCenterLabel
        {
            get { return string.Format(L("LOCGT_CenterFormat", "Center X {0:0.000}  Y {1:0.000}"), calibratedRightCenterX, calibratedRightCenterY); }
        }

        public string LeftRecommendedDeadzoneLabel
        {
            get { return GetRecommendedDeadzoneLabel(calibratedLeftCenterNoise); }
        }

        public string RightRecommendedDeadzoneLabel
        {
            get { return GetRecommendedDeadzoneLabel(calibratedRightCenterNoise); }
        }

        public int LeftRecommendedDeadzonePercent
        {
            get { return GetRecommendedDeadzonePercent(calibratedLeftCenterNoise); }
        }

        public int RightRecommendedDeadzonePercent
        {
            get { return GetRecommendedDeadzonePercent(calibratedRightCenterNoise); }
        }

        public string LeftRangeQualityLabel
        {
            get { return GetRangeQualityLabel(leftStickDiagnostics); }
        }

        public string RightRangeQualityLabel
        {
            get { return GetRangeQualityLabel(rightStickDiagnostics); }
        }

        public int LeftRangeQualityPercent
        {
            get { return GetRangeQualityPercent(leftStickDiagnostics); }
        }

        public int RightRangeQualityPercent
        {
            get { return GetRangeQualityPercent(rightStickDiagnostics); }
        }

        public string LatencyStatusLabel
        {
            get
            {
                return hasLatencyTestStarted
                    ? latencyStatusLabel
                    : "-";
            }
        }

        public string StartLatencyButtonLabel
        {
            get
            {
                return isLatencyTestRunning
                    ? L("LOCGT_StopLatency", "Stop latency")
                    : L("LOCGT_StartLatency", "Start latency");
            }
        }

        public string LatencyResultLabel
        {
            get
            {
                if (isLatencyTestRunning)
                {
                    return "- ms";
                }

                if (!hasLatencyTestStarted || latencyTestSamples == 0)
                {
                    return "- ms";
                }

                return string.Format("{0:0} ms", lastLatencyMs);
            }
        }

        public string LatencyStatsLabel
        {
            get
            {
                if (!hasLatencyTestStarted || latencyTestSamples == 0)
                {
                    return "-";
                }

                return string.Format(L("LOCGT_LatencyStatsFormat", "Best {0:0} ms  Average {1:0} ms  Samples {2}"),
                    bestLatencyMs,
                    latencyTestSumMs / latencyTestSamples,
                    latencyTestSamples);
            }
        }

        public string PollingLatencyAverageLabel
        {
            get
            {
                if (!hasLatencyTestStarted || inputEventIntervalSamples == 0)
                {
                    return "-";
                }

                return string.Format(L("LOCGT_PollingHintFormat", "Polling avg {0:0.0} ms"), inputEventIntervalSumMs / inputEventIntervalSamples);
            }
        }

        public string LatencySampleCountLabel
        {
            get
            {
                return hasLatencyTestStarted
                    ? string.Format(L("LOCGT_LatencySamplesFormat", "{0} samples"), inputEventIntervalSamples)
                    : L("LOCGT_LatencyNoSamples", "No samples");
            }
        }

        public string LatencyRangeLabel
        {
            get
            {
                if (!hasLatencyTestStarted || inputEventIntervalSamples == 0 || inputEventIntervalMinMs == double.MaxValue)
                {
                    return "-";
                }

                return string.Format(L("LOCGT_LatencyRangeFormat", "{0:0.0} ms min / {1:0.0} ms max"),
                    inputEventIntervalMinMs,
                    inputEventIntervalMaxMs);
            }
        }

        public string LatencyTestDurationLabel
        {
            get
            {
                if (!hasLatencyTestStarted)
                {
                    return "-";
                }

                var seconds = Math.Max(0d, (DateTime.UtcNow - latencyTestStartedAt).TotalSeconds);
                return string.Format(L("LOCGT_LatencyDurationFormat", "{0:0}s session"), seconds);
            }
        }

        public string PollingRateCurrentLabel
        {
            get
            {
                return hasLatencyTestStarted && inputEventIntervalSamples > 0
                    ? GetHzLabel(currentInputEventIntervalMs)
                    : "- Hz";
            }
        }

        public string PollingRateAverageValueLabel
        {
            get
            {
                if (!hasLatencyTestStarted || inputEventIntervalSamples == 0)
                {
                    return "- Hz";
                }

                return GetHzLabel(inputEventIntervalSumMs / inputEventIntervalSamples);
            }
        }

        public string PollingRateMaxValueLabel
        {
            get
            {
                if (!hasLatencyTestStarted || inputEventIntervalMinMs == double.MaxValue)
                {
                    return "- Hz";
                }

                return GetHzLabel(inputEventIntervalMinMs);
            }
        }

        public string PollingJitterLabel
        {
            get
            {
                if (!hasLatencyTestStarted || inputEventIntervalSamples == 0)
                {
                    return "- ms";
                }

                return string.Format("{0:0.0} ms", Math.Max(0d, inputEventIntervalMaxMs - inputEventIntervalMinMs));
            }
        }

        public string EstimatedDelayLabel
        {
            get
            {
                if (!hasLatencyTestStarted || inputEventIntervalSamples == 0)
                {
                    return "- ms";
                }

                return string.Format("{0:0.0} ms", inputEventIntervalSumMs / inputEventIntervalSamples);
            }
        }

        public string InputEventLatencyAverageLabel
        {
            get
            {
                if (!hasLatencyTestStarted || inputEventIntervalSamples == 0)
                {
                    return "-";
                }

                return string.Format(L("LOCGT_EventIntervalFormat", "Observed input event interval: {0:0.0} ms avg"), inputEventIntervalSumMs / inputEventIntervalSamples);
            }
        }

        public PointCollection LatencyRateGraphPoints
        {
            get
            {
                var points = new PointCollection();
                if (!hasLatencyTestStarted || latencyRateHistory.Count == 0)
                {
                    return points;
                }

                const double width = 540d;
                const double height = 132d;
                var values = new List<double>(latencyRateHistory);
                var step = values.Count <= 1 ? width : width / (values.Count - 1);
                for (var index = 0; index < values.Count; index++)
                {
                    var normalized = Math.Max(0d, Math.Min(1d, values[index] / 1000d));
                    points.Add(new Point(index * step, height - (normalized * height)));
                }

                return points;
            }
        }

        public int QuickTestProgress
        {
            get
            {
                const int totalChecks = 19;
                var completed = CountPressedButtons(coveredButtons);

                if (maxLeftTrigger >= TriggerFullPressThreshold)
                {
                    completed++;
                }

                if (maxRightTrigger >= TriggerFullPressThreshold)
                {
                    completed++;
                }

                if (maxLeftStickMagnitude >= StickEdgeThreshold)
                {
                    completed++;
                }

                if (maxRightStickMagnitude >= StickEdgeThreshold)
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
                    return L("LOCGT_ConnectControllerToStart", "Connect a controller to start.");
                }

                if (QuickTestProgress == 100)
                {
                    return L("LOCGT_AllControlsCovered", "All normalized controls covered.");
                }

                return string.Format(L("LOCGT_PercentCompleteFormat", "{0}% complete"), QuickTestProgress);
            }
        }

        public string ButtonCoverageLabel
        {
            get { return string.Format(L("LOCGT_ButtonsSeenFormat", "{0}/15 buttons seen"), CountPressedButtons(coveredButtons)); }
        }

        public string AnalogCoverageLabel
        {
            get
            {
                return string.Format("{0} {1}%  {2} {3}%  LS {4}%  RS {5}%",
                    LeftTriggerLabel,
                    (int)Math.Round(maxLeftTrigger * 100),
                    RightTriggerLabel,
                    (int)Math.Round(maxRightTrigger * 100),
                    (int)Math.Round(maxLeftStickMagnitude * 100),
                    (int)Math.Round(maxRightStickMagnitude * 100));
            }
        }

        public string QuickTestMissingLabel
        {
            get
            {
                var missing = GetMissingInputLabels();
                return missing.Count == 0 ? L("LOCGT_NothingMissing", "Nothing missing.") : string.Join(", ", missing);
            }
        }

        public bool CoveredSouth { get { return coveredButtons.South; } }
        public bool CoveredEast { get { return coveredButtons.East; } }
        public bool CoveredWest { get { return coveredButtons.West; } }
        public bool CoveredNorth { get { return coveredButtons.North; } }
        public bool CoveredLeftShoulder { get { return coveredButtons.LeftShoulder; } }
        public bool CoveredRightShoulder { get { return coveredButtons.RightShoulder; } }
        public bool CoveredLeftStickButton { get { return coveredButtons.LeftStick; } }
        public bool CoveredRightStickButton { get { return coveredButtons.RightStick; } }
        public bool CoveredBack { get { return coveredButtons.Back; } }
        public bool CoveredStart { get { return coveredButtons.Start; } }
        public bool CoveredGuide { get { return coveredButtons.Guide; } }
        public bool CoveredDpadUp { get { return coveredButtons.DpadUp; } }
        public bool CoveredDpadDown { get { return coveredButtons.DpadDown; } }
        public bool CoveredDpadLeft { get { return coveredButtons.DpadLeft; } }
        public bool CoveredDpadRight { get { return coveredButtons.DpadRight; } }
        public bool CoveredLeftTrigger { get { return maxLeftTrigger >= TriggerFullPressThreshold; } }
        public bool CoveredRightTrigger { get { return maxRightTrigger >= TriggerFullPressThreshold; } }
        public bool CoveredLeftStickRange { get { return maxLeftStickMagnitude >= StickEdgeThreshold; } }
        public bool CoveredRightStickRange { get { return maxRightStickMagnitude >= StickEdgeThreshold; } }

        public int GuidedTestProgress
        {
            get
            {
                if (GuidedTestInputs == null || GuidedTestInputs.Count == 0)
                {
                    return 0;
                }

                return Math.Max(0, Math.Min(100, (int)Math.Round(guidedTestStepIndex * 100d / GuidedTestInputs.Count)));
            }
        }

        public string GuidedTestButtonLabel
        {
            get
            {
                return isGuidedTestRunning
                    ? L("LOCGT_RestartGuidedTest", "Restart guided test")
                    : L("LOCGT_StartGuidedTest", "Start guided test");
            }
        }

        public string GuidedTestStatusLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return L("LOCGT_ConnectControllerToStart", "Connect a controller to start.");
                }

                if (!isGuidedTestRunning && guidedTestStepIndex == 0)
                {
                    return L("LOCGT_GuidedTestReady", "Start a guided pass to verify every normalized input.");
                }

                var current = GetCurrentGuidedInputLabel();
                if (current == null)
                {
                    return L("LOCGT_GuidedTestComplete", "Guided test complete. All normalized controls were seen.");
                }

                return string.Format(L("LOCGT_GuidedTestStepFormat", "Next: {0}"), current);
            }
        }

        public string GuidedTestNextInputLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return "--";
                }

                var current = GetCurrentGuidedInputLabel();
                return current == null ? L("LOCGT_GuidedTestCompleteShort", "Complete") : current;
            }
        }

        public string GuidedTestActionLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return L("LOCGT_NoControllerDetected", "No controller detected");
                }

                return guidedTestStepIndex >= GuidedTestInputs.Count
                    ? L("LOCGT_AllControlsCovered", "All normalized controls covered.")
                    : L("LOCGT_PressThisControl", "Press this control");
            }
        }

        public int HealthScore
        {
            get
            {
                var drift = EvaluatedRestDrift;
                var driftPenalty = drift <= HealthyDeadzoneThreshold ? 0d : Math.Min(100d, (drift - HealthyDeadzoneThreshold) * 600d);
                return Math.Max(0, Math.Min(100, (int)Math.Round(100d - driftPenalty)));
            }
        }

        public string HealthLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return L("LOCGT_NoController", "No controller");
                }

                var drift = EvaluatedRestDrift;
                if (drift >= AttentionDriftThreshold)
                {
                    return L("LOCGT_HealthAttentionRequired", "Attention required");
                }

                if (drift >= MinorDriftThreshold)
                {
                    return L("LOCGT_HealthNeedsReview", "Needs review");
                }

                if (HealthScore >= 90)
                {
                    return L("LOCGT_HealthExcellent", "Excellent");
                }

                if (HealthScore >= 75)
                {
                    return L("LOCGT_HealthGood", "Good");
                }

                return L("LOCGT_HealthGood", "Good");
            }
        }

        public string HealthSummaryLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return L("LOCGT_ConnectControllerToStart", "Connect a controller to start.");
                }

                if (EvaluatedRestDrift < HealthyDeadzoneThreshold)
                {
                    return L("LOCGT_HealthSummaryCentered", "Centered sticks look stable right now.");
                }

                if (EvaluatedRestDrift < AttentionDriftThreshold)
                {
                    return L("LOCGT_HealthSummarySmallDrift", "Small centered-stick movement is visible. Release the sticks and watch whether it settles.");
                }

                return L("LOCGT_HealthSummaryReview", "Centered-stick drift is high enough to review deadzone, calibration, or hardware condition.");
            }
        }

        public string HealthDriftFactorLabel
        {
            get
            {
                return string.Format(L("LOCGT_HealthDriftFactorFormat", "Rest drift used for health: {0:0.000} ({1})"),
                    EvaluatedRestDrift,
                    GetDriftStatus(EvaluatedRestDrift));
            }
        }

        public string HealthRangeFactorLabel
        {
            get
            {
                return string.Format(L("LOCGT_HealthRangeFactorFormat", "Outer range seen: LS {0}% / RS {1}%"),
                    LeftStickMaxReachPercent,
                    RightStickMaxReachPercent);
            }
        }

        public string HealthCoverageFactorLabel
        {
            get
            {
                return string.Format(L("LOCGT_HealthCoverageFactorFormat", "Quick checks: {0}% ({1})"),
                    QuickTestProgress,
                    ButtonCoverageLabel);
            }
        }

        public string ExportReportStatusLabel
        {
            get { return exportReportStatusLabel; }
        }

        public string ControllerSummary
        {
            get
            {
                if (!State.IsConnected)
                {
                    return L("LOCGT_ConnectControllerAndPress", "Connect a controller and press any button.");
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

        public string DeviceCapabilitiesLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return "-";
                }

                return string.Format(L("LOCGT_DeviceCapabilitiesFormat", "{0} normalized controls, 2 sticks, 2 analog triggers, {1} extra controls"),
                    CountNormalizedControls(),
                    State.ExtraButtons == null ? 0 : State.ExtraButtons.Count);
            }
        }

        public string DeviceApiLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return "-";
                }

                return string.Format(L("LOCGT_DeviceApiFormat", "{0} via SDL GameController"), State.Layout);
            }
        }

        public string DeviceRumbleCapabilityLabel
        {
            get
            {
                if (!State.IsConnected)
                {
                    return "-";
                }

                return settings.EnableRumbleTests
                    ? L("LOCGT_RumbleCapabilityEnabled", "Rumble test enabled; hardware support depends on controller mode and driver.")
                    : L("LOCGT_RumbleCapabilityDisabled", "Rumble test disabled in plugin settings.");
            }
        }

        public string ExtraButtonDetailLabel
        {
            get
            {
                if (!HasExtraButtons)
                {
                    return L("LOCGT_NoExtraButtons", "No additional buttons exposed by SDL.");
                }

                var builder = new StringBuilder();
                for (var index = 0; index < State.ExtraButtons.Count; index++)
                {
                    if (index > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(State.ExtraButtons[index].Label);
                }

                return builder.ToString();
            }
        }

        public string SelectedVisualSchemeKey
        {
            get { return selectedVisualSchemeKey; }
            set
            {
                if (selectedVisualSchemeKey == value)
                {
                    return;
                }

                selectedVisualSchemeKey = value;
                isVisualSchemeManuallySelected = true;
                NotifyVisualSchemeChanged();
            }
        }

        public double ControllerVisualWidth
        {
            get { return EffectiveVisualSchemeDefinition.TestWidth; }
        }

        public double ControllerVisualHeight
        {
            get { return EffectiveVisualSchemeDefinition.TestHeight; }
        }

        public double GuidedControllerVisualWidth
        {
            get { return EffectiveVisualSchemeDefinition.GuidedWidth; }
        }

        public double GuidedControllerVisualHeight
        {
            get { return EffectiveVisualSchemeDefinition.GuidedHeight; }
        }

        public string SouthLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "Cross";
                }

                return UsesSwitchProLabels ? "B" : "A";
            }
        }

        public string EastLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "Circle";
                }

                return UsesSwitchProLabels ? "A" : "B";
            }
        }

        public string WestLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "Square";
                }

                return UsesSwitchProLabels ? "Y" : "X";
            }
        }

        public string NorthLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "Triangle";
                }

                return UsesSwitchProLabels ? "X" : "Y";
            }
        }

        public string LeftShoulderLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "L1";
                }

                return UsesSwitchProLabels ? "L" : "LB";
            }
        }

        public string RightShoulderLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "R1";
                }

                return UsesSwitchProLabels ? "R" : "RB";
            }
        }

        public string LeftTriggerLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "L2";
                }

                return UsesSwitchProLabels ? "ZL" : "LT";
            }
        }

        public string RightTriggerLabel
        {
            get
            {
                if (UsesPlayStationLabels)
                {
                    return "R2";
                }

                return UsesSwitchProLabels ? "ZR" : "RT";
            }
        }

        public string LeftStickButtonLabel
        {
            get { return UsesPlayStationLabels ? "L3" : UsesSwitchProLabels ? "L Stick" : "LS"; }
        }

        public string RightStickButtonLabel
        {
            get { return UsesPlayStationLabels ? "R3" : UsesSwitchProLabels ? "R Stick" : "RS"; }
        }

        public string BackButtonLabel
        {
            get { return UsesPlayStationLabels ? "Share" : UsesSwitchProLabels ? "Minus" : "View"; }
        }

        public string StartButtonLabel
        {
            get { return UsesPlayStationLabels ? "Options" : UsesSwitchProLabels ? "Plus" : "Menu"; }
        }

        public string GuideButtonLabel
        {
            get { return UsesPlayStationLabels ? "PS" : "Guide"; }
        }

        public string DpadUpLabel
        {
            get { return UsesPlayStationLabels ? "D-pad Up" : "D-Up"; }
        }

        public string DpadDownLabel
        {
            get { return UsesPlayStationLabels ? "D-pad Down" : "D-Down"; }
        }

        public string DpadLeftLabel
        {
            get { return UsesPlayStationLabels ? "D-pad Left" : "D-Left"; }
        }

        public string DpadRightLabel
        {
            get { return UsesPlayStationLabels ? "D-pad Right" : "D-Right"; }
        }

        private bool UsesSwitchProLabels
        {
            get { return ControllerVisualSchemeCatalog.UsesSwitchProLabels(EffectiveVisualSchemeKey); }
        }

        private bool UsesPlayStationLabels
        {
            get { return ControllerVisualSchemeCatalog.UsesPlayStationLabels(EffectiveVisualSchemeKey); }
        }

        public bool IsEightBitDoLayout
        {
            get { return State.Layout == GamepadLayout.EightBitDo; }
        }

        public bool IsEightBitDoPro3Artwork
        {
            get
            {
                return State.Layout == GamepadLayout.EightBitDo &&
                    (State.EightBitDoModel == EightBitDoModel.Pro2 ||
                     State.EightBitDoModel == EightBitDoModel.Pro3);
            }
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

        public bool IsDualSenseLayout
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.DualSense; }
        }

        public bool IsXboxVisualScheme
        {
            get { return IsXboxOneVisualScheme; }
        }

        public bool IsXboxOneVisualScheme
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.XboxOne; }
        }

        public bool IsXboxSeriesVisualScheme
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.XboxSeries; }
        }

        public bool IsSteamControllerVisualScheme
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.SteamController; }
        }

        public bool IsPlayStationVisualScheme
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.PlayStation; }
        }

        public bool IsSwitchProVisualScheme
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.SwitchPro; }
        }

        public bool IsEightBitDoUltimateVisualScheme
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.EightBitDoUltimate; }
        }

        public bool IsEightBitDoUltimate2VisualScheme
        {
            get { return IsEightBitDoUltimateVisualScheme; }
        }

        public bool IsEightBitDoProVisualScheme
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.EightBitDoPro; }
        }

        public bool IsUniversalControllerArtwork
        {
            get { return EffectiveVisualSchemeKey == ControllerVisualSchemeCatalog.Universal; }
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
                SyncDetectedVisualScheme();
                RaiseRumbleCanExecuteChanged();
                startCenterCalibrationCommand.RaiseCanExecuteChanged();
                startLatencyTestCommand.RaiseCanExecuteChanged();
                exportLatencyCommand.RaiseCanExecuteChanged();
                exportSticksCommand.RaiseCanExecuteChanged();
            }));
        }

        private string EffectiveVisualSchemeKey
        {
            get
            {
                return string.IsNullOrEmpty(selectedVisualSchemeKey)
                    ? ControllerVisualSchemeCatalog.Detect(State)
                    : selectedVisualSchemeKey;
            }
        }

        private ControllerVisualSchemeDefinition EffectiveVisualSchemeDefinition
        {
            get { return ControllerVisualSchemeCatalog.GetDefinition(EffectiveVisualSchemeKey, L); }
        }

        private void InitializeVisualSchemeOptions()
        {
            foreach (var option in ControllerVisualSchemeCatalog.CreateOptions(L))
            {
                VisualSchemeOptions.Add(option);
            }
        }

        private void InitializeGuidedTestInputs()
        {
            GuidedTestInputs.Add(new GuidedTestInputItem("South"));
            GuidedTestInputs.Add(new GuidedTestInputItem("East"));
            GuidedTestInputs.Add(new GuidedTestInputItem("West"));
            GuidedTestInputs.Add(new GuidedTestInputItem("North"));
            GuidedTestInputs.Add(new GuidedTestInputItem("LeftShoulder"));
            GuidedTestInputs.Add(new GuidedTestInputItem("RightShoulder"));
            GuidedTestInputs.Add(new GuidedTestInputItem("LeftTrigger"));
            GuidedTestInputs.Add(new GuidedTestInputItem("RightTrigger"));
            GuidedTestInputs.Add(new GuidedTestInputItem("LeftStick"));
            GuidedTestInputs.Add(new GuidedTestInputItem("RightStick"));
            GuidedTestInputs.Add(new GuidedTestInputItem("Back"));
            GuidedTestInputs.Add(new GuidedTestInputItem("Start"));
            GuidedTestInputs.Add(new GuidedTestInputItem("Guide"));
            GuidedTestInputs.Add(new GuidedTestInputItem("DpadUp"));
            GuidedTestInputs.Add(new GuidedTestInputItem("DpadDown"));
            GuidedTestInputs.Add(new GuidedTestInputItem("DpadLeft"));
            GuidedTestInputs.Add(new GuidedTestInputItem("DpadRight"));
            GuidedTestInputs.Add(new GuidedTestInputItem("LeftStickRange"));
            GuidedTestInputs.Add(new GuidedTestInputItem("RightStickRange"));
            RefreshGuidedTestInputs();
        }

        private void SyncDetectedVisualScheme()
        {
            if (isVisualSchemeManuallySelected)
            {
                return;
            }

            var detectedSchemeKey = ControllerVisualSchemeCatalog.Detect(State);
            if (selectedVisualSchemeKey == detectedSchemeKey)
            {
                return;
            }

            selectedVisualSchemeKey = detectedSchemeKey;
            NotifyVisualSchemeChanged();
        }

        private void NotifyVisualSchemeChanged()
        {
            OnPropertyChanged("SelectedVisualSchemeKey");
            OnPropertyChanged("ControllerVisualWidth");
            OnPropertyChanged("ControllerVisualHeight");
            OnPropertyChanged("GuidedControllerVisualWidth");
            OnPropertyChanged("GuidedControllerVisualHeight");
            OnPropertyChanged("IsDualSenseLayout");
            OnPropertyChanged("IsXboxVisualScheme");
            OnPropertyChanged("IsXboxOneVisualScheme");
            OnPropertyChanged("IsXboxSeriesVisualScheme");
            OnPropertyChanged("IsSteamControllerVisualScheme");
            OnPropertyChanged("IsPlayStationVisualScheme");
            OnPropertyChanged("IsSwitchProVisualScheme");
            OnPropertyChanged("IsEightBitDoUltimateVisualScheme");
            OnPropertyChanged("IsEightBitDoUltimate2VisualScheme");
            OnPropertyChanged("IsEightBitDoProVisualScheme");
            OnPropertyChanged("IsUniversalControllerArtwork");
            OnPropertyChanged("SouthLabel");
            OnPropertyChanged("EastLabel");
            OnPropertyChanged("WestLabel");
            OnPropertyChanged("NorthLabel");
            OnPropertyChanged("LeftShoulderLabel");
            OnPropertyChanged("RightShoulderLabel");
            OnPropertyChanged("LeftTriggerLabel");
            OnPropertyChanged("RightTriggerLabel");
            OnPropertyChanged("LeftStickButtonLabel");
            OnPropertyChanged("RightStickButtonLabel");
            OnPropertyChanged("BackButtonLabel");
            OnPropertyChanged("StartButtonLabel");
            OnPropertyChanged("GuideButtonLabel");
            OnPropertyChanged("DpadUpLabel");
            OnPropertyChanged("DpadDownLabel");
            OnPropertyChanged("DpadLeftLabel");
            OnPropertyChanged("DpadRightLabel");
            OnPropertyChanged("AnalogCoverageLabel");
            OnPropertyChanged("QuickTestMissingLabel");
            RefreshGuidedTestInputs();
        }

        private void UpdateDiagnostics(GamepadState nextState)
        {
            UpdateLatency(nextState);

            if (!nextState.IsConnected)
            {
                previousButtons = null;
                previousExtraButtons = null;
                return;
            }

            UpdateCenterCalibration(nextState);

            TrackRestDrift(nextState);

            leftStickDiagnostics.AddSample(nextState.LeftStick);
            rightStickDiagnostics.AddSample(nextState.RightStick);
            UpdateCoverage(nextState);
            UpdateGuidedTestProgress(nextState);

            if (previousButtons == null)
            {
                previousButtons = CopyButtons(nextState.Buttons);
                previousExtraButtons = CopyExtraButtons(nextState.ExtraButtons);
                return;
            }

            TrackButtonChange(SouthLabel, previousButtons.South, nextState.Buttons.South);
            TrackButtonChange(EastLabel, previousButtons.East, nextState.Buttons.East);
            TrackButtonChange(WestLabel, previousButtons.West, nextState.Buttons.West);
            TrackButtonChange(NorthLabel, previousButtons.North, nextState.Buttons.North);
            TrackButtonChange(LeftShoulderLabel, previousButtons.LeftShoulder, nextState.Buttons.LeftShoulder);
            TrackButtonChange(RightShoulderLabel, previousButtons.RightShoulder, nextState.Buttons.RightShoulder);
            TrackButtonChange(BackButtonLabel, previousButtons.Back, nextState.Buttons.Back);
            TrackButtonChange(StartButtonLabel, previousButtons.Start, nextState.Buttons.Start);
            TrackButtonChange(GuideButtonLabel, previousButtons.Guide, nextState.Buttons.Guide);
            TrackButtonChange("Touchpad", previousButtons.Touchpad, nextState.Buttons.Touchpad);
            TrackButtonChange(LeftStickButtonLabel, previousButtons.LeftStick, nextState.Buttons.LeftStick);
            TrackButtonChange(RightStickButtonLabel, previousButtons.RightStick, nextState.Buttons.RightStick);
            TrackButtonChange(LeftTriggerLabel, State.LeftTrigger > 0.02f, nextState.LeftTrigger > 0.02f);
            TrackButtonChange(RightTriggerLabel, State.RightTrigger > 0.02f, nextState.RightTrigger > 0.02f);
            TrackButtonChange(DpadUpLabel, previousButtons.DpadUp, nextState.Buttons.DpadUp);
            TrackButtonChange(DpadDownLabel, previousButtons.DpadDown, nextState.Buttons.DpadDown);
            TrackButtonChange(DpadLeftLabel, previousButtons.DpadLeft, nextState.Buttons.DpadLeft);
            TrackButtonChange(DpadRightLabel, previousButtons.DpadRight, nextState.Buttons.DpadRight);
            TrackExtraButtonChanges(previousExtraButtons, nextState.ExtraButtons);

            previousButtons = CopyButtons(nextState.Buttons);
            previousExtraButtons = CopyExtraButtons(nextState.ExtraButtons);
        }

        private void TrackExtraButtonChanges(IList<ExtraButtonState> previous, IList<ExtraButtonState> current)
        {
            if (current == null)
            {
                return;
            }

            for (var index = 0; index < current.Count; index++)
            {
                var currentButton = current[index];
                var previousPressed = false;
                if (previous != null)
                {
                    for (var previousIndex = 0; previousIndex < previous.Count; previousIndex++)
                    {
                        if (previous[previousIndex].RawIndex == currentButton.RawIndex)
                        {
                            previousPressed = previous[previousIndex].IsPressed;
                            break;
                        }
                    }
                }

                TrackButtonChange(currentButton.Label, previousPressed, currentButton.IsPressed);
            }
        }

        private void TrackButtonChange(string inputName, bool previous, bool current)
        {
            if (previous == current)
            {
                return;
            }

            if (isLatencyTestRunning)
            {
                TrackLatencyTest(current);
            }
            else
            {
                TrackInputEventLatency();
            }

            if (!isInputLogEnabled)
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

            inputLogExportStatusLabel = string.Format(L("LOCGT_InputLogEntriesFormat", "{0} entries ready to export."), InputHistory.Count);
            OnPropertyChanged("InputLogExportStatusLabel");
            exportInputLogCommand.RaiseCanExecuteChanged();
            resetInputLogCommand.RaiseCanExecuteChanged();
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
                OnPropertyChanged("MappingStatusLabel");
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
            OnPropertyChanged("MappingStatusLabel");
        }

        private bool CanRunRumble()
        {
            return settings.EnableRumbleTests && State.IsConnected && !isRumbleRunning;
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
            isGuidedTestRunning = false;
            guidedTestStepIndex = 0;
            maxLeftRestDrift = 0d;
            maxRightRestDrift = 0d;
            ResetRestDriftCandidate();
            maxLeftStickMagnitude = 0d;
            maxRightStickMagnitude = 0d;
            maxLeftTrigger = 0f;
            maxRightTrigger = 0f;
            coveredButtons = new GamepadButtonState();
            leftStickDiagnostics.Reset();
            rightStickDiagnostics.Reset();
            ClearInputHistory();
            ResetCalibration();
            ResetLatency();
            NotifyStateChanged();
        }

        private void ClearInputHistory()
        {
            InputHistory.Clear();
            inputLogExportStatusLabel = L("LOCGT_InputLogExportReady", "Enable input log and press buttons to collect entries.");
            OnPropertyChanged("InputHistory");
            OnPropertyChanged("InputLogExportStatusLabel");
            exportInputLogCommand.RaiseCanExecuteChanged();
            resetInputLogCommand.RaiseCanExecuteChanged();
        }

        private void StartCenterCalibration()
        {
            if (!State.IsConnected || isCenterCalibrationRunning)
            {
                return;
            }

            isCenterCalibrationRunning = true;
            centerCalibrationEndsAt = DateTime.UtcNow.AddMilliseconds(CenterCalibrationDurationMilliseconds);
            centerCalibrationSamples = 0;
            leftCenterXSum = 0d;
            leftCenterYSum = 0d;
            rightCenterXSum = 0d;
            rightCenterYSum = 0d;
            leftCenterMaxNoise = 0d;
            rightCenterMaxNoise = 0d;
            OnPropertyChanged("CalibrationStatusLabel");
            OnPropertyChanged("CalibrationProgress");
            startCenterCalibrationCommand.RaiseCanExecuteChanged();
        }

        private void StartGuidedTest()
        {
            if (!State.IsConnected)
            {
                return;
            }

            ResetDiagnostics();
            isGuidedTestRunning = true;
            guidedTestStepIndex = 0;
            RefreshGuidedTestInputs();
            OnPropertyChanged("GuidedTestProgress");
            OnPropertyChanged("GuidedTestButtonLabel");
            OnPropertyChanged("GuidedTestStatusLabel");
            OnPropertyChanged("GuidedTestNextInputLabel");
            OnPropertyChanged("GuidedTestActionLabel");
        }

        private void OpenGuidedTest()
        {
            if (!State.IsConnected || openGuidedTest == null)
            {
                return;
            }

            openGuidedTest(this);
        }

        private void ResetCalibration()
        {
            isCenterCalibrationRunning = false;
            centerCalibrationSamples = 0;
            leftCenterXSum = 0d;
            leftCenterYSum = 0d;
            rightCenterXSum = 0d;
            rightCenterYSum = 0d;
            leftCenterMaxNoise = 0d;
            rightCenterMaxNoise = 0d;
            calibratedLeftCenterX = 0d;
            calibratedLeftCenterY = 0d;
            calibratedRightCenterX = 0d;
            calibratedRightCenterY = 0d;
            calibratedLeftCenterNoise = 0d;
            calibratedRightCenterNoise = 0d;
            OnPropertyChanged("CalibrationStatusLabel");
            OnPropertyChanged("CalibrationProgress");
            OnPropertyChanged("LeftCalibrationCenterLabel");
            OnPropertyChanged("RightCalibrationCenterLabel");
            OnPropertyChanged("LeftRecommendedDeadzoneLabel");
            OnPropertyChanged("RightRecommendedDeadzoneLabel");
            OnPropertyChanged("LeftRecommendedDeadzonePercent");
            OnPropertyChanged("RightRecommendedDeadzonePercent");
            startCenterCalibrationCommand.RaiseCanExecuteChanged();
        }

        private void ResetStickRangeDiagnostics()
        {
            maxLeftStickMagnitude = 0d;
            maxRightStickMagnitude = 0d;
            leftStickDiagnostics.Reset();
            rightStickDiagnostics.Reset();
            OnPropertyChanged("LeftStickCircularCoverageGeometry");
            OnPropertyChanged("RightStickCircularCoverageGeometry");
            OnPropertyChanged("LeftStickCircularCoveragePercent");
            OnPropertyChanged("RightStickCircularCoveragePercent");
            OnPropertyChanged("LeftStickMaxReachPercent");
            OnPropertyChanged("RightStickMaxReachPercent");
            OnPropertyChanged("LeftStickCircularCoverageLabel");
            OnPropertyChanged("RightStickCircularCoverageLabel");
            OnPropertyChanged("LeftStickPathSampleLabel");
            OnPropertyChanged("RightStickPathSampleLabel");
            OnPropertyChanged("LeftStickMaxReachLabel");
            OnPropertyChanged("RightStickMaxReachLabel");
            OnPropertyChanged("LeftStickAxisRangeLabel");
            OnPropertyChanged("RightStickAxisRangeLabel");
            OnPropertyChanged("LeftStickAverageMagnitudeLabel");
            OnPropertyChanged("RightStickAverageMagnitudeLabel");
            OnPropertyChanged("LeftRangeQualityLabel");
            OnPropertyChanged("RightRangeQualityLabel");
            OnPropertyChanged("LeftRangeQualityPercent");
            OnPropertyChanged("RightRangeQualityPercent");
            OnPropertyChanged("HealthRangeFactorLabel");
        }

        private void ResetLatency()
        {
            lastStateSampleAt = null;
            lastInputEventAt = null;
            currentPollingIntervalMs = 0d;
            pollingIntervalSumMs = 0d;
            pollingIntervalMinMs = double.MaxValue;
            pollingIntervalMaxMs = 0d;
            pollingIntervalSamples = 0;
            inputEventIntervalSumMs = 0d;
            inputEventIntervalMinMs = double.MaxValue;
            inputEventIntervalMaxMs = 0d;
            inputEventIntervalSamples = 0;
            currentInputEventIntervalMs = 0d;
            latencyRateHistory.Clear();
            hasLatencyTestStarted = false;
            isLatencyTestRunning = false;
            lastLatencyMs = 0d;
            bestLatencyMs = 0d;
            latencyTestSumMs = 0d;
            latencyTestSamples = 0;
            latencyStatusLabel = L("LOCGT_LatencyWaiting", "Waiting for input changes.");
            OnPropertyChanged("LatencyStatusLabel");
            OnPropertyChanged("StartLatencyButtonLabel");
            OnPropertyChanged("LatencyResultLabel");
            OnPropertyChanged("LatencyStatsLabel");
            OnPropertyChanged("PollingLatencyAverageLabel");
            OnPropertyChanged("InputEventLatencyAverageLabel");
            OnPropertyChanged("LatencySampleCountLabel");
            OnPropertyChanged("LatencyRangeLabel");
            OnPropertyChanged("LatencyTestDurationLabel");
            OnPropertyChanged("PollingRateCurrentLabel");
            OnPropertyChanged("PollingRateAverageValueLabel");
            OnPropertyChanged("PollingRateMaxValueLabel");
            OnPropertyChanged("PollingJitterLabel");
            OnPropertyChanged("EstimatedDelayLabel");
            OnPropertyChanged("LatencyRateGraphPoints");
            startLatencyTestCommand.RaiseCanExecuteChanged();
            resetLatencyCommand.RaiseCanExecuteChanged();
            exportLatencyCommand.RaiseCanExecuteChanged();
        }

        private void ToggleLatencyTest()
        {
            if (isLatencyTestRunning)
            {
                StopLatencyTest();
                return;
            }

            StartLatencyTest();
        }

        private void StartLatencyTest()
        {
            if (!State.IsConnected)
            {
                return;
            }

            isLatencyTestRunning = true;
            hasLatencyTestStarted = true;
            lastInputEventAt = null;
            currentInputEventIntervalMs = 0d;
            inputEventIntervalSumMs = 0d;
            inputEventIntervalMinMs = double.MaxValue;
            inputEventIntervalMaxMs = 0d;
            inputEventIntervalSamples = 0;
            lastLatencyMs = 0d;
            bestLatencyMs = 0d;
            latencyTestSumMs = 0d;
            latencyTestSamples = 0;
            latencyRateHistory.Clear();
            latencyTestStartedAt = DateTime.UtcNow;
            latencyStatusLabel = L("LOCGT_LatencyArmed", "Latency test armed. Press any controller button.");
            OnPropertyChanged("LatencyStatusLabel");
            OnPropertyChanged("StartLatencyButtonLabel");
            OnPropertyChanged("LatencyResultLabel");
            OnPropertyChanged("LatencyStatsLabel");
            OnPropertyChanged("PollingRateCurrentLabel");
            OnPropertyChanged("PollingRateAverageValueLabel");
            OnPropertyChanged("PollingRateMaxValueLabel");
            OnPropertyChanged("PollingJitterLabel");
            OnPropertyChanged("EstimatedDelayLabel");
            OnPropertyChanged("InputEventLatencyAverageLabel");
            OnPropertyChanged("LatencySampleCountLabel");
            OnPropertyChanged("LatencyRangeLabel");
            OnPropertyChanged("LatencyTestDurationLabel");
            OnPropertyChanged("LatencyRateGraphPoints");
            startLatencyTestCommand.RaiseCanExecuteChanged();
            resetLatencyCommand.RaiseCanExecuteChanged();
            exportLatencyCommand.RaiseCanExecuteChanged();
        }

        private void StopLatencyTest()
        {
            if (!isLatencyTestRunning)
            {
                return;
            }

            isLatencyTestRunning = false;
            latencyStatusLabel = latencyTestSamples == 0
                ? L("LOCGT_LatencyStoppedNoSample", "Latency test stopped. No button press was captured.")
                : string.Format(L("LOCGT_LatencyStoppedFormat", "Latency test stopped. Last captured value: {0:0} ms."), lastLatencyMs);
            OnPropertyChanged("LatencyStatusLabel");
            OnPropertyChanged("StartLatencyButtonLabel");
            OnPropertyChanged("LatencyResultLabel");
            OnPropertyChanged("LatencyStatsLabel");
            OnPropertyChanged("PollingRateCurrentLabel");
            OnPropertyChanged("PollingRateAverageValueLabel");
            OnPropertyChanged("PollingRateMaxValueLabel");
            OnPropertyChanged("PollingJitterLabel");
            OnPropertyChanged("EstimatedDelayLabel");
            OnPropertyChanged("InputEventLatencyAverageLabel");
            OnPropertyChanged("LatencySampleCountLabel");
            OnPropertyChanged("LatencyRangeLabel");
            OnPropertyChanged("LatencyTestDurationLabel");
            startLatencyTestCommand.RaiseCanExecuteChanged();
            resetLatencyCommand.RaiseCanExecuteChanged();
            exportLatencyCommand.RaiseCanExecuteChanged();
        }

        private void ExportReport()
        {
            try
            {
                var fileName = string.Format("GamepadTester-report-{0:yyyyMMdd-HHmmss}.txt", DateTime.Now);
                var path = PromptExportPath(fileName);
                if (string.IsNullOrWhiteSpace(path))
                {
                    exportReportStatusLabel = L("LOCGT_ExportCancelled", "Export cancelled.");
                    OnPropertyChanged("ExportReportStatusLabel");
                    return;
                }

                File.WriteAllText(path, BuildReportText(), Encoding.UTF8);
                exportReportStatusLabel = string.Format(L("LOCGT_ReportExportedFormat", "Report exported to {0}"), path);
            }
            catch (Exception ex)
            {
                exportReportStatusLabel = string.Format(L("LOCGT_ReportExportFailedFormat", "Could not export report: {0}"), ex.Message);
            }

            OnPropertyChanged("ExportReportStatusLabel");
        }

        private void ExportInputLog()
        {
            try
            {
                if (InputHistory.Count == 0)
                {
                    inputLogExportStatusLabel = L("LOCGT_InputLogExportEmpty", "No input log entries to export yet.");
                    OnPropertyChanged("InputLogExportStatusLabel");
                    return;
                }

                var fileName = string.Format("GamepadTester-input-log-{0:yyyyMMdd-HHmmss}.txt", DateTime.Now);
                var path = PromptExportPath(fileName);
                if (string.IsNullOrWhiteSpace(path))
                {
                    inputLogExportStatusLabel = L("LOCGT_ExportCancelled", "Export cancelled.");
                    OnPropertyChanged("InputLogExportStatusLabel");
                    return;
                }

                File.WriteAllText(path, BuildInputLogText(), Encoding.UTF8);
                inputLogExportStatusLabel = string.Format(L("LOCGT_InputLogExportedFormat", "Input log exported to {0}"), path);
            }
            catch (Exception ex)
            {
                inputLogExportStatusLabel = string.Format(L("LOCGT_InputLogExportFailedFormat", "Could not export input log: {0}"), ex.Message);
            }

            OnPropertyChanged("InputLogExportStatusLabel");
            exportInputLogCommand.RaiseCanExecuteChanged();
            resetInputLogCommand.RaiseCanExecuteChanged();
        }

        private void ExportLatencyData()
        {
            try
            {
                if (inputEventIntervalSamples == 0)
                {
                    exportReportStatusLabel = L("LOCGT_LatencyExportEmpty", "No latency samples to export yet.");
                    OnPropertyChanged("ExportReportStatusLabel");
                    return;
                }

                var fileName = string.Format("GamepadTester-latency-{0:yyyyMMdd-HHmmss}.txt", DateTime.Now);
                var path = PromptExportPath(fileName);
                if (string.IsNullOrWhiteSpace(path))
                {
                    exportReportStatusLabel = L("LOCGT_ExportCancelled", "Export cancelled.");
                    OnPropertyChanged("ExportReportStatusLabel");
                    return;
                }

                File.WriteAllText(path, BuildLatencyExportText(), Encoding.UTF8);
                exportReportStatusLabel = string.Format(L("LOCGT_LatencyExportedFormat", "Latency data exported to {0}"), path);
            }
            catch (Exception ex)
            {
                exportReportStatusLabel = string.Format(L("LOCGT_LatencyExportFailedFormat", "Could not export latency data: {0}"), ex.Message);
            }

            OnPropertyChanged("ExportReportStatusLabel");
            exportLatencyCommand.RaiseCanExecuteChanged();
        }

        private void ExportStickData()
        {
            try
            {
                var fileName = string.Format("GamepadTester-sticks-{0:yyyyMMdd-HHmmss}.txt", DateTime.Now);
                var path = PromptExportPath(fileName);
                if (string.IsNullOrWhiteSpace(path))
                {
                    exportReportStatusLabel = L("LOCGT_ExportCancelled", "Export cancelled.");
                    OnPropertyChanged("ExportReportStatusLabel");
                    return;
                }

                File.WriteAllText(path, BuildStickExportText(), Encoding.UTF8);
                exportReportStatusLabel = string.Format(L("LOCGT_SticksExportedFormat", "Stick data exported to {0}"), path);
            }
            catch (Exception ex)
            {
                exportReportStatusLabel = string.Format(L("LOCGT_SticksExportFailedFormat", "Could not export stick data: {0}"), ex.Message);
            }

            OnPropertyChanged("ExportReportStatusLabel");
        }

        private static string PromptExportPath(string defaultFileName)
        {
            var dialog = new SaveFileDialog
            {
                FileName = defaultFileName,
                DefaultExt = ".txt",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                AddExtension = true,
                OverwritePrompt = true
            };

            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrWhiteSpace(documents))
            {
                dialog.InitialDirectory = documents;
            }

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        private string BuildLatencyExportText()
        {
            var log = new StringBuilder();
            log.AppendLine("Gamepad Tester latency data");
            log.AppendLine(string.Format("Generated: {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
            log.AppendLine(string.Format("Controller: {0}", State.ControllerName));
            log.AppendLine(string.Format("Device: {0}", DeviceModelLabel));
            log.AppendLine(string.Format("Backend: {0}", BackendLabel));
            log.AppendLine();
            log.AppendLine("[Summary]");
            log.AppendLine(string.Format("Current rate: {0}", PollingRateCurrentLabel));
            log.AppendLine(string.Format("Max rate: {0}", PollingRateMaxValueLabel));
            log.AppendLine(string.Format("Average rate: {0}", PollingRateAverageValueLabel));
            log.AppendLine(string.Format("Estimated interval: {0}", EstimatedDelayLabel));
            log.AppendLine(string.Format("Jitter: {0}", PollingJitterLabel));
            log.AppendLine(string.Format("Samples: {0}", inputEventIntervalSamples));
            log.AppendLine(string.Format("Best interval: {0:0.0} ms", inputEventIntervalMinMs == double.MaxValue ? 0d : inputEventIntervalMinMs));
            log.AppendLine(string.Format("Worst interval: {0:0.0} ms", inputEventIntervalMaxMs));
            log.AppendLine(string.Format("Average interval: {0:0.0} ms", inputEventIntervalSamples == 0 ? 0d : inputEventIntervalSumMs / inputEventIntervalSamples));
            log.AppendLine();
            log.AppendLine("[Recent rates]");
            foreach (var rate in latencyRateHistory)
            {
                log.AppendLine(string.Format("{0:0.0} Hz", rate));
            }

            return log.ToString();
        }

        private string BuildStickExportText()
        {
            var log = new StringBuilder();
            log.AppendLine("Gamepad Tester stick diagnostics");
            log.AppendLine(string.Format("Generated: {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
            log.AppendLine(string.Format("Controller: {0}", State.ControllerName));
            log.AppendLine(string.Format("Device: {0}", DeviceModelLabel));
            log.AppendLine(string.Format("Backend: {0}", BackendLabel));
            log.AppendLine();
            log.AppendLine("[Left stick]");
            log.AppendLine(LeftStickVector);
            log.AppendLine(LeftStickDriftStatus);
            log.AppendLine(LeftStickAngleLabel);
            log.AppendLine(LeftStickCurrentMagnitudeLabel);
            log.AppendLine(LeftStickMaxReachLabel);
            log.AppendLine(LeftStickAxisRangeLabel);
            log.AppendLine(LeftStickAverageMagnitudeLabel);
            log.AppendLine(LeftStickCircularCoverageLabel);
            log.AppendLine(LeftStickPathSampleLabel);
            log.AppendLine(LeftRangeQualityLabel);
            log.AppendLine();
            log.AppendLine("[Right stick]");
            log.AppendLine(RightStickVector);
            log.AppendLine(RightStickDriftStatus);
            log.AppendLine(RightStickAngleLabel);
            log.AppendLine(RightStickCurrentMagnitudeLabel);
            log.AppendLine(RightStickMaxReachLabel);
            log.AppendLine(RightStickAxisRangeLabel);
            log.AppendLine(RightStickAverageMagnitudeLabel);
            log.AppendLine(RightStickCircularCoverageLabel);
            log.AppendLine(RightStickPathSampleLabel);
            log.AppendLine(RightRangeQualityLabel);
            log.AppendLine();
            log.AppendLine("[Calibration]");
            log.AppendLine(CalibrationStatusLabel);
            log.AppendLine(LeftCalibrationCenterLabel);
            log.AppendLine(RightCalibrationCenterLabel);
            log.AppendLine(LeftRecommendedDeadzoneLabel);
            log.AppendLine(RightRecommendedDeadzoneLabel);

            return log.ToString();
        }

        private string BuildInputLogText()
        {
            var log = new StringBuilder();
            log.AppendLine("Gamepad Tester input log");
            log.AppendLine(string.Format("Generated: {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
            log.AppendLine(string.Format("Controller: {0}", State.ControllerName));
            log.AppendLine(string.Format("Device: {0}", DeviceModelLabel));
            log.AppendLine(string.Format("Backend: {0}", BackendLabel));
            log.AppendLine();
            log.AppendLine("Timestamp\tInput\tState");

            for (var i = InputHistory.Count - 1; i >= 0; i--)
            {
                var item = InputHistory[i];
                log.AppendLine(string.Format("{0:yyyy-MM-dd HH:mm:ss.fff}\t{1}\t{2}", item.Timestamp, item.InputName, item.State));
            }

            return log.ToString();
        }

        private string BuildReportText()
        {
            var report = new StringBuilder();
            report.AppendLine("Gamepad Tester report");
            report.AppendLine(string.Format("Generated: {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
            report.AppendLine();

            report.AppendLine("[Device]");
            report.AppendLine(string.Format("Name: {0}", State.ControllerName));
            report.AppendLine(string.Format("Display name: {0}", DeviceModelLabel));
            report.AppendLine(string.Format("VID/PID: {0}", string.IsNullOrWhiteSpace(DeviceIdLabel) ? "Unknown" : DeviceIdLabel));
            report.AppendLine(string.Format("Layout: {0}", State.Layout));
            report.AppendLine(string.Format("8BitDo model: {0}", State.EightBitDoModel));
            report.AppendLine();

            report.AppendLine("[Summary]");
            report.AppendLine(string.Format("Health score: {0}%", HealthScore));
            report.AppendLine(string.Format("Health label: {0}", HealthLabel));
            report.AppendLine(HealthSummaryLabel);
            report.AppendLine(HealthDriftFactorLabel);
            report.AppendLine(HealthRangeFactorLabel);
            report.AppendLine(HealthCoverageFactorLabel);
            report.AppendLine();

            report.AppendLine("[Current input]");
            report.AppendLine(string.Format("Active buttons: {0}", ActiveButtonCount));
            report.AppendLine(string.Format("Left stick: {0} | {1}", LeftStickVector, LeftStickDriftStatus));
            report.AppendLine(string.Format("Right stick: {0} | {1}", RightStickVector, RightStickDriftStatus));
            report.AppendLine(string.Format("Left trigger: {0}%", LeftTriggerPercent));
            report.AppendLine(string.Format("Right trigger: {0}%", RightTriggerPercent));
            report.AppendLine();

            report.AppendLine("[Session checks]");
            report.AppendLine(string.Format("Progress: {0}%", QuickTestProgress));
            report.AppendLine(ButtonCoverageLabel);
            report.AppendLine(AnalogCoverageLabel);
            report.AppendLine(string.Format("Missing: {0}", QuickTestMissingLabel));
            report.AppendLine();

            report.AppendLine("[Sticks]");
            report.AppendLine(string.Format("Left circular coverage: {0}", LeftStickCircularCoverageLabel));
            report.AppendLine(string.Format("Left max reach: {0}", LeftStickMaxReachLabel));
            report.AppendLine(string.Format("Left range: {0}", LeftStickAxisRangeLabel));
            report.AppendLine(string.Format("Right circular coverage: {0}", RightStickCircularCoverageLabel));
            report.AppendLine(string.Format("Right max reach: {0}", RightStickMaxReachLabel));
            report.AppendLine(string.Format("Right range: {0}", RightStickAxisRangeLabel));
            report.AppendLine();

            report.AppendLine("[Calibration]");
            report.AppendLine(CalibrationStatusLabel);
            report.AppendLine(string.Format("Left center: {0}", LeftCalibrationCenterLabel));
            report.AppendLine(string.Format("Left deadzone: {0}", LeftRecommendedDeadzoneLabel));
            report.AppendLine(string.Format("Right center: {0}", RightCalibrationCenterLabel));
            report.AppendLine(string.Format("Right deadzone: {0}", RightRecommendedDeadzoneLabel));
            report.AppendLine();

            report.AppendLine("[Latency]");
            report.AppendLine(string.Format("Manual latency: {0}", LatencyResultLabel));
            report.AppendLine(LatencyStatsLabel);
            report.AppendLine(PollingLatencyAverageLabel);
            report.AppendLine(InputEventLatencyAverageLabel);

            return report.ToString();
        }

        private void UpdateCenterCalibration(GamepadState nextState)
        {
            if (!isCenterCalibrationRunning)
            {
                return;
            }

            centerCalibrationSamples++;
            leftCenterXSum += nextState.LeftStick.X;
            leftCenterYSum += nextState.LeftStick.Y;
            rightCenterXSum += nextState.RightStick.X;
            rightCenterYSum += nextState.RightStick.Y;
            leftCenterMaxNoise = Math.Max(leftCenterMaxNoise, nextState.LeftStick.Magnitude);
            rightCenterMaxNoise = Math.Max(rightCenterMaxNoise, nextState.RightStick.Magnitude);

            if (DateTime.UtcNow < centerCalibrationEndsAt)
            {
                return;
            }

            isCenterCalibrationRunning = false;
            calibratedLeftCenterX = leftCenterXSum / Math.Max(1, centerCalibrationSamples);
            calibratedLeftCenterY = leftCenterYSum / Math.Max(1, centerCalibrationSamples);
            calibratedRightCenterX = rightCenterXSum / Math.Max(1, centerCalibrationSamples);
            calibratedRightCenterY = rightCenterYSum / Math.Max(1, centerCalibrationSamples);
            calibratedLeftCenterNoise = leftCenterMaxNoise;
            calibratedRightCenterNoise = rightCenterMaxNoise;
            startCenterCalibrationCommand.RaiseCanExecuteChanged();
        }

        private void UpdateLatency(GamepadState nextState)
        {
            if (!nextState.IsConnected)
            {
                lastStateSampleAt = null;
                return;
            }

            var now = DateTime.UtcNow;
            if (lastStateSampleAt.HasValue)
            {
                currentPollingIntervalMs = (now - lastStateSampleAt.Value).TotalMilliseconds;
                if (currentPollingIntervalMs > 0d && currentPollingIntervalMs < 1000d)
                {
                    pollingIntervalSamples++;
                    pollingIntervalSumMs += currentPollingIntervalMs;
                    pollingIntervalMinMs = Math.Min(pollingIntervalMinMs, currentPollingIntervalMs);
                    pollingIntervalMaxMs = Math.Max(pollingIntervalMaxMs, currentPollingIntervalMs);
                }
            }

            lastStateSampleAt = now;
        }

        private static string GetHzLabel(double intervalMs)
        {
            if (intervalMs <= 0d || intervalMs >= 10000d || double.IsNaN(intervalMs) || double.IsInfinity(intervalMs))
            {
                return "- Hz";
            }

            var hz = 1000d / intervalMs;
            return hz < 10d
                ? string.Format("{0:0.0} Hz", hz)
                : string.Format("{0:0} Hz", hz);
        }

        private void TrackInputEventLatency()
        {
            var now = DateTime.UtcNow;
            if (lastInputEventAt.HasValue)
            {
                var interval = (now - lastInputEventAt.Value).TotalMilliseconds;
                if (interval > 0d && interval < 10000d)
                {
                    inputEventIntervalSamples++;
                    inputEventIntervalSumMs += interval;
                    inputEventIntervalMinMs = Math.Min(inputEventIntervalMinMs, interval);
                    inputEventIntervalMaxMs = Math.Max(inputEventIntervalMaxMs, interval);
                    latencyStatusLabel = string.Format(L("LOCGT_LastInputObservedFormat", "Last input observed after {0:0.0} ms."), currentPollingIntervalMs);
                }
            }
            else
            {
                latencyStatusLabel = L("LOCGT_FirstInputObserved", "First input observed. Press repeatedly for an average.");
            }

            lastInputEventAt = now;
        }

        private void TrackLatencyTest(bool isPressed)
        {
            if (!isLatencyTestRunning || !isPressed)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (!lastInputEventAt.HasValue)
            {
                lastInputEventAt = now;
                latencyStatusLabel = L("LOCGT_FirstInputObserved", "First input observed. Press repeatedly for an average.");
                OnPropertyChanged("LatencyStatusLabel");
                return;
            }

            lastLatencyMs = Math.Max(0d, (now - lastInputEventAt.Value).TotalMilliseconds);
            currentInputEventIntervalMs = lastLatencyMs;
            lastInputEventAt = now;
            if (lastLatencyMs <= 0d || lastLatencyMs >= 10000d)
            {
                return;
            }

            latencyTestSamples++;
            latencyTestSumMs += lastLatencyMs;
            bestLatencyMs = latencyTestSamples == 1 ? lastLatencyMs : Math.Min(bestLatencyMs, lastLatencyMs);
            inputEventIntervalSamples++;
            inputEventIntervalSumMs += lastLatencyMs;
            inputEventIntervalMinMs = Math.Min(inputEventIntervalMinMs, lastLatencyMs);
            inputEventIntervalMaxMs = Math.Max(inputEventIntervalMaxMs, lastLatencyMs);

            latencyRateHistory.Enqueue(1000d / lastLatencyMs);
            while (latencyRateHistory.Count > LatencyGraphMaxSamples)
            {
                latencyRateHistory.Dequeue();
            }

            latencyStatusLabel = string.Format(L("LOCGT_LatencyCapturedFormat", "Captured {0:0} ms."), lastLatencyMs);
            OnPropertyChanged("LatencyStatusLabel");
            OnPropertyChanged("StartLatencyButtonLabel");
            OnPropertyChanged("LatencyResultLabel");
            OnPropertyChanged("LatencyStatsLabel");
            OnPropertyChanged("PollingLatencyAverageLabel");
            OnPropertyChanged("InputEventLatencyAverageLabel");
            OnPropertyChanged("PollingRateCurrentLabel");
            OnPropertyChanged("PollingRateAverageValueLabel");
            OnPropertyChanged("PollingRateMaxValueLabel");
            OnPropertyChanged("PollingJitterLabel");
            OnPropertyChanged("EstimatedDelayLabel");
            OnPropertyChanged("LatencyRateGraphPoints");
            OnPropertyChanged("LatencySampleCountLabel");
            OnPropertyChanged("LatencyRangeLabel");
            OnPropertyChanged("LatencyTestDurationLabel");
            startLatencyTestCommand.RaiseCanExecuteChanged();
            resetLatencyCommand.RaiseCanExecuteChanged();
            exportLatencyCommand.RaiseCanExecuteChanged();
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

        private void UpdateGuidedTestProgress(GamepadState nextState)
        {
            if (!isGuidedTestRunning || GuidedTestInputs == null || guidedTestStepIndex >= GuidedTestInputs.Count)
            {
                return;
            }

            var currentKey = GuidedTestInputs[guidedTestStepIndex].Key;
            if (!IsGuidedInputActive(currentKey, nextState))
            {
                return;
            }

            guidedTestStepIndex++;
            RefreshGuidedTestInputs();
            OnPropertyChanged("GuidedTestProgress");
            OnPropertyChanged("GuidedTestStatusLabel");
            OnPropertyChanged("GuidedTestNextInputLabel");
            OnPropertyChanged("GuidedTestActionLabel");
        }

        private bool IsGuidedInputActive(string key, GamepadState nextState)
        {
            switch (key)
            {
                case "South":
                    return nextState.Buttons.South;
                case "East":
                    return nextState.Buttons.East;
                case "West":
                    return nextState.Buttons.West;
                case "North":
                    return nextState.Buttons.North;
                case "LeftShoulder":
                    return nextState.Buttons.LeftShoulder;
                case "RightShoulder":
                    return nextState.Buttons.RightShoulder;
                case "LeftTrigger":
                    return nextState.LeftTrigger >= TriggerFullPressThreshold;
                case "RightTrigger":
                    return nextState.RightTrigger >= TriggerFullPressThreshold;
                case "LeftStick":
                    return nextState.Buttons.LeftStick;
                case "RightStick":
                    return nextState.Buttons.RightStick;
                case "Back":
                    return nextState.Buttons.Back;
                case "Start":
                    return nextState.Buttons.Start;
                case "Guide":
                    return nextState.Buttons.Guide;
                case "DpadUp":
                    return nextState.Buttons.DpadUp;
                case "DpadDown":
                    return nextState.Buttons.DpadDown;
                case "DpadLeft":
                    return nextState.Buttons.DpadLeft;
                case "DpadRight":
                    return nextState.Buttons.DpadRight;
                case "LeftStickRange":
                    return nextState.LeftStick.Magnitude >= StickEdgeThreshold;
                case "RightStickRange":
                    return nextState.RightStick.Magnitude >= StickEdgeThreshold;
                default:
                    return false;
            }
        }

        private static void AddMissingButton(ICollection<string> missing, bool isCovered, string label)
        {
            if (!isCovered)
            {
                missing.Add(label);
            }
        }

        private List<string> GetMissingInputLabels()
        {
            var missing = new List<string>();
            AddMissingButton(missing, coveredButtons.South, SouthLabel);
            AddMissingButton(missing, coveredButtons.East, EastLabel);
            AddMissingButton(missing, coveredButtons.West, WestLabel);
            AddMissingButton(missing, coveredButtons.North, NorthLabel);
            AddMissingButton(missing, coveredButtons.LeftShoulder, LeftShoulderLabel);
            AddMissingButton(missing, coveredButtons.RightShoulder, RightShoulderLabel);
            AddMissingButton(missing, coveredButtons.LeftStick, LeftStickButtonLabel);
            AddMissingButton(missing, coveredButtons.RightStick, RightStickButtonLabel);
            AddMissingButton(missing, coveredButtons.Back, BackButtonLabel);
            AddMissingButton(missing, coveredButtons.Start, StartButtonLabel);
            AddMissingButton(missing, coveredButtons.Guide, GuideButtonLabel);
            AddMissingButton(missing, coveredButtons.DpadUp, DpadUpLabel);
            AddMissingButton(missing, coveredButtons.DpadDown, DpadDownLabel);
            AddMissingButton(missing, coveredButtons.DpadLeft, DpadLeftLabel);
            AddMissingButton(missing, coveredButtons.DpadRight, DpadRightLabel);

            if (maxLeftTrigger < TriggerFullPressThreshold)
            {
                missing.Add(LeftTriggerLabel + " 100%");
            }

            if (maxRightTrigger < TriggerFullPressThreshold)
            {
                missing.Add(RightTriggerLabel + " 100%");
            }

            if (maxLeftStickMagnitude < StickEdgeThreshold)
            {
                missing.Add("LS edge");
            }

            if (maxRightStickMagnitude < StickEdgeThreshold)
            {
                missing.Add("RS edge");
            }

            return missing;
        }

        private void RefreshGuidedTestInputs()
        {
            if (GuidedTestInputs == null)
            {
                return;
            }

            var currentKey = GetCurrentGuidedInputKey();
            for (var index = 0; index < GuidedTestInputs.Count; index++)
            {
                var item = GuidedTestInputs[index];
                item.Label = GetGuidedInputLabel(item.Key);
                item.IsCovered = index < guidedTestStepIndex;
                item.IsCurrent = State.IsConnected && item.Key == currentKey;
            }
        }

        private string GetCurrentGuidedInputKey()
        {
            if (GuidedTestInputs == null || guidedTestStepIndex < 0 || guidedTestStepIndex >= GuidedTestInputs.Count)
            {
                return null;
            }

            return GuidedTestInputs[guidedTestStepIndex].Key;
        }

        private string GetCurrentGuidedInputLabel()
        {
            var key = GetCurrentGuidedInputKey();
            return key == null ? null : GetGuidedInputLabel(key);
        }

        private string GetGuidedInputLabel(string key)
        {
            switch (key)
            {
                case "South":
                    return SouthLabel;
                case "East":
                    return EastLabel;
                case "West":
                    return WestLabel;
                case "North":
                    return NorthLabel;
                case "LeftShoulder":
                    return LeftShoulderLabel;
                case "RightShoulder":
                    return RightShoulderLabel;
                case "LeftTrigger":
                    return LeftTriggerLabel + " 100%";
                case "RightTrigger":
                    return RightTriggerLabel + " 100%";
                case "LeftStick":
                    return LeftStickButtonLabel;
                case "RightStick":
                    return RightStickButtonLabel;
                case "Back":
                    return BackButtonLabel;
                case "Start":
                    return StartButtonLabel;
                case "Guide":
                    return GuideButtonLabel;
                case "DpadUp":
                    return DpadUpLabel;
                case "DpadDown":
                    return DpadDownLabel;
                case "DpadLeft":
                    return DpadLeftLabel;
                case "DpadRight":
                    return DpadRightLabel;
                case "LeftStickRange":
                    return "LS edge";
                case "RightStickRange":
                    return "RS edge";
                default:
                    return key;
            }
        }

        private bool IsGuidedInputCovered(string key)
        {
            switch (key)
            {
                case "South":
                    return coveredButtons.South;
                case "East":
                    return coveredButtons.East;
                case "West":
                    return coveredButtons.West;
                case "North":
                    return coveredButtons.North;
                case "LeftShoulder":
                    return coveredButtons.LeftShoulder;
                case "RightShoulder":
                    return coveredButtons.RightShoulder;
                case "LeftTrigger":
                    return maxLeftTrigger >= TriggerFullPressThreshold;
                case "RightTrigger":
                    return maxRightTrigger >= TriggerFullPressThreshold;
                case "LeftStick":
                    return coveredButtons.LeftStick;
                case "RightStick":
                    return coveredButtons.RightStick;
                case "Back":
                    return coveredButtons.Back;
                case "Start":
                    return coveredButtons.Start;
                case "Guide":
                    return coveredButtons.Guide;
                case "DpadUp":
                    return coveredButtons.DpadUp;
                case "DpadDown":
                    return coveredButtons.DpadDown;
                case "DpadLeft":
                    return coveredButtons.DpadLeft;
                case "DpadRight":
                    return coveredButtons.DpadRight;
                case "LeftStickRange":
                    return maxLeftStickMagnitude >= StickEdgeThreshold;
                case "RightStickRange":
                    return maxRightStickMagnitude >= StickEdgeThreshold;
                default:
                    return false;
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
                Touchpad = buttons.Touchpad,
                LeftStick = buttons.LeftStick,
                RightStick = buttons.RightStick,
                DpadUp = buttons.DpadUp,
                DpadDown = buttons.DpadDown,
                DpadLeft = buttons.DpadLeft,
                DpadRight = buttons.DpadRight
            };
        }

        private static List<ExtraButtonState> CopyExtraButtons(IList<ExtraButtonState> buttons)
        {
            var copy = new List<ExtraButtonState>();
            if (buttons == null)
            {
                return copy;
            }

            for (var index = 0; index < buttons.Count; index++)
            {
                copy.Add(new ExtraButtonState
                {
                    RawIndex = buttons[index].RawIndex,
                    Label = buttons[index].Label,
                    IsPressed = buttons[index].IsPressed
                });
            }

            return copy;
        }

        private void TrackRestDrift(GamepadState nextState)
        {
            if (!IsRestDriftCandidate(nextState))
            {
                ResetRestDriftCandidate();
                return;
            }

            var now = DateTime.UtcNow;
            if (!restCandidateStartedAt.HasValue)
            {
                StartRestDriftCandidate(nextState, now);
                return;
            }

            var moved = Math.Abs(nextState.LeftStick.X - lastRestCandidateLeftX) > RestStabilityDelta ||
                        Math.Abs(nextState.LeftStick.Y - lastRestCandidateLeftY) > RestStabilityDelta ||
                        Math.Abs(nextState.RightStick.X - lastRestCandidateRightX) > RestStabilityDelta ||
                        Math.Abs(nextState.RightStick.Y - lastRestCandidateRightY) > RestStabilityDelta;

            if (moved)
            {
                StartRestDriftCandidate(nextState, now);
                return;
            }

            StoreRestCandidatePosition(nextState);

            if ((now - restCandidateStartedAt.Value).TotalMilliseconds < RestObservationMilliseconds)
            {
                return;
            }

            maxLeftRestDrift = Math.Max(maxLeftRestDrift, nextState.LeftStick.Magnitude);
            maxRightRestDrift = Math.Max(maxRightRestDrift, nextState.RightStick.Magnitude);
        }

        private static bool IsRestDriftCandidate(GamepadState state)
        {
            return CountPressedButtons(state.Buttons) == 0 &&
                   state.LeftTrigger < 0.02f &&
                   state.RightTrigger < 0.02f &&
                   state.LeftStick.Magnitude < MaximumRestDriftCandidate &&
                   state.RightStick.Magnitude < MaximumRestDriftCandidate;
        }

        private void StartRestDriftCandidate(GamepadState state, DateTime now)
        {
            restCandidateStartedAt = now;
            StoreRestCandidatePosition(state);
        }

        private void StoreRestCandidatePosition(GamepadState state)
        {
            lastRestCandidateLeftX = state.LeftStick.X;
            lastRestCandidateLeftY = state.LeftStick.Y;
            lastRestCandidateRightX = state.RightStick.X;
            lastRestCandidateRightY = state.RightStick.Y;
        }

        private void ResetRestDriftCandidate()
        {
            restCandidateStartedAt = null;
            lastRestCandidateLeftX = 0d;
            lastRestCandidateLeftY = 0d;
            lastRestCandidateRightX = 0d;
            lastRestCandidateRightY = 0d;
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

        private static int CountNormalizedControls()
        {
            return 19;
        }

        private static int CountPressedExtraButtons(IList<ExtraButtonState> buttons)
        {
            if (buttons == null)
            {
                return 0;
            }

            var count = 0;
            for (var index = 0; index < buttons.Count; index++)
            {
                if (buttons[index].IsPressed)
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

        private string GetDriftStatus(double magnitude)
        {
            if (magnitude < HealthyDeadzoneThreshold)
            {
                return L("LOCGT_NoDrift", "No drift");
            }

            if (magnitude < MinorDriftThreshold)
            {
                return L("LOCGT_DriftSafe", "Safe");
            }

            if (magnitude < AttentionDriftThreshold)
            {
                return L("LOCGT_MinorDrift", "Minor drift");
            }

            return L("LOCGT_MajorDrift", "Major drift");
        }

        private string GetCircularCoverageLabel(StickDiagnosticsTracker tracker)
        {
            return string.Format(L("LOCGT_CircularCoverageFormat", "Circular coverage: {0}% ({1}/72 sectors)"), tracker.CoveragePercent, tracker.CoveredSectors);
        }

        private string GetPathSampleLabel(StickDiagnosticsTracker tracker)
        {
            return string.Format(L("LOCGT_PathSamplesFormat", "Path samples: {0}"), tracker.PathPoints.Count);
        }

        private string GetAxisRangeLabel(StickDiagnosticsTracker tracker)
        {
            if (tracker.SampleCount == 0)
            {
                return L("LOCGT_RangeNoSamples", "Range: no samples");
            }

            return string.Format(L("LOCGT_RangeFormat", "Range X {0:0.00}..{1:0.00}  Y {2:0.00}..{3:0.00}"),
                tracker.MinX,
                tracker.MaxX,
                tracker.MinY,
                tracker.MaxY);
        }

        private string GetAverageMagnitudeLabel(StickDiagnosticsTracker tracker)
        {
            if (tracker.SampleCount == 0)
            {
                return L("LOCGT_AverageZero", "Average: 0%");
            }

            return string.Format(L("LOCGT_AverageFormat", "Average: {0}%"), Math.Min(100, (int)Math.Round(tracker.AverageMagnitude * 100d)));
        }

        private string GetAngleLabel(StickState stick)
        {
            if (stick.Magnitude < 0.05d)
            {
                return L("LOCGT_AngleCenter", "Angle: center");
            }

            var angle = Math.Atan2(stick.Y, stick.X) * 180d / Math.PI;
            if (angle < 0d)
            {
                angle += 360d;
            }

            return string.Format(L("LOCGT_AngleFormat", "Angle: {0:0} deg"), angle);
        }

        private string GetRecommendedDeadzoneLabel(double noise)
        {
            return string.Format(L("LOCGT_RecommendedDeadzoneFormat", "Recommended deadzone: {0}%"), GetRecommendedDeadzonePercent(noise));
        }

        private static int GetRecommendedDeadzonePercent(double noise)
        {
            var recommended = Math.Max(0.04d, Math.Min(0.25d, noise + 0.025d));
            return (int)Math.Round(recommended * 100d);
        }

        private string GetRangeQualityLabel(StickDiagnosticsTracker tracker)
        {
            if (tracker.SampleCount == 0)
            {
                return L("LOCGT_RangeNotMeasured", "Range not measured yet.");
            }

            return string.Format(L("LOCGT_RangeQualityFormat", "Outer range quality: {0}%"), GetRangeQualityPercent(tracker));
        }

        private static int GetRangeQualityPercent(StickDiagnosticsTracker tracker)
        {
            return Math.Max(0, Math.Min(100, (int)Math.Round(tracker.MaxMagnitude * 100d)));
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

        private string L(string key, string fallback)
        {
            if (localizer == null)
            {
                return fallback;
            }

            var value = localizer(key);
            return string.IsNullOrWhiteSpace(value) || value == key ? fallback : value;
        }

        private void NotifyStateChanged()
        {
            OnPropertyChanged("State");
            OnPropertyChanged("LeftStickDotX");
            OnPropertyChanged("LeftStickDotY");
            OnPropertyChanged("RightStickDotX");
            OnPropertyChanged("RightStickDotY");
            OnPropertyChanged("CompactLeftStickDotX");
            OnPropertyChanged("CompactLeftStickDotY");
            OnPropertyChanged("CompactRightStickDotX");
            OnPropertyChanged("CompactRightStickDotY");
            OnPropertyChanged("LeftStickDiagnosticsDotX");
            OnPropertyChanged("LeftStickDiagnosticsDotY");
            OnPropertyChanged("RightStickDiagnosticsDotX");
            OnPropertyChanged("RightStickDiagnosticsDotY");
            OnPropertyChanged("LeftTriggerPercent");
            OnPropertyChanged("RightTriggerPercent");
            OnPropertyChanged("IsLeftTriggerActive");
            OnPropertyChanged("IsRightTriggerActive");
            OnPropertyChanged("LeftStickDriftPercent");
            OnPropertyChanged("RightStickDriftPercent");
            OnPropertyChanged("IsDpadActive");
            OnPropertyChanged("ActiveButtonCount");
            OnPropertyChanged("ExtraActiveButtonCount");
            OnPropertyChanged("HasExtraButtons");
            OnPropertyChanged("IsFavoriteButtonActive");
            OnPropertyChanged("ExtraButtonSummaryLabel");
            OnPropertyChanged("LeftStickVector");
            OnPropertyChanged("RightStickVector");
            OnPropertyChanged("LeftStickDriftStatus");
            OnPropertyChanged("RightStickDriftStatus");
            OnPropertyChanged("MaxDriftLabel");
            OnPropertyChanged("SessionRestDriftLabel");
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
            OnPropertyChanged("CalibrationStatusLabel");
            OnPropertyChanged("CalibrationProgress");
            OnPropertyChanged("LeftCalibrationCenterLabel");
            OnPropertyChanged("RightCalibrationCenterLabel");
            OnPropertyChanged("LeftRecommendedDeadzoneLabel");
            OnPropertyChanged("RightRecommendedDeadzoneLabel");
            OnPropertyChanged("LeftRecommendedDeadzonePercent");
            OnPropertyChanged("RightRecommendedDeadzonePercent");
            OnPropertyChanged("LeftRangeQualityLabel");
            OnPropertyChanged("RightRangeQualityLabel");
            OnPropertyChanged("LeftRangeQualityPercent");
            OnPropertyChanged("RightRangeQualityPercent");
            OnPropertyChanged("LatencyStatusLabel");
            OnPropertyChanged("StartLatencyButtonLabel");
            OnPropertyChanged("LatencyResultLabel");
            OnPropertyChanged("LatencyStatsLabel");
            OnPropertyChanged("PollingLatencyAverageLabel");
            OnPropertyChanged("InputEventLatencyAverageLabel");
            OnPropertyChanged("PollingRateCurrentLabel");
            OnPropertyChanged("PollingRateAverageValueLabel");
            OnPropertyChanged("PollingRateMaxValueLabel");
            OnPropertyChanged("PollingJitterLabel");
            OnPropertyChanged("EstimatedDelayLabel");
            OnPropertyChanged("LatencySampleCountLabel");
            OnPropertyChanged("LatencyRangeLabel");
            OnPropertyChanged("LatencyTestDurationLabel");
            OnPropertyChanged("QuickTestProgress");
            OnPropertyChanged("QuickTestLabel");
            OnPropertyChanged("ButtonCoverageLabel");
            OnPropertyChanged("AnalogCoverageLabel");
            OnPropertyChanged("QuickTestMissingLabel");
            OnPropertyChanged("GuidedTestProgress");
            OnPropertyChanged("GuidedTestButtonLabel");
            OnPropertyChanged("GuidedTestStatusLabel");
            OnPropertyChanged("GuidedTestNextInputLabel");
            OnPropertyChanged("GuidedTestActionLabel");
            RefreshGuidedTestInputs();
            OnPropertyChanged("CoveredSouth");
            OnPropertyChanged("CoveredEast");
            OnPropertyChanged("CoveredWest");
            OnPropertyChanged("CoveredNorth");
            OnPropertyChanged("CoveredLeftShoulder");
            OnPropertyChanged("CoveredRightShoulder");
            OnPropertyChanged("CoveredLeftStickButton");
            OnPropertyChanged("CoveredRightStickButton");
            OnPropertyChanged("CoveredBack");
            OnPropertyChanged("CoveredStart");
            OnPropertyChanged("CoveredGuide");
            OnPropertyChanged("CoveredDpadUp");
            OnPropertyChanged("CoveredDpadDown");
            OnPropertyChanged("CoveredDpadLeft");
            OnPropertyChanged("CoveredDpadRight");
            OnPropertyChanged("CoveredLeftTrigger");
            OnPropertyChanged("CoveredRightTrigger");
            OnPropertyChanged("CoveredLeftStickRange");
            OnPropertyChanged("CoveredRightStickRange");
            OnPropertyChanged("HealthScore");
            OnPropertyChanged("HealthLabel");
            OnPropertyChanged("HealthSummaryLabel");
            OnPropertyChanged("HealthDriftFactorLabel");
            OnPropertyChanged("HealthRangeFactorLabel");
            OnPropertyChanged("HealthCoverageFactorLabel");
            OnPropertyChanged("ControllerSummary");
            OnPropertyChanged("HasController");
            OnPropertyChanged("IsNoControllerVisible");
            OnPropertyChanged("DeviceIdLabel");
            OnPropertyChanged("DeviceModelLabel");
            OnPropertyChanged("DeviceCapabilitiesLabel");
            OnPropertyChanged("DeviceApiLabel");
            OnPropertyChanged("DeviceRumbleCapabilityLabel");
            OnPropertyChanged("ExtraButtonDetailLabel");
            OnPropertyChanged("BackendLabel");
            OnPropertyChanged("MappingStatusLabel");
            OnPropertyChanged("RumbleStatusLabel");
            OnPropertyChanged("SouthLabel");
            OnPropertyChanged("EastLabel");
            OnPropertyChanged("WestLabel");
            OnPropertyChanged("NorthLabel");
            OnPropertyChanged("LeftShoulderLabel");
            OnPropertyChanged("RightShoulderLabel");
            OnPropertyChanged("LeftTriggerLabel");
            OnPropertyChanged("RightTriggerLabel");
            OnPropertyChanged("LeftStickButtonLabel");
            OnPropertyChanged("RightStickButtonLabel");
            OnPropertyChanged("BackButtonLabel");
            OnPropertyChanged("StartButtonLabel");
            OnPropertyChanged("GuideButtonLabel");
            OnPropertyChanged("DpadUpLabel");
            OnPropertyChanged("DpadDownLabel");
            OnPropertyChanged("DpadLeftLabel");
            OnPropertyChanged("DpadRightLabel");
            OnPropertyChanged("IsEightBitDoLayout");
            OnPropertyChanged("IsEightBitDoPro3Artwork");
            OnPropertyChanged("IsEightBitDoUltimate2CArtwork");
            OnPropertyChanged("IsEightBitDoUltimate2Artwork");
            OnPropertyChanged("IsSwitchProLayout");
            OnPropertyChanged("IsXboxLayout");
            OnPropertyChanged("IsPlayStationLayout");
            OnPropertyChanged("IsDualSenseLayout");
            OnPropertyChanged("IsXboxVisualScheme");
            OnPropertyChanged("IsXboxOneVisualScheme");
            OnPropertyChanged("IsXboxSeriesVisualScheme");
            OnPropertyChanged("IsSteamControllerVisualScheme");
            OnPropertyChanged("IsPlayStationVisualScheme");
            OnPropertyChanged("IsSwitchProVisualScheme");
            OnPropertyChanged("IsEightBitDoUltimateVisualScheme");
            OnPropertyChanged("IsEightBitDoUltimate2VisualScheme");
            OnPropertyChanged("IsEightBitDoProVisualScheme");
            OnPropertyChanged("IsUniversalControllerArtwork");
            OnPropertyChanged("IsGenericLayout");
            openGuidedTestCommand.RaiseCanExecuteChanged();
            startGuidedTestCommand.RaiseCanExecuteChanged();
        }

        public void Dispose()
        {
            ClearInputHistory();
            pollingService.StateUpdated -= OnStateUpdated;
            pollingService.Dispose();
        }
    }
}
