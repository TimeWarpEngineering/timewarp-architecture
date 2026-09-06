# Round 1 — merged findings
**Date:** 2026-09-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: kanban/in-progress/161-research-should-credential-management-contracts-declare-authenticationschemes/task.md:109-119
- Description: The coverage audit section is titled “In-proc HostGraph (`web-server-integration-tests`)” and marks InvokeMeteredCapability anonymous as `401`. That anonymous case lives in the co-located Jaribu runfile `source/container-apps/web/features/metered-capability/invoke-metered-capability/invoke-metered-capability-tests.cs` (`Unauthorized_Given_No_Bearer`), not under `tests/.../web-server-integration-tests`. Bearer coverage for that route in the suite project is via `program-104-sunny-paths-tests.cs`; cookie-isolation gap remains correctly noted.
- Suggestion: Broaden the section label to “in-proc HostGraph” (or footnote the co-located runfile) so the anonymous cell is not attributed to `web-server-integration-tests` alone.
- Source: general
- Disposition notes: Coverage intro now names both the suite project and the co-located InvokeMetered runfile for the anonymous 401 cell.

## Duplicates / conflicts

- None (single reviewer).
