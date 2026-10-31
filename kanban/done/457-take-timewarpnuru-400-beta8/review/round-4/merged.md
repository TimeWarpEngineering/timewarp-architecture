# Round 4 — merged findings
**Date:** 2026-10-31
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 3 | 2 | 0 |
| suggestion | 1 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/Directory.Build.props:13
- Description: `RCS1194` in the dev CLI `NoWarn`.
- Source: general (carried)
- Disposition notes: Fixed in `3a9888364`. The NoWarn is still absent.

### M2 — Severity: bug — Status: fixed
- File: Directory.Packages.props:18
- Description: The pins were at beta.8 and the task needed beta.9.
- Source: general (carried)
- Disposition notes: The pins reached beta.9. The task has since been retargeted to beta.10, which is M6.

### M3 — Severity: suggestion — Status: fixed
- File: kanban/in-progress/457-take-timewarpnuru-400-beta8/task.md
- Description: The task text described beta.8.
- Source: general (carried)
- Disposition notes: Fixed in `80fb4833a`. The beta.10 retarget is M7.

### M4 — Severity: bug — Status: open
- File: tools/dev-cli/Directory.Build.targets:7
- Description: `OmitPackagedRepoConfigService` is still present. On 2026-10-31 the operator chose to
  wait for Nuru, and the Requirements now say to delete this step.
- Suggestion: Delete the target. Delete the file too if only the parent import is left.
- Source: general (carried from round 3). Raised from the round-3 guard request to a delete, per the
  operator decision.
- Disposition notes:

### M5 — Severity: bug — Status: open
- File: tools/dev-cli/services/repo-config-service.cs:1
- Description: The local copy of the DevCli `RepoConfigService` is still present. Nuru 4.0.0-beta.10
  (task 562) fixes CA2263 upstream, and the Requirements say to delete this copy. Raised from
  suggestion to bug because the operator decision turned it into a stated requirement.
- Suggestion: Delete the file. Confirm the runfile builds clean on beta.10.
- Source: general (carried from round 3)
- Disposition notes:

### M6 — Severity: bug — Status: open
- File: Directory.Packages.props:18-19
- Description: Both Nuru pins are `4.0.0-beta.9`. The Requirements need `4.0.0-beta.10`, and that
  version is published (nuget.org flat container HTTP 200 for both packages).
- Suggestion: Bump both pins to beta.10. Rebuild `bin/dev`. Run `ganda repo audit`, `dev build`,
  `dev test`, `dev template-smoke`, and `dev check-version`.
- Source: general
- Disposition notes:

### M7 — Severity: suggestion — Status: open
- File: kanban/in-progress/457-take-timewarpnuru-400-beta8/task.md
- Description: Goal, Checklist, Results, and How to validate still describe beta.9 and the local copy.
- Suggestion: Rewrite them for beta.10 with no local copy.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer). Round 4 `general.md` Issue 2 covers both M4 and M5.
