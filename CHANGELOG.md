# Changelog

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
