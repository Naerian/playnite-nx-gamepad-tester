using GamepadTester.Models;
using GamepadTester.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace GamepadTester.Tests
{
    internal static class Program
    {
        private static int executed;

        private static int Main()
        {
            try
            {
                TestControllerIdentification();
                TestDiagnosticConfidence();
                TestRestDriftTracking();
                TestStickDiagnostics();
                TestSimulatedProvider();
                TestVisualSchemeCatalog();
                TestCompatibilityReport();
                TestLocalizationParity();
                Console.WriteLine("GamepadTester.Tests: {0} checks passed.", executed);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("GamepadTester.Tests failed: {0}", ex.Message);
                return 1;
            }
        }

        private static void TestControllerIdentification()
        {
            Equal(GamepadLayout.EightBitDo, ControllerIdentificationService.DetectLayout("Xbox Controller", 0x2DC8, 0x310B), "8BitDo VID wins over XInput name");
            Equal(GamepadLayout.SwitchPro, ControllerIdentificationService.DetectLayout("Pro Controller", 0x057E, 0x2009), "Switch Pro VID/PID");
            Equal(GamepadLayout.PlayStation, ControllerIdentificationService.DetectLayout("DualSense Wireless Controller", 0x054C, 0x0CE6), "DualSense name");
            Equal(GamepadLayout.Xbox, ControllerIdentificationService.DetectLayout("Xbox Wireless Controller", 0x045E, 0x0B13), "Xbox name");
            Equal(GamepadLayout.Generic, ControllerIdentificationService.DetectLayout("Arcade Stick", 0, 0), "Generic fallback");
            Equal(EightBitDoModel.Pro2, ControllerIdentificationService.DetectEightBitDoModel("8BitDo Pro 2", 0x2DC8, 0), "8BitDo Pro 2 model");
            Equal(EightBitDoModel.Ultimate2Wireless, ControllerIdentificationService.DetectEightBitDoModel("8BitDo Ultimate 2", 0x2DC8, 0x310B), "8BitDo Ultimate model");
        }

        private static void TestDiagnosticConfidence()
        {
            Equal(DiagnosticStage.NotEvaluated, DiagnosticConfidenceEvaluator.ForHealth(false, 0).Stage, "Disconnected health is not evaluated");
            Equal(DiagnosticStage.Collecting, DiagnosticConfidenceEvaluator.ForHealth(true, 119).Stage, "Health waits for stable samples");
            Equal(DiagnosticStage.Ready, DiagnosticConfidenceEvaluator.ForHealth(true, 120).Stage, "Health becomes ready at threshold");
            Equal(DiagnosticConfidenceLevel.High, DiagnosticConfidenceEvaluator.ForHealth(true, 300).Level, "Health reaches high confidence");
            Equal(DiagnosticStage.Collecting, DiagnosticConfidenceEvaluator.ForLatency(true, 19).Stage, "Latency waits for samples");
            Equal(DiagnosticStage.Ready, DiagnosticConfidenceEvaluator.ForLatency(true, 20).Stage, "Latency becomes ready");
            Equal(DiagnosticStage.Collecting, DiagnosticConfidenceEvaluator.ForStickRange(120, 35).Stage, "Range requires explored directions");
            Equal(DiagnosticStage.Ready, DiagnosticConfidenceEvaluator.ForStickRange(120, 36).Stage, "Range becomes ready");
        }

        private static void TestStickDiagnostics()
        {
            var tracker = new StickDiagnosticsTracker();
            for (var index = 0; index < 72; index++)
            {
                var angle = Math.PI * 2d * (index + 0.5d) / 72d;
                tracker.AddSample(new StickState
                {
                    X = (float)(Math.Cos(angle) * 0.7d),
                    Y = (float)(Math.Sin(angle) * 0.7d)
                });
            }

            True(tracker.ExploredSectors >= 60, "Circular movement explores enough sectors for high confidence");
            Equal(72, tracker.SampleCount, "Stick samples are counted");
            tracker.Reset();
            Equal(0, tracker.ExploredSectors, "Reset clears explored sectors");
        }

        private static void TestRestDriftTracking()
        {
            var tracker = new RestDriftTracker();
            var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var activeState = new GamepadState
            {
                IsConnected = true,
                LeftStick = new StickState { X = 0.8f, Y = 0f }
            };

            tracker.AddSample(activeState, start);
            tracker.AddSample(activeState, start.AddSeconds(1));
            Equal(0, tracker.SampleCount, "Normal stick movement is excluded from rest drift");
            Equal(0d, tracker.MaxDrift, "Normal movement cannot trigger a drift warning");

            var restingState = new GamepadState
            {
                IsConnected = true,
                LeftStick = new StickState { X = 0.04f, Y = 0f }
            };
            tracker.AddSample(restingState, start.AddSeconds(2));
            tracker.AddSample(restingState, start.AddMilliseconds(2500));
            True(tracker.SampleCount > 0, "Stable centered offset is sampled as rest drift");
            True(tracker.MaxDrift >= 0.039d, "Stable centered offset is measured");

            tracker.Reset();
            Equal(0, tracker.SampleCount, "Rest drift reset clears samples");
            Equal(0d, tracker.MaxDrift, "Rest drift reset clears the peak");

            restingState.ExtraButtons.Add(new ExtraButtonState { RawIndex = 15, IsPressed = true });
            tracker.AddSample(restingState, start.AddSeconds(3));
            tracker.AddSample(restingState, start.AddSeconds(4));
            Equal(0, tracker.SampleCount, "Extra button activity is excluded from rest drift");
        }

        private static void TestSimulatedProvider()
        {
            var provider = new SimulatedGamepadInputProvider();
            provider.AddController(new GamepadControllerInfo { InstanceId = 7, Name = "Simulated Xbox" });
            provider.Enqueue(new GamepadState { IsConnected = true, ControllerName = "Simulated Xbox" });
            provider.SelectController(7);
            Equal(7, provider.SelectedInstanceId, "Simulated controller selection");
            True(provider.ReadState().IsConnected, "Simulated state queue");
            True(provider.TryRumble(1, 1, 1), "Simulated rumble");
            Equal(1, provider.RumbleCallCount, "Simulated rumble is recorded");
        }

        private static void TestVisualSchemeCatalog()
        {
            var definitions = ControllerVisualSchemeCatalog.CreateDefinitions(null).ToList();
            True(definitions.Count >= 8, "Visual scheme catalog is populated");
            Equal(definitions.Count, definitions.Select(item => item.Key).Distinct().Count(), "Visual scheme keys are unique");
            True(definitions.All(item => item.TestWidth > 0 && item.TestHeight > 0), "Visual schemes have valid dimensions");
        }

        private static void TestCompatibilityReport()
        {
            var state = new GamepadState
            {
                IsConnected = true,
                ControllerName = "Simulated Xbox",
                VendorId = 0x045E,
                ProductId = 0x0B13,
                Layout = GamepadLayout.Xbox,
                SdlVersion = "2.30.9",
                SdlGuid = "030000005e040000130b000000000000",
                SdlMapping = "simulated-mapping",
                AxisCount = 6,
                ButtonCount = 15,
                HatCount = 1
            };
            var report = GamepadCompatibilityReportBuilder.Build(state, null, "high", "medium", "medium", "low");
            True(report.Contains("VID: 045E"), "Compatibility report includes VID");
            True(report.Contains("SDL GUID: 030000005e040000130b000000000000"), "Compatibility report includes SDL GUID");
            True(report.Contains("Axes exposed: 6"), "Compatibility report includes capabilities");
            True(!report.Contains(Environment.UserName), "Compatibility report excludes the user name");
            True(!report.Contains("8BitDo model:"), "Non-8BitDo reports omit model-specific fields");

            state.Layout = GamepadLayout.EightBitDo;
            state.EightBitDoModel = EightBitDoModel.Pro2;
            report = GamepadCompatibilityReportBuilder.Build(state, null, "high", "medium", "medium", "low");
            True(report.Contains("8BitDo model: Pro2"), "8BitDo reports include the detected model");
        }

        private static void TestLocalizationParity()
        {
            var localization = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Localization"));
            var englishPath = Path.Combine(localization, "en_US.xaml");
            True(File.Exists(englishPath), "English localization exists");
            var expected = ReadKeys(englishPath);

            foreach (var path in Directory.GetFiles(localization, "*.xaml"))
            {
                var actual = ReadKeys(path);
                var missing = expected.Except(actual).ToArray();
                True(missing.Length == 0, Path.GetFileName(path) + " is missing: " + string.Join(", ", missing));
            }
        }

        private static HashSet<string> ReadKeys(string path)
        {
            XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
            return new HashSet<string>(XDocument.Load(path).Root.Elements()
                .Select(element => (string)element.Attribute(x + "Key"))
                .Where(key => !string.IsNullOrWhiteSpace(key)));
        }

        private static void Equal<T>(T expected, T actual, string name)
        {
            executed++;
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(string.Format("{0}: expected {1}, got {2}", name, expected, actual));
            }
        }

        private static void True(bool value, string name)
        {
            executed++;
            if (!value)
            {
                throw new InvalidOperationException(name);
            }
        }
    }
}
