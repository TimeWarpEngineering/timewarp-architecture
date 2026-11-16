# Round 2 — merged findings
**Date:** 2026-11-16
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: Directory.Packages.props:158
- Description: Round 1: the rewritten FastEndpoints comment still said `Microsoft.AspNetCore.OpenApi` 11.0.0 allows `Microsoft.OpenApi` `[3.10.2, 4.0.0)` and that foundation-server lifts a 3.10.2 floor. `foundation-server.csproj` still said the OpenApi reference lifts an rc.2 floor to the RC3 train. The 11.0.0 nuspec depends on `Microsoft.OpenApi` `[3.11.0, 4.0.0)`, and FastEndpoints.OpenApi 9.0.0 net11.0 depends on `Microsoft.AspNetCore.OpenApi` 11.0.0.
- Suggestion: State those GA facts in both comments and keep the direct references.
- Source: general
- Disposition notes: Fixed in `15da1a135`. `Directory.Packages.props:158` and `foundation-server.csproj:10` and `:12` state the GA facts. `Microsoft.OpenApi` is `3.11.0` at `Directory.Packages.props:165`. Direct references remain. Round 2 re-read that delta; no new findings. The fix-pass `## Results` record (`./bin/dev build`, `./bin/dev test -q`, `ganda repo audit`) covers the comment-only edit. Gates were not re-run.

## Duplicates / conflicts

- Single reviewer. M1 carried from round 1; nothing new to collapse.
