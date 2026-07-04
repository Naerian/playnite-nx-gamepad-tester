# Playnite NX Gamepad Tester

Playnite NX Gamepad Tester is a Playnite extension for testing controllers directly from Playnite Desktop mode.

It is designed for couch, TV, handheld-PC, and console-like setups where users want to verify gamepad buttons, sticks, triggers, rumble, drift, and device metadata without leaving Playnite.

## Features

- Test connected controllers from Playnite Desktop mode.
- SDL GameController input backend for normalized controller mappings.
- Supports Xbox-compatible XInput controllers.
- Supports PlayStation, Switch Pro, 8BitDo, and generic SDL-compatible controllers where SDL exposes a GameController mapping.
- Multi-controller selector when more than one controller is connected.
- Optional Playnite Desktop sidebar entry.
- Extension settings for sidebar visibility, controller switching behavior, device selector behavior, and rumble tests.
- Universal SVG-style controller map with live button, shoulder, trigger, stick, and D-pad feedback.
- Analog trigger percentage display with live visual fill.
- Stick position scopes for left and right sticks.
- Quick test checklist for buttons, triggers, and stick range coverage.
- Input history for button press/release events.
- Stick diagnostics with movement paths, circular coverage sectors, max reach, and drift status.
- Controller health estimate based on rest drift.
- Device information panel with controller name, detected display name, VID/PID, layout, and backend.
- Rumble diagnostics with standard, light, medium, heavy, low motor, high motor, pulse, alternating, and ramp tests.
- Localizable UI through Playnite resource dictionaries.

## Requirements

- Windows.
- Playnite 10.x.
- Playnite SDK compatible runtime.
- A controller exposed to SDL as a GameController.

The plugin reads controller inputs through SDL GameController normalization. Sticks are normalized to `-1..1`, and triggers are normalized to `0..1`.

## Installation

1. Download the latest `.pext` file from the [GitHub releases page](https://github.com/Naerian/playnite-nx-gamepad-tester/releases).
2. Open the `.pext` file, or drag it into Playnite.
3. Restart Playnite if Playnite asks you to do so.

After installation, the extension appears as **Gamepad Tester** in Playnite's extension menus.

## Usage

Open:

`Extensions > Gamepad Tester`

Connect one or more controllers before opening the tester. If multiple controllers are available, the selector at the top of the window lets you choose the active device by name.

The main **Test** tab shows a controller map, live button states, stick scopes, trigger percentages, quick health metrics, and basic device information.

## Quick Test

The **Quick test** tab tracks whether the main normalized inputs have been seen during the current session.

Use it to confirm that every face button, shoulder, stick click, menu button, D-pad direction, trigger, and stick edge can be detected by the plugin. Press **Reset session** to clear the checklist and start again.

## Stick Diagnostics

The **Sticks** tab provides a more detailed look at joystick behavior:

- **Path** shows the recent movement trail for each stick.
- **Circular coverage** fills the outer ring as the stick reaches edge sectors around a full 360-degree sweep.
- **Max reach** reports the strongest magnitude seen during the current session.
- **Drift status** reports the current stick magnitude while resting near center.

For a clean circular test, press **Reset sticks**, then move the stick slowly around the full outer edge. A healthy stick should fill most or all sectors and produce a smooth path without large gaps or sudden jumps.

## Inputs

The **Inputs** tab shows a button matrix and a chronological input history.

This is useful when a button works in-game but appears mapped differently, because it shows the SDL-normalized button that the plugin receives.

## Diagnostics

The **Diagnostics** tab separates health and analog information from the visual controller map.

The health score is based on rest drift only. Quick test coverage is tracked separately so actively moving the sticks during a test does not make a new controller look unhealthy.

## Rumble

The **Rumble** tab exposes several vibration patterns:

- Standard, light, medium, and heavy presets.
- Low-frequency motor test.
- High-frequency motor test.
- Pulse, alternating, and ramp patterns.

Rumble support depends on the controller mode, driver, and SDL support for the connected device.

## Device Detection

Gamepad Tester uses the controller name, vendor ID, product ID, and SDL layout information to present a readable display name where possible.

Supported families include:

- Xbox One / Series and compatible XInput devices.
- PlayStation DualShock 4 and DualSense controllers where SDL maps them.
- Nintendo Switch Pro controllers.
- 8BitDo controllers in XInput or compatible SDL GameController modes.
- Generic SDL GameController-compatible devices.

If a controller appears as a generic device, it can usually still be tested as long as SDL exposes normalized axes and buttons.

## Fullscreen Status

The current implementation targets Playnite Desktop mode first.

Fullscreen integration is planned as a later step. The intended direction is to expose a controller-friendly view or theme integration surface without overloading Playnite's native text-only extension menus.

## Localization

The plugin uses Playnite localization resource dictionaries under `Localization/`.

Translations are stored as locale-specific XAML resource dictionaries. To add or update a translation, copy an existing locale file, rename it to the target locale, and translate the string values while keeping the same resource keys.

Community translation contributions are welcome.

## Support

If you find this project useful and want to support its development, consider buying me a coffee!

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/naerian)
