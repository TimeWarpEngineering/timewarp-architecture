# Round 2 — merged findings
**Date:** 2026-09-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 1 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: .github/workflows/workflow.yml
- Description: Unpinned `--prerelease` plus nuget.org in the install configfile could soft-fallback to `1.0.0-beta.15` and no-op later audit checks.
- Suggestion: Fail-closed GitHub Packages install.
- Source: general (round 1); re-verified round 2
- Disposition notes: Fixed — github-only configfile; post-install version gate refuses nuget.org-vintage beta.15 or earlier; same-step PATH export. Round 2 confirmed globs refuse beta.0–15 and allow `1.0.0-beta.29(+hash)`.

### M2 — Severity: suggestion — Status: wontfix
- File: .github/workflows/workflow.yml path filters
- Description: `kanban/**` is omitted, so kanban-only runfile PRs never start `repo-audit`.
- Suggestion: Add `kanban/**` or document intentional exclusion.
- Source: general (round 1); re-verified round 2
- Disposition notes: Wontfix — CI guard is for co-located `*-tests.cs` under `source/**` (already in path filters). Adding `kanban/**` would start this workflow on every kanban-only PR. Round 2 agreed.

## Duplicates / conflicts

- None. Prior stable M# IDs carried forward. No new findings.
