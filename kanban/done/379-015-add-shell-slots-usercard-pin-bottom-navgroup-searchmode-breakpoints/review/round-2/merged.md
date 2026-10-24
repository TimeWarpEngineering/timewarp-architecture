# Round 2 — merged findings
**Date:** 2026-10-24
**Sources:** general
**Scope:** `b6bd9ba6d..27b7623a9` (fix pass for round 1)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 2 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 1 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/ShellUserCard.razor:29
- Description: The card name was cached when the auth parameter was set, so an alias that
  arrived later never showed.
- Source: general (round 1)
- Disposition notes: `DisplayName` is computed on each render. A render test pushes an alias
  through a ProfileState re-render. Verified in round 2 (`TimeWarpPage_Should_` 14/14).

### M2 — Severity: bug — Status: open
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:689
- Description: The generator still never binds the generated `NavPlacement` enum, because it is
  emitted by `RegisterSourceOutput`. `GetConstantValue` therefore has no value in an app. A const
  field falls back to the name `Pin`, a cast falls back to `null`, and both become Top with no
  diagnostic. A Roslyn probe without the enum in source showed `HasValue=False` for the const
  field, the cast, and the member. The Design region overclaims. The const-field and cast tests
  declare the enum in source, which is not how an app compiles.
- Suggestion: Either follow a const field's symbol to its initializer and read a cast operand's
  literal value, or accept only `NavPlacement.Top/Bottom` and using-static `Top/Bottom` and
  report a diagnostic for anything else. Correct the Design region. Run the tests without the enum
  declared in source.
- Source: general (round 1, re-opened in round 2)
- Disposition notes: The round 1 fix made bound constants work, but production never binds them.

### M3 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/NavMenu.razor.css:14
- Description: A tall menu overflowed onto the pinned card.
- Source: general (round 1)
- Disposition notes: `overflow-y: auto`, pinned by the breakpoint file test and the Playwright
  rail check.

### M4 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/NavMenu.razor:46
- Description: The rail removed the accessible names, and the tooltip repeated the visible label
  at full width.
- Source: general (round 1)
- Disposition notes: Labels and the card meta are visually hidden. `ItemTooltip` sets the title
  only in the rail, through `shell-rail.ts`.

### M5 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/first-paint-playwright-tests.cs:124
- Description: The rail had never been rendered at a real width.
- Source: general (round 1)
- Disposition notes: `Rail_Should_KeepTheCardClearOfTheMenu` checks 1024×700 and 1400×800. The
  chevron is documented as the rail mark for categories. The implementer reported 3/3 passing;
  the check was not re-run in round 2.

### M6 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor.css:131
- Description: The user-card padding was duplicated through `::deep`.
- Source: general (round 1)
- Disposition notes: The `::deep` copy is removed.

### M7 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor:172
- Description: Nothing read `--twe-shell-nav-width`.
- Source: general (round 1)
- Disposition notes: `Width="var(--twe-shell-nav-width)"`, pinned by the breakpoint file test.

### M8 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/source/features/shell-rail.ts:19
- Description: The watcher state is module-level and the module instance is shared. A second
  `watch` replaces the first listener, and the first instance's `stop()` removes the second
  one's. This works today only because the shell disposes the old `NavMenu` before the new one
  calls `watch`.
- Suggestion: Return a per-instance handle from `watch` and dispose it in `DisposeAsync`, as
  `drawer.ts` `Bind` does.
- Source: general (round 2)
- Disposition notes:

## Duplicates / conflicts

- None. The round 2 general Issue 1 is M2 carried forward. Issue 2 is the new M8.
