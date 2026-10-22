# Round 1 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/components/composites/Tabs.razor:99; SegmentedControl.razor:97
- Description: The root's inline `onkeydown` cancels ArrowLeft/ArrowRight/Home/End even with Alt, Ctrl, Meta, or Shift held. `OnKeyDownAsync` ignores those chords, so they are cancelled and do nothing. With focus in the group, Alt+Left/Right (browser Back/Forward) and Ctrl+Home/End (page scroll) stop working.
- Suggestion: Guard the script on `!altKey && !ctrlKey && !metaKey && !shiftKey` so it cancels exactly what the C# handler handles. Pin the guard with a render test.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/components/composites/roving-selection.cs:61 (plus both root `onkeydown` strings)
- Description: The cancelled-key list (two hand-typed JS strings) and the `FromKey` switch must agree. Only a comment keeps them in step, which goes against the AGENTS.md rule "generate one from the other or add a build check".
- Suggestion: Keep the key names as constants in `RovingSelection` and build one cancel-script constant from them. Both roots bind it. Add a test that the rendered `onkeydown` covers each handled key and the modifier guard.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/components/composites/Tabs.razor:22; SegmentedControl.razor:20
- Description: The `@code` blocks are identical (about 80 lines each), and so is the keyboard and focus Design prose. The render-test helpers are duplicated across the two test files.
- Suggestion: Extract a shared abstract base component (`@inherits`) in `components/composites/` that owns the parameters, focus, choose, and key handling, with the shared Design region. Each `.razor` keeps its markup and ARIA role names. Optionally share the test parsers.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/components/composites/Tabs.razor:85; SegmentedControl.razor:83
- Description: The method-scope pattern variable `MoveResult` is PascalCase and shadows the type name (tw-csharp: method-scope camelCase).
- Suggestion: Rename it to `move`.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- M1 and M2 touch the same script. M1 is the live behavior defect. M2 is the single-source fix that stops the two copies from drifting again. One change can close both, and M3 then makes it a single edit.
