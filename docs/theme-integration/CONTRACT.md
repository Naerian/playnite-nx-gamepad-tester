# Gamepad Tester Fullscreen contract 1.0

This contract is the stable surface intended for Playnite Fullscreen theme developers.

## Blocks

`GamepadTesterLauncher`, `StatusBadge`, `ButtonMap`, `StickCheck`, `TriggerCheck`, `RumblePad`, and `LatencyMini`.

Use the attached host property for static or dynamically created views:

```xaml
<ContentControl gt:GamepadTesterThemeHost.Block="ButtonMap" />
```

The plugin initializes marked hosts on `Loaded`. A theme helper can force another scan with:

```xaml
Command="{PluginSettings Plugin=GamepadTester, Path=RefreshThemeBlocksCommand}"
```

## Host diagnostics

Every marked `ContentControl` exposes these read-only attached properties:

- `InitializationState`: `Pending`, `WaitingForPlugin`, `Ready`, `UnknownBlock`, `Occupied`, or `Error`.
- `InitializationMessage`: a developer-facing explanation.
- `ResolvedBlock`: the normalized block name.
- `ContractVersion`: currently `1.0`.

Bind them with `(gt:GamepadTesterThemeHost.InitializationState)` and the equivalent property paths. `Occupied` means the host already had content and the plugin intentionally did not replace it.

## Runtime state

Each embedded block exposes:

- `IsControllerConnected`
- `IsInputCaptureActive`
- `ActiveTestKind`: `None`, `Buttons`, `Sticks`, `Latency`, or `Rumble`
- `ThemeContractVersion`

The shared data context also exposes `HasController`, `IsAnyTestRunning`, the individual `Is...Running` properties, commands, and live diagnostic values. Theme code should bind to these states instead of inspecting child visuals.

## Resources

- `GamepadTesterControlBackgroundBrush`
- `GamepadTesterButtonBackgroundBrush`
- `GamepadTesterControlBorderBrush`
- `GamepadTesterStickGuideBrush`
- `GamepadTesterTextBrush`

Declare overrides at window or view scope. The plugin falls back to the corresponding Playnite theme resources.

## Navigation responsibility

The theme owns focus, transitions, close behavior, and background navigation. Keep test blocks in a contained focus scope and suppress modal close actions while `IsInputCaptureActive` is true. Name the return control `GamepadTester_BackButton` when you want focus restored there after capture.
