# Round 1 — merged findings
**Date:** 2026-10-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 5 | 0 |
| nit | 0 | 1 | 1 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/ask-sign-in-playwright-tests.cs:420
- Description: Ctrl-K step waited on `.twe-palette__row--active`, which is already present (Page row 0) before filtering — race on the "ask" check and screenshot.
- Source: general (Issue 1)
- Disposition notes: Fixed in 5e1dd6dba — `Assertions.Expect` waits until the active row's kind is "Command" and its name contains "ask" (case-insensitive) before screenshot/Enter.

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/command-palette-tests.cs:124
- Description: shown^excluded loop was a tautology; no completeness check of the roster against real ActionSets.
- Source: general (Issue 2)
- Disposition notes: Fixed in 5e1dd6dba — replaced by reflection completeness check (every `*ActionSet+Action : IAction` in web-spa is in `ActionRoster.All`).

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent-surface-state/agent-surface-state.close-ask-panel.cs:21
- Description: CloseAskPanel shown while ToggleAskExpand opted out for the same reason; "ask" ranking relied on label length.
- Source: general (Issue 3)
- Disposition notes: Fixed in 5e1dd6dba — CloseAskPanel opted out with panel-chrome reason; OpenAskPanel is now the only "ask" command; task.md Results command list updated.

### M4 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-roster.cs:82
- Description: Fallback description "Run <Name>" leaked a code identifier and matched "run" for every derived command.
- Source: general (Issue 4)
- Disposition notes: Fixed in 5e1dd6dba — empty fallback description; CommandPalette.razor omits the description span when empty; test updated.

### M5 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-analyzers/generators/action-roster-source-generator.cs:115
- Description: Opt-out reason read only as a string literal on one declaration; const reasons and other partial declarations missed.
- Source: general (Issue 5)
- Disposition notes: Fixed in 5e1dd6dba — scans every partial declaration, evaluates the argument with `GetConstantValue`; `GetAttributes()` not usable because the attribute is emitted by the same generator (documented in Design region). Generator tests cover const and other-partial cases.

### M6 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-roster.cs:83
- Description: Uncatalogued derived commands default to no permissions; rule undocumented.
- Source: general (Issue 6)
- Disposition notes: Fixed in 5e1dd6dba — rule recorded in skills/tw-blazor/SKILL.md (permissioned action must carry `[CatalogAction(Permissions = …)]` or opt out). A structural guard was not added (the suggestion's minimum ask was the rule).

### M7 — Severity: nit — Status: fixed
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/action-roster-source-generator-tests.cs:1
- Description: Missing Purpose region; no tests for private/generic/non-literal reason paths.
- Source: general (Issue 7)
- Disposition notes: Fixed in 5e1dd6dba — Purpose region plus 4 cases (private skipped, generic skipped, const reason, opt-out on other partial). Sourcegenerator tests 118/118.

### M8 — Severity: nit — Status: wontfix
- File: source/analyzers/timewarp-architecture-analyzers/generators/action-roster-source-generator.cs:46
- Description: `CommandPaletteOptOutAttribute` emitted into every project referencing the generator, including ones without actions.
- Source: general (Issue 8)
- Disposition notes: wontfix (orchestrator) — internal, harmless, and consistent with the existing `[Page]` / `[StateAccess]` emission pattern; conditional emission would make this generator the one exception.

## Duplicates / conflicts

- None.
