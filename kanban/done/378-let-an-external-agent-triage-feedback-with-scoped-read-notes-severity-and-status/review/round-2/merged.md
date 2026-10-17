# Round 2 — merged findings
**Date:** 2026-10-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/feedback/download-feedback-attachment/download-feedback-attachment-handler-application.cs:93; source/container-apps/web/features/feedback/feedback-owner-names-application.cs:83
- Description: A role-assigned agent principal inherited human grants on download and owner names.
- Source: general (round 1)
- Disposition notes: Fixed in f6384d665. The scheme is chosen from `IAgentPermissionScopeSource`, which is backed by the authenticated identity's `AuthenticationType`. Verified by runfile and HTTP tests the reviewer re-ran.

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/features/feedback/admin-feedback-authorization-tests.cs:283
- Description: No HTTP test drove a real agent bearer token through the new endpoints.
- Source: general (round 1)
- Disposition notes: Fixed in f6384d665. `Scoped_Given_Agent_Bearer_Tokens` and `Role_Assigned_Agent_Should_Not_Inherit_Human_Grants` pass, 9/9 in the class.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminItemPage.razor:177
- Description: Status and note sections were gated on `admin.feedback.manage`, not their endpoint policies.
- Source: general (round 1)
- Disposition notes: Fixed in f6384d665. Status is gated on `feedback.status`, note on `feedback.note`, and reply stays on `admin.feedback.manage`.

## New findings

None.

## Duplicates / conflicts

- None. Single reviewer.
