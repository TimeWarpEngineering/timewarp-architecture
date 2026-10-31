# Round 5 — merged findings
**Date:** 2026-10-31
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 5 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/Directory.Build.props:13
- Description: `RCS1194` in the dev CLI `NoWarn`.
- Source: general (carried)
- Disposition notes: Fixed in `3a9888364`. It is still absent at `ba9795f43`.

### M2 — Severity: bug — Status: fixed
- File: Directory.Packages.props:18
- Description: The pins were at beta.8.
- Source: general (carried)
- Disposition notes: Superseded by M6, which is fixed.

### M3 — Severity: suggestion — Status: fixed
- File: kanban/in-progress/457-take-timewarpnuru-400-beta8/task.md
- Description: The task text described beta.8.
- Source: general (carried)
- Disposition notes: Fixed in `80fb4833a`.

### M4 — Severity: bug — Status: fixed
- File: tools/dev-cli/Directory.Build.targets
- Description: `OmitPackagedRepoConfigService` was still present.
- Source: general (carried)
- Disposition notes: Fixed in `ba9795f43`, which deletes the file. Only the parent import was left
  after the target was removed. No tracked file still references the target.

### M5 — Severity: bug — Status: fixed
- File: tools/dev-cli/services/repo-config-service.cs
- Description: There was still a local copy of DevCli `RepoConfigService`.
- Source: general (carried)
- Disposition notes: Fixed in `ba9795f43`, which deletes the file. DevCli beta.10
  `content/any/services/repo-config-service.cs:63` uses `JsonContext.RepoConfig`.
  `dotnet build tools/dev-cli/dev.cs --no-incremental` reports 0 warnings and 0 errors, with no new
  `NoWarn`.

### M6 — Severity: bug — Status: fixed
- File: Directory.Packages.props:18-19
- Description: The pins were at beta.9, and the task needs beta.10.
- Source: general (carried)
- Disposition notes: Fixed in `ba9795f43`. Both pins are `4.0.0-beta.10`, with no `VersionOverride`.
  `dev build` from source and the agent-identity runfile each report 0 warnings and 0 errors.

### M7 — Severity: suggestion — Status: fixed
- File: kanban/in-progress/457-take-timewarpnuru-400-beta8/task.md
- Description: Goal, Checklist, Results, and How to validate described beta.9.
- Source: general (carried)
- Disposition notes: Fixed in `ba9795f43`. All four sections describe beta.10 with no local copy.

### M8 — Severity: nit — Status: fixed
- File: kanban/in-progress/457-take-timewarpnuru-400-beta8/task.md:90
- Description: There was a stray fragment in the `## Review` ledger and no blank line before `## Notes`.
- Source: general
- Disposition notes: The review oracle fixed it in the kitchen this round. No product change was needed.

## Duplicates / conflicts

- None.
