# Playnite NX Gamepad Tester

Playnite NX Gamepad Tester is a Playnite extension for testing controllers directly from Playnite Desktop mode.

It is designed for couch, TV, handheld-PC, and console-like setups where users want to verify gamepad buttons, sticks, triggers, rumble, drift, latency, and device metadata without leaving Playnite.

## Features

- SDL GameController backend using Playnite's bundled SDL runtime where available.
- Normalized stick values (`-1..1`) and trigger values (`0..1`).
- Xbox naming convention for normalized controls: `LS`, `RS`, `LB`, `RB`, `LT`, `RT`, `A`, `B`, `X`, `Y`.
- Support for Xbox/XInput, PlayStation, Nintendo Switch Pro, 8BitDo, and generic SDL-compatible controllers.
- Multi-controller selector when more than one controller is connected.
- Manual visual scheme override for Universal, Xbox, PlayStation, DualSense, Switch Pro, 8BitDo Ultimate 2, and 8BitDo Pro layouts.
- Live controller map with button, shoulder, trigger, stick, and D-pad feedback.
- Guided test pass that asks for the next missing normalized input.
- Stick diagnostics with paths, circular coverage, max reach, range quality, center capture, and recommended deadzone.
- Health estimate based on resting drift only, with configurable thresholds.
- Latency panel for observed polling/input timing.
- Input log with opt-in recording and export.
- Rumble tests integrated into the main test dashboard.
- Device information with controller name, display name, VID/PID, layout, backend, and SDL mapping status.
- Optional Playnite Desktop sidebar entry.
- Localizable UI through Playnite resource dictionaries.

## Requirements

- Windows.
- Playnite 10.x.
- Playnite SDK compatible runtime.
- A controller exposed to SDL as a GameController.

8BitDo controllers can expose themselves through XInput or DInput depending on the hardware mode. Gamepad Tester reads through SDL GameController normalization, so both paths are supported when SDL can map the device. XInput mode is usually the most consistent first option; DInput can still work if SDL has a compatible mapping for that mode.

## Installation

1. Download the latest `.pext` file from the [GitHub releases page](https://github.com/Naerian/playnite-nx-gamepad-tester/releases).
2. Open the `.pext` file, or drag it into Playnite.
3. Restart Playnite if Playnite asks you to do so.

After installation, open **Extensions > Gamepad Tester**.

## Usage

Connect one or more controllers before opening the tester. If multiple controllers are available, the selector near the tabs lets you choose the active device by name.

If no controller is detected, the extension hides the tester UI and shows a clear warning. For 8BitDo devices, switch input mode if the current mode is not detected.

## Tabs

### Test

The main dashboard shows the selected controller map, device summary, health, guided test, current inputs, stick snapshots, trigger percentages, and rumble buttons.

Use **Start guided test** to clear the current session and press controls in the order requested by the tester. The pass completes when all normalized buttons, triggers, and stick edge checks have been seen.

### Sticks & Calibration

Use this tab for deeper analog diagnostics:

- Stick movement paths.
- Circular coverage around the outer edge.
- Current magnitude and angle.
- Center capture for deadzone recommendation.
- Range quality checks for repaired or replaced sticks.

These diagnostics are read-only and do not modify Windows or driver calibration.

### Latency

The latency tab estimates the delay observed by the plugin between starting a test and seeing the next controller input. It is not a direct USB hardware latency measurement, but it helps compare wired, Bluetooth, and adapter modes.

### Input Log

The input log is paused by default for performance and readability. Enable it when you need a detailed event trail, then export it if a developer or tester needs the data.

### Device Info

Shows the raw and friendly device identity, VID/PID, detected layout, SDL backend, and mapping status.

## Settings

The extension settings include:

- Show or hide the Playnite sidebar item.
- Reset diagnostics when switching controller.
- Show the device selector even with one controller.
- Enable or disable rumble tests.
- Enable input log by default.
- Configure healthy, minor, and attention drift thresholds.
- Configure stick edge and trigger full-press thresholds for guided checks.
- Configure center calibration duration.

Threshold values are normalized. Sticks use `0..1` magnitude, and triggers use `0..1` pressure.

## Building

Run:

```powershell
.\BuildGamepadTester.ps1
```

The build script compiles the extension and deploys it to:

```text
C:\Playnite\Extensions\GamepadTester
```

To create a local `.pext` package after building:

```powershell
.\PackageGamepadTester.ps1
```

The package is written to `dist\GamepadTester-<version>.pext`.

## Release QA

Before publishing a `.pext`, run through [docs/visual-qa-checklist.md](docs/visual-qa-checklist.md). The checklist covers common window sizes, Windows scaling, controller states, theme contrast, and tab readability.

## Localization

The plugin uses Playnite localization resource dictionaries under `Localization/`.

Translations are stored as locale-specific XAML resource dictionaries. To add or update a translation, copy an existing locale file, rename it to the target locale, and translate the string values while keeping the same resource keys.

Community translation contributions are welcome.

## Support

If you find this project useful and want to support its development, consider buying me a coffee!

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/naerian)
