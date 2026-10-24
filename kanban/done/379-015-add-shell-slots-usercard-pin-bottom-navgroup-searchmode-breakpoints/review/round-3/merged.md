# Round 3 — merged findings
**Date:** 2026-10-24
**Sources:** general
**Scope:** `9ab07f923..f0bb4fd10` (fix pass for round 2)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- Fixed in round 1's pass and verified in round 2. Not changed in this delta.

### M2 — Severity: bug — Status: fixed
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:693
- Description: The generated `NavPlacement` enum was never bound, so a const field or a cast fell
  to Top.
- Source: general (round 1, re-opened in round 2)
- Disposition notes: `ReadNavPlacementValue` follows a const field to its initializer and a cast
  to its operand. It reads an unbound `NavPlacement.Bottom` or using-static `Bottom` by name, and
  `Foo.Bottom` is rejected. The Design region now describes those shapes. The tests do not
  declare the enum (`Run` compiles only the source, as an app does). Verified in round 3:
  `PageSourceGenerator_Tests` 36/36 and `NavigationDestinations_` 7/7.

### M3 — Severity: bug — Status: fixed
- Fixed in round 1's pass and verified in round 2. Not changed in this delta.

### M4 — Severity: suggestion — Status: fixed
- Fixed in round 1's pass and verified in round 2. `NavMenu` rail wiring was rewritten for M8, but
  the `SetRail` / tooltip path is unchanged.

### M5 — Severity: suggestion — Status: fixed
- Fixed in round 1's pass. The implementer reports `FirstPaint_Given_PrerenderedShell` 3/3 on this
  pass. Not re-run by the reviewer.

### M6 — Severity: nit — Status: fixed
- Fixed in round 1's pass and verified in round 2.

### M7 — Severity: nit — Status: fixed
- Fixed in round 1's pass and verified in round 2.

### M8 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/shell-rail.ts:24
- Description: The watcher state was module-level, so a second menu's `watch` replaced the first
  listener.
- Source: general (round 2)
- Disposition notes: `watch` returns a per-call handle that holds its own query and listener. Its
  `stop` is idempotent. `NavMenu` stores that handle, stops it in `DisposeAsync` and on a
  dispose-during-import race, and releases the module reference. The breakpoint file test pins
  that shape. Verified in round 3: `ShellBreakpoints_Should_` 1/1, `./bin/dev build` 0 errors.

## Duplicates / conflicts

- None. No new findings this round. The using-alias and bare-integer observations in
  `general.md` were not raised.
