# Playnite NX Gamepad Tester

> **This plugin is no longer maintained and will be removed soon.**
>
> Controller testing (and more) now lives in **[Controller Manager](https://github.com/Naerian/playnite-nx-session-controller-manager)**. Install that plugin and uninstall Gamepad Tester. Do not keep both: Fullscreen theme blocks named `GamepadTester_*` would register twice and break those views.

## Move to Controller Manager

1. Install Controller Manager from the [releases page](https://github.com/Naerian/playnite-nx-session-controller-manager/releases), from Playnite **Add-ons → Browse**, or open:

   `playnite://playnite/installaddon/ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc`

2. Uninstall **Gamepad Tester** from **Add-ons → Installed → Generic**.

3. Restart Playnite when prompted.

Controller Manager already includes the gamepad tester plus session tracking, connection alerts, adaptive switching, overlays, and battery indicators.

Docs: [English wiki](https://github.com/Naerian/playnite-nx-session-controller-manager/wiki) · [Wiki en español](https://github.com/Naerian/playnite-nx-session-controller-manager/wiki)

## Version 1.3.0 (final)

`1.3.0` is the last Gamepad Tester release. It only shows a replacement notice (startup, settings, sidebar, top panel) with **Install Controller Manager** and **Uninstall Gamepad Tester**. There is no tester UI anymore.

If you are still on `1.2.1` or older, install `1.3.0` from [this repo’s releases](https://github.com/Naerian/playnite-nx-gamepad-tester/releases), then follow the notice above.

## Historical notes

Earlier versions provided Desktop controller testing and optional Fullscreen theme blocks (`StatusBadge`, `ButtonMap`, `StickCheck`, `TriggerCheck`, `RumblePad`, `LatencyMini`). That work continues in Controller Manager; theme authors should migrate integrations there.

Archived docs in this repo:

- [Fullscreen theme contract](docs/theme-integration/CONTRACT.md)
- [Sample Fullscreen view](docs/theme-integration/GamepadTesterSampleView.xaml)
- Old wiki: [EN](https://github.com/Naerian/playnite-nx-gamepad-tester/wiki/EN-Installation-and-Quick-Start) · [ES](https://github.com/Naerian/playnite-nx-gamepad-tester/wiki/ES-Instalacion-e-inicio-rapido)

## Support

If you find Controller Manager useful and want to support its development:

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/naerian)
