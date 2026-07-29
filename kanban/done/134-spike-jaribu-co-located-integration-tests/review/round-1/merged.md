# Round 1 — merged findings
**Date:** 2026-07-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 2 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: wontfix
- File: `source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs:34,36`; `source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs:18,20`
- Description: `#if !JARIBU_MULTI` is not template-safe — dotnet-new's conditional processor treats the unrecognized symbol as unset, strips the directive lines, and keeps the guarded top-level `return` unconditionally; a generated app's aggregator build fails with CS8802. Verified empirically by generating an app from the spike branch.
- Suggestion: permanent mechanism must route the mode switch through a template-recognized symbol, a `cnd:noEmit`-style escape, or exclude co-located `-tests.cs` from conditional processing; extend `dev template-smoke` as the regression gate.
- Source: general
- Disposition notes: wontfix ON THE SPIKE BRANCH (never merges; generated apps unaffected while it stays unmerged). Recorded in findings.md as a CONFIRMED template-safety adoption blocker that the follow-up adoption task must solve before any co-located runfile lands on dev. Decider: orchestrator (fix is explicitly the follow-up task's design work; task spec keeps grammar/template mechanism landing out of spike scope).

### M2 — Severity: bug — Status: wontfix
- File: `tests/container-apps/jaribu-spike-tests/jaribu-spike-tests.csproj`; `tools/dev-cli/endpoints/test-command.cs:64-69`
- Description: `dev test` globs `tests/**/*.csproj` (independent of `.slnx`) and invokes `dotnet test <csproj-path> -c Release`, which fails against MTP projects on .NET 10 ("Testing with VSTest target is no longer supported"). The spike aggregator is therefore picked up and fails under `dev test` on the spike branch. Bare `dotnet test` from the project dir works (7/7).
- Suggestion: findings.md flags as a concrete adoption blocker: `dev test` invocation must gain MTP support (or aggregators live outside its glob) before any Jaribu aggregator is committed to dev.
- Source: general
- Disposition notes: wontfix ON THE SPIKE BRANCH (dev's `dev test` unaffected while unmerged; changing `dev test` is explicitly out of spike scope per task.md). Recorded in findings.md as adoption blocker with the two fix options. Decider: orchestrator.

### M3 — Severity: suggestion — Status: wontfix
- File: `source/container-apps/{api,web}/msbuild/feature-membership.targets:40-42`
- Description: Blanket `Exclude **/*-tests.cs` carve-out silently exempts any orphaned/misnamed `-tests.cs` file from the membership guard — a validation blind spot.
- Suggestion: document as the explicit tradeoff of "exclude glob" vs "registered-unrouted `tests` suffix" in findings.md; it is confirming evidence for the strategic carve-out question.
- Source: general
- Disposition notes: wontfix on the spike branch (inline comment already marks the exclude as non-permanent); tradeoff documented in findings.md feeding the strategic mechanism decision. Decider: orchestrator.

## Duplicates / conflicts

- None — single reviewer, three distinct findings.
