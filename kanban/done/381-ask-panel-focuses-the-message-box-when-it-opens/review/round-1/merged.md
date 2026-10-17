# Round 1 — merged findings
**Date:** 2026-10-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/command-palette.ts:68
- Description: Ask claims the palette's return target; when the palette was opened from the appbar
  search field that target is the trigger, and Ask's close-time `RestoreOpener` focuses it outside
  the palette's `restoring` guard, re-opening the palette.
- Suggestion: pass a palette-owned guarded restore with the claim; add a Playwright step for the
  search-field path.
- Source: general
- Disposition notes: fixed on this task (review oracle). The new Playwright test failed first
  (palette visible after Ask close), which also showed a second ordering: the palette's
  `RestoreFocus` can land on the search field before Ask's `CaptureOpener`, so Ask captures the
  trigger directly. Fix: the claim carries a palette-owned `restore` (focus behind the `restoring`
  guard) whenever the claimed target is inside the trigger, whether handed over or already
  focused; `RestoreOpener` uses it. Test:
  `Close_Should_Not_Reopen_Palette_When_Ask_Came_From_Search_Field`.

## Duplicates / conflicts

- None (single reviewer).
