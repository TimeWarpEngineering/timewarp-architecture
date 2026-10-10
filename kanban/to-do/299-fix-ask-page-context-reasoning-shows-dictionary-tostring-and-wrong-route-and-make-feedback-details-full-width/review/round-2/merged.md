# Round 2 — merged findings
**Date:** 2026-10-10
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/page-agent-route.cs
- Description: An observed route always beat the live NavigationManager, so it could go stale.
- Source: general (round 1)
- Disposition notes: Re-verified. Reads are live. Covered by Route_Follows_The_Observed_Manager_And_Keeps_The_Path_For_A_Stuck_One.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/components/App.razor
- Description: timeWarpPagePath was absolute while FromNavigation is base-relative.
- Source: general (round 1)
- Disposition notes: Re-verified. The script removes the document.baseURI path.

## Duplicates / conflicts

- None. No new findings in round 2.
