# Round 2 — merged findings
**Date:** 2026-10-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: skills/tw-deploy/SKILL.md:287
- Description: The SigNoz subsection split the "Rules for new code:" list.
- Source: general (round 1)
- Disposition notes: 53e4f4f24 moved the yarp rule back into the list. The subsection follows it, before Secrets and parameters.

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/aspire/aspire-tests/{compose,kubernetes,aca,aca-neon}-publish-tests.cs
- Description: The publish `signoz` absence checks passed trivially on fixtures with no SigNoz URL.
- Source: general (round 1)
- Disposition notes: 53e4f4f24 passes `--Observability:SigNoz:Url=https://observability.example` to every in-proc publish. Re-run in this worktree: `Publish_Given_` 47/47 passed.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:107
- Description: The SigNoz Design paragraph split PublicUrl from its PublicOrigin continuation.
- Source: general (round 1)
- Disposition notes: 53e4f4f24 moved the PublicOrigin sentences up. SigNoz now sits before public-host forwarding.

## Duplicates / conflicts

- None. No new findings on the fix delta.
