# Changelog

## 0.3.5

- Moved the repository root to the plugin project folder.
- Updated build and packaging scripts to run from the new repository root.

## 0.3.4

- Added support for Gamepad Tester embedded blocks in dynamically opened fullscreen theme views.

## 0.3.3

- Restored Desktop tabs to inherit the active Playnite theme style.
- Removed the duplicated Vibration tab while keeping rumble controls in the Test dashboard.
- Updated internal tab navigation indexes after removing the duplicate tab.

## 0.3.2

- Added the Playnite add-on installer manifest.
- Switched the packaged extension icon to `media/icon.png`.
- Updated the Playnite sidebar and top panel icon to the bundled `gamepad-2` glyph.

## 0.3.1

- Removed README build and QA sections to keep the documentation focused for users and theme developers.
- Localized the bundled resource dictionaries for all included non-English languages.

## 0.3.0

- Added fullscreen-friendly Playnite integration with a configurable top panel shortcut.
- Added a theme custom element, `GamepadTester_GamepadTesterLauncher`, for fullscreen themes that want to place their own launcher.
- Added embeddable fullscreen theme blocks for status, button map, sticks, rumble, and latency.
- Added fullscreen-aware window sizing for the tester and guided test windows.
- Improved visual scheme consistency across controller layouts.
- Improved Test, Latency, Input log, Sticks & Calibration, and Device info panels.
- Fixed missing localization entries for latency samples and device capability labels.
- Updated release packaging metadata.

## 0.2.0

- Redesigned the main Test dashboard with controller map, health, guided checks, current inputs, stick snapshots, and rumble in one place.
- Added multiple visual schemes, including Universal, Xbox, PlayStation, DualSense, Switch Pro, 8BitDo Ultimate 2, and 8BitDo Pro.
- Added configurable drift, range, trigger, and calibration thresholds.
- Added guided test flow for normalized button, trigger, and stick edge coverage.
- Added opt-in input log with export.
- Added latency diagnostics and report export.
- Improved SDL extra button labels with technical SDL raw indexes.
- Added `.pext` packaging script and visual QA checklist.

## 0.1.0

- Initial Playnite Desktop extension prototype.
- SDL GameController polling.
- Live controller map, stick scopes, triggers, health, input log, rumble, and device information.
