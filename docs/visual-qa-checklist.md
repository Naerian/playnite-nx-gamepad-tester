# Visual QA Checklist

Use this checklist before tagging a release or publishing a `.pext`.

## Window Sizes

- Default Playnite extension window.
- 1280x720.
- 1600x900.
- 1920x1080 maximized.

## Windows Scaling

- 100%.
- 125%.
- 150%.

## Controller States

- No controller connected: warning only, tester UI hidden.
- One controller connected: selector hidden unless enabled in settings.
- Two controllers connected: selector aligned with tabs, no overlap with window controls.
- Visual scheme override works for Universal, Xbox, PlayStation, DualSense, Switch Pro, 8BitDo Ultimate 2, and 8BitDo Pro.

## Tabs

- Test: no unnecessary scroll at 1080p, controller map centered, guided test readable.
- Sticks & Calibration: stick scopes do not overlap labels.
- Latency: central result remains visible before and after a captured input.
- Input log: disabled state is readable, enabled log text has theme contrast, export button aligned.
- Device info: left-aligned and readable in light/dark themes.

## Theme Contrast

- Default dark theme.
- Default light theme.
- Active input pills keep readable text.
- Disabled/secondary text remains visible against panel backgrounds.
