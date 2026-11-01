# Round 2 — merged findings
**Date:** 2026-11-01
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 1 | 1 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: kanban/in-progress/257-take-microsoftopenapi-3x-when-aspnetcoreopenapi-allows-it/task.md
- Description: (round 1) Uncommitted diff, no Results, and gates not recorded.
- Source: general
- Disposition notes: Product committed in e06c648f7. Results and gates recorded in 52efa0805,
  and the checklist is ticked. `./bin/dev build` re-run by the reviewer: 0 warnings, 0 errors.

### M2 — Severity: suggestion — Status: fixed
- File: Directory.Packages.props:139; source/foundation/foundation-server/foundation-server.csproj:10-12
- Description: (round 1) The pin comments still cited the 2.0.0 / GHSA lift.
- Source: general
- Disposition notes: Both comments now describe the RC3 and 3.10.2 → 3.11.0 lifts.

### M3 — Severity: suggestion — Status: open
- File: Directory.Packages.props:139
- Description: The comment rewrite deleted RC train facts that are still true and are recorded
  nowhere else: the shared-framework pins match SDK RC3, EF and Npgsql must move together
  (moving EF alone fails restore with NU1608/NU1107), and the Extensions monthly packages
  (10.11.0) and System.ServiceModel.Primitives (10.1.0) have no 11.x.
- Suggestion: Restore those facts as short comments next to the pins they explain, without the
  task-by-task history.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: tests/container-apps/web/web-server-integration-tests/features/identity/fast-endpoint-auth-schemes-tests.cs:6
- Description: The Design region names FastEndpoints 8.3 and ASP.NET Core 10. The suite now runs
  on FastEndpoints 9 and ASP.NET Core 11 RC3.
- Suggestion: Drop the version numbers or update them.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None.
