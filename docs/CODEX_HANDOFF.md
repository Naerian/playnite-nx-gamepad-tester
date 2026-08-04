# Codex handoff - Playnite NX Gamepad Tester

This document is the recovery note for future Codex sessions after a Windows reinstall, Codex CLI reinstall, or workspace move. Start here before changing the extension.

## Current project state

- Project: Playnite NX Gamepad Tester.
- Repository: `https://github.com/Naerian/playnite-nx-gamepad-tester`.
- Local path before reinstall: `C:\Users\naria\Documents\New project\GamepadTester\GamepadTester`.
- Playnite path used during development: `C:\Playnite`.
- Installed extension path: `C:\Playnite\Extensions\GamepadTester`.
- Current extension version in this checkout: `1.2.1`.
- Current main commit before this handoff: `f09c252` (`Release Gamepad Tester 1.2.1`).
- Release artifact naming: `GamepadTester-<version>.pext`.
- Public release URL pattern: `https://github.com/Naerian/playnite-nx-gamepad-tester/releases/download/v<version>/GamepadTester-<version>.pext`.

The repository was clean and synchronized with `origin/main` before this handoff was created.

## What the extension does

Gamepad Tester is a Playnite extension for testing controllers from Desktop mode, with optional Fullscreen theme integration blocks. It uses SDL GameController normalization to support Xbox/XInput, PlayStation, Nintendo Switch Pro, Steam Controller, 8BitDo, and generic mapped controllers when SDL recognizes the active device mode.

Main Desktop features:

- Live visual controller layouts.
- Multi-controller selection.
- Visual scheme override.
- Button, D-pad, shoulder, trigger, and stick feedback.
- Guided input test.
- Stick diagnostics and calibration-oriented diagnostics.
- Health/readiness estimation.
- Polling/latency diagnostics with chart.
- Input log with opt-in recording and export.
- Rumble tests.
- Device information and exportable compatibility report.
- Optional Desktop sidebar item.

Fullscreen theme integration:

- Theme contract version currently documented as `1.1`.
- Main docs: `docs/theme-integration/CONTRACT.md`.
- Reference XAML: `docs/theme-integration/GamepadTesterSampleView.xaml`.
- Blocks include status, button map, stick check, trigger check, rumble pad, and latency mini.
- Back/B, Escape, directional navigation, tab movement, and close actions are guarded while button, stick, or latency capture is active.
- `LB + RB` held for one second is the deliberate exit gesture from capture mode.
- Embedded controls expose `CanNavigateBack` for themes and can guard controls named `GamepadTester_BackButton`.

## Important architectural decisions

- Build with classic .NET Framework MSBuild, not `dotnet build`.
- Target framework is `.NET Framework 4.6.2`.
- Use SDL GameController normalization. Do not hand-roll separate XInput/DInput button maps unless there is a strong reason.
- 8BitDo devices can expose themselves as XInput or DInput. Do not guess the mode when SDL metadata is inconclusive; report the evidence.
- Use Playnite's bundled SDL runtime where available.
- Do not overwrite the installed DLL while Playnite is open.
- Always check both `Playnite.DesktopApp` and `Playnite.FullscreenApp` before local install.
- Keep local signing/export secrets out of Git.
- `BuildGamepadTester.ps1` and `PackageGamepadTester.ps1` are currently local helper scripts ignored by `.gitignore`, not tracked project files.
- Public package verification should compare local and downloaded `.pext` SHA-256 hashes, because GitHub raw views and manifests can lag.
- Theme resources should be dedicated where theme authors need override control, for example:
  - `GamepadTesterControlBackgroundBrush`
  - `GamepadTesterButtonBackgroundBrush`
  - `GamepadTesterControlBorderBrush`
  - `GamepadTesterStickGuideBrush`
  - `GamepadTesterTextBrush`

## Build

Use system MSBuild:

```powershell
cd "C:\Users\naria\Documents\New project\GamepadTester\GamepadTester"
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" "GamepadTester.sln" /t:Rebuild /p:Configuration=Debug
```

The release/package flow has historically used `bin\Debug`, because the local helper scripts package from there. Re-check the scripts and project configuration before changing that.

Optional local helper if present:

```powershell
.\BuildGamepadTester.ps1
```

## Test

Run the bundled regression checks:

```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" "GamepadTester.Tests\GamepadTester.Tests.csproj" /t:Rebuild /p:Configuration=Debug
.\GamepadTester.Tests\bin\Debug\GamepadTester.Tests.exe
```

Expected coverage around version `1.2.1` was 136 checks, including Fullscreen capture/navigation gating.

Useful manual checks:

- Open Desktop extension from toolbar/menu.
- Open Desktop extension from SidebarItem if enabled.
- Confirm controller selector and visual scheme selector are readable in the active theme.
- Confirm input log opt-in, reset, and export.
- Confirm latency Start/Stop/Reset/Export.
- Confirm stick diagnostics start/stop and export.
- Confirm rumble status messages are localized.
- In Fullscreen/theme integration, verify:
  - Button test does not let Back/B close the view during capture.
  - Stick diagnostics does not let Back/B close the view during capture.
  - Latency capture does not let Back/B close the view during capture.
  - `LB + RB` held for one second exits capture and restores navigation.
  - Theme controls can bind close/back behavior to `CanNavigateBack`.

## Local install into Playnite

Never install while Playnite is running:

```powershell
Get-Process Playnite.DesktopApp, Playnite.FullscreenApp -ErrorAction SilentlyContinue
```

If either process exists, close Playnite first.

Then copy the built extension to:

```text
C:\Playnite\Extensions\GamepadTester
```

The local helper script can do this if present:

```powershell
.\BuildGamepadTester.ps1
```

After install, verify at least:

```powershell
Get-Content "C:\Playnite\Extensions\GamepadTester\extension.yaml"
Get-FileHash "C:\Playnite\Extensions\GamepadTester\GamepadTester.dll" -Algorithm SHA256
```

## Package

The current local helper script creates `dist\GamepadTester-<version>.pext`:

```powershell
.\PackageGamepadTester.ps1
```

Manual package contents should include:

- `GamepadTester.dll`
- `extension.yaml`
- `icon.png` if present
- `media\`
- `Localization\`

Do not include build folders, pdb files, certificates, or local logs in the public `.pext`.

## Verify installer and package

Use Playnite Toolbox from the portable installation:

```powershell
& "C:\Playnite\Toolbox.exe" verify Installer ".\installer.yaml"
```

For package checks, inspect the `.pext` as a zip and verify it contains only expected extension files. After publishing, download the GitHub release asset and compare hashes:

```powershell
Get-FileHash ".\dist\GamepadTester-1.2.1.pext" -Algorithm SHA256
```

## Release checklist

When creating a new version:

1. Update version in `extension.yaml`.
2. Update version in `Properties\AssemblyInfo.cs`.
3. Update visible version/About strings if present.
4. Update `README.md` version note when user-facing behavior changes.
5. Update `CHANGELOG.md`.
6. Add the new entry to `installer.yaml`.
7. Build.
8. Run tests.
9. Package `.pext`.
10. Verify `installer.yaml` with `C:\Playnite\Toolbox.exe`.
11. Commit and push.
12. Create/push tag if the release flow expects tags.
13. Create GitHub release with the `.pext`.
14. Download the public `.pext` and compare SHA-256 against the local artifact.
15. Update the Wiki if theme integration, user workflow, or release behavior changed.

GitHub CLI release command pattern:

```powershell
gh release create "v<version>" ".\dist\GamepadTester-<version>.pext" --repo Naerian/playnite-nx-gamepad-tester --title "Gamepad Tester <version>" --notes-file ".\CHANGELOG.md"
```

Adjust release notes rather than publishing the whole changelog if needed.

## Wiki

The project uses the GitHub Wiki for user and theme-developer documentation.

Before reinstall, a local wiki checkout was present next to the repo:

```text
C:\Users\naria\Documents\New project\GamepadTester\playnite-nx-gamepad-tester.wiki
```

Wiki remote should be:

```text
https://github.com/Naerian/playnite-nx-gamepad-tester.wiki.git
```

After reinstall, clone it if needed:

```powershell
cd "C:\Users\naria\Documents\New project\GamepadTester"
git clone https://github.com/Naerian/playnite-nx-gamepad-tester.wiki.git playnite-nx-gamepad-tester.wiki
```

Keep at least these areas current:

- Installation and quick start.
- Desktop usage.
- Fullscreen theme integration.
- Theme resource overrides.
- Dynamic custom window initialization.
- Capture/back-navigation rules.
- Troubleshooting.

## Files and folders worth knowing

- `GamepadTester.cs`: extension entrypoint and Playnite integration.
- `Views\GamepadTesterView.xaml`: main Desktop UI.
- `ViewModels\GamepadTesterViewModel.cs`: main state, commands, diagnostics, capture modes.
- `Services\SdlGamepadProvider.cs`: SDL input backend.
- `Services\ControllerIdentificationService.cs`: controller naming and visual scheme identification.
- `Models\ControllerVisualSchemeCatalog.cs`: available visual schemes.
- `Views\ControllerLayouts\`: visual controller layouts.
- `Views\Controls\LatencyRateChart.cs`: latency chart.
- `Views\Controls\DiagnosticRadarChart.cs`: diagnostic radar chart.
- `Views\ThemeIntegration\`: Fullscreen embeddable controls.
- `docs\theme-integration\CONTRACT.md`: theme developer contract.
- `docs\theme-integration\GamepadTesterSampleView.xaml`: example integration.
- `Localization\*.xaml`: localized resources. Keep key parity across all dictionaries.
- `installer.yaml`: Playnite add-on database manifest.
- `media\icon.png`: add-on/package icon.
- `Icons\gamepad-2.svg`: Desktop sidebar/top-panel glyph.

## Things to back up before formatting

- Clone/push state is on GitHub, but copy any local untracked artifacts you care about.
- Copy portable Playnite if used:

```text
C:\Playnite
```

- Copy Playnite user data/settings if it is not fully portable.
- Back up certificates/API keys/secrets manually. Windows user/machine-encrypted credentials may not survive formatting.
- Back up any local `.pfx`, `.cer`, screenshots, release notes drafts, and unpublished test artifacts if needed. They are intentionally ignored by Git.
- If a theme integration was being tested locally, back up the theme folder, for example:

```text
C:\Users\naria\Documents\New project\Nexium
C:\Playnite\Themes\Fullscreen
```

## Known pending ideas

These are not mandatory release blockers, but useful next directions:

- Continue refining Fullscreen theme-developer ergonomics with real theme feedback.
- Keep controller capture states strict so Playnite/global navigation cannot close tester panels during tests.
- Consider extracting more controller layout metadata so XAML layouts are easier to maintain.
- Improve Playnite Addon Database manifest/toolbox validation whenever Playnite changes validation rules.
- Keep chart/theme resources separated enough that themes can override visuals without affecting unrelated Playnite controls.

## Suggested prompt after reinstall

Use this in a new Codex session:

```text
Estamos retomando Playnite NX Gamepad Tester.
Lee docs/CODEX_HANDOFF.md en C:\Users\naria\Documents\New project\GamepadTester\GamepadTester y continúa desde ahí.
Antes de tocar nada, comprueba git status, versión actual, Playnite cerrado y estado de releases.
```
