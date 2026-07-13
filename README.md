# Playnite NX Gamepad Tester

Playnite NX Gamepad Tester is a Playnite extension for testing controllers in Desktop mode, with optional embeddable blocks for Fullscreen themes.

It is designed for couch, TV, handheld-PC, and console-like setups where users want to verify gamepad buttons, sticks, triggers, rumble, drift, latency, and device metadata without leaving Playnite.

## Documentation

- [Documentation (English)](https://github.com/Naerian/playnite-nx-gamepad-tester/wiki/EN-Installation-and-Quick-Start)
- [Documentación (Español)](https://github.com/Naerian/playnite-nx-gamepad-tester/wiki/ES-Instalacion-e-inicio-rapido)
- [Fullscreen theme integration](https://github.com/Naerian/playnite-nx-gamepad-tester/wiki/EN-Fullscreen-Theme-Integration)

## Features

- SDL GameController backend using Playnite's bundled SDL runtime where available.
- Normalized stick values (`-1..1`) and trigger values (`0..1`).
- Xbox naming convention for normalized controls: `LS`, `RS`, `LB`, `RB`, `LT`, `RT`, `A`, `B`, `X`, `Y`.
- Support for Xbox/XInput, PlayStation, Nintendo Switch Pro, 8BitDo, and generic SDL-compatible controllers.
- Multi-controller selector when more than one controller is connected.
- Automatic and manual visual schemes for Universal, Xbox One, Xbox Series X / S, PlayStation, DualSense, Switch Pro, 8BitDo Ultimate, 8BitDo Pro, and Steam Controller layouts.
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

## Desktop and Fullscreen

Desktop mode provides the complete tester: controller selection, visual scheme override, guided checks, stick diagnostics, health, latency, input logs, vibration, device information, and exportable reports.

Fullscreen themes can open focused tester views or embed independent `StatusBadge`, `ButtonMap`, `StickCheck`, `RumblePad`, and `LatencyMini` blocks. The theme remains responsible for focus, controller navigation, layout, animation, and modal behavior. See the [Fullscreen integration guide](https://github.com/Naerian/playnite-nx-gamepad-tester/wiki/EN-Fullscreen-Theme-Integration) for commands and XAML examples.

## Compatibility note

Gamepad Tester uses SDL GameController normalization. Xbox/XInput, PlayStation, Nintendo Switch Pro, Steam Controller, 8BitDo, and generic mapped controllers are supported when SDL recognizes the active device mode. 8BitDo XInput and DInput modes can expose different mappings and capabilities.

## Support

If you find this project useful and want to support its development, consider buying me a coffee!

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/naerian)
