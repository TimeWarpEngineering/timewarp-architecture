# Round 2 — merged findings
**Date:** 2026-10-30
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tools/dev-cli/Directory.Build.props:13
- Description: `RCS1194` is still in the dev CLI `NoWarn`, with its comment. It suppresses a
  warning, which the task forbids, and the operator rejected it as an exception (2026-10-30).
  Silenced rule: Roslynator RCS1194.
- Suggestion: Remove `RCS1194` and its comment lines after M2. DevCli 4.0.0-beta.9
  `package-feed-lookup.cs:142` adds the `HttpRequestError` constructor, so the warning no longer
  fires.
- Source: general (carried from round 1 M1)
- Disposition notes: The operator chose the upstream fix (Nuru beta.9 shipped). No fix pass has run yet.

### M2 — Severity: bug — Status: open
- File: Directory.Packages.props:18
- Description: The `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` pins are `4.0.0-beta.8`. The task
  requires `4.0.0-beta.9`.
- Suggestion: Bump both pins to beta.9 with no `VersionOverride`, self-install `bin/dev`, and re-run
  the five gates.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: kanban/in-progress/457-take-timewarpnuru-400-beta8/task.md
- Description: The Goal restatement, Results, How to validate, and Checklist still describe
  beta.8 and the RCS1194 suppression.
- Suggestion: Rewrite them for beta.9 with no suppression, and record the gate results.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer). Round-1 M1 keeps its ID.
