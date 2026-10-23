# Round 1 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:10; components/elements/Drawer.razor:30-32; task.md Results
- Description: The Design regions blame `fluent-button` for Tab leaving the dialog. HeadlessChrome/153.0.8010.12 also lets Tab leave a `showModal()` dialog that holds only native buttons (`BODY -> a -> b -> BODY`). The real cause is that Chromium does not keep Tab inside a modal dialog. Goal 4 asks for the real reason for each remaining piece, so this goal is not met.
- Suggestion: Restate the cause in both Design regions and in Results. Optionally add a plain-button control to the evidence.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/components/elements/Drawer.razor:28-30
- Description: The Design says a flex column on the dialog "drops the browser's Tab trap". There is no Tab trap with either `display:block` or `display:flex`, so this is the wrong reason for the inner panel.
- Suggestion: Replace it with the real reason, or say the panel is a layout choice only.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:123, 150, 179
- Description: `Dispose` removes its listeners before it closes the dialog, so three pieces of code never take effect: the `disposed` guards in `onKeyDown` and `onDialogClose`, and the `pendingModuleClose` increment that `Dispose` adds. Design line 19 says Dispose is counted, but nothing reads that count.
- Suggestion: Remove the dead guards. Make `Dispose` call `dialog.close()` directly, or fix the Design line. Keep the guard in `Show`.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).

## Probe (reviewer, for M1/M2)

Playwright 1.63.0 runfile outside the repo, HeadlessChrome/153.0.8010.12:
`<div style="display:contents"><dialog style="display:{block|flex}"><button id=a><button id=b></dialog></div>`,
`showModal()`, focus `#b`, Tab x4 → `BODY -> a -> b -> BODY` for both display values.
