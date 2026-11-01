# Round 3 — merged findings
**Date:** 2026-11-01
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- Source: general (round 1)
- Disposition notes: fixed in round 2 (e06c648f7, 52efa0805).

### M2 — Severity: suggestion — Status: fixed
- Source: general (round 1)
- Disposition notes: fixed in round 2.

### M3 — Severity: suggestion — Status: fixed
- File: Directory.Packages.props:63-64, 70, 90, 107, 122-126, 166-167, 206-207, 246
- Description: (round 2) The RC-train facts were deleted with the old pin comment.
- Source: general
- Disposition notes: 925b61eb3 restores the shared-framework/SDK RC3 match, the EF+Npgsql
  move-together rule (NU1608/NU1107), and the 10.11.0 / 10.1.0 "no 11.x" notes as short comments
  beside the pins, with no task history.

### M4 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/features/identity/fast-endpoint-auth-schemes-tests.cs:6
- Description: (round 2) The Design region named FastEndpoints 8.3 and ASP.NET Core 10.
- Source: general
- Disposition notes: 925b61eb3 removes both version numbers.

## Duplicates / conflicts

- None. No new findings in round 3.
