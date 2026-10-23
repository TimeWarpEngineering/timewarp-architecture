# Round 2 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:10; components/elements/Drawer.razor:28-33; task.md Results
- Description: Design blamed `fluent-button` for Tab leaving the dialog.
- Source: general (round 1)
- Disposition notes: Fixed in `0f55dc6da`. Design now says this Chromium does not keep Tab inside a modal dialog of only native buttons. The Playwright probe asserts this for `display:block` and `display:flex`, and it passed in round 2.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/elements/Drawer.razor:28-30; Drawer.razor.css
- Description: Design said a flex column on the dialog drops the browser's Tab trap.
- Source: general (round 1)
- Disposition notes: Fixed in `0f55dc6da`. The panel's reason is now the flex column and the sheet hit target. The dialog has `padding: 0`, so the hit-target claim holds.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/drawer.ts:123, 150, 179
- Description: The `disposed` guards in listeners that Dispose had already removed, and the `pendingModuleClose` increment in Dispose, were dead code.
- Source: general (round 1)
- Disposition notes: Fixed in `0f55dc6da`. `Dispose` removes its listeners, then calls `dialog.close()` while the dialog is open. Only `Show` reads `disposed`. The new Playwright Dispose check (0 `RequestClose` calls, and `Show` stays shut afterwards) passed.

## Duplicates / conflicts

- None. No new findings in round 2.
