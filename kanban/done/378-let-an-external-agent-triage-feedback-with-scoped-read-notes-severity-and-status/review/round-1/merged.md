# Round 1 — merged findings
**Date:** 2026-10-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/features/feedback/download-feedback-attachment/download-feedback-attachment-handler-application.cs:86; source/container-apps/web/features/feedback/feedback-owner-names-application.cs:65
- Description: The download handler and `FeedbackOwnerNames` evaluate the caller principal under the human-session schemes no matter which scheme authenticated the request. An agent principal with a role assignment (`SetPrincipalRoles` has no agent guard) therefore inherits human grants. A `feedback:note`-only token downloads attachments through `admin.feedback.read`, and a `feedback:read` token sees owner display names through `admin.profiles.read`. That breaks "token scopes are the only grants" and "owner display name stays null for this caller".
- Suggestion: evaluate only under the request's authenticated scheme (agent-token caller → `AgentToken` only, human session → human schemes only). Use `IAgentCallerContext` / `IAgentPermissionScopeSource` for this. Add a test with a role-assigned agent principal.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-server-integration-tests/features/feedback/admin-feedback-authorization-tests.cs
- Description: No HTTP test drives a real agent bearer token through the new endpoints. The scope matrix is checked only at the seed and attribute-reflection level.
- Suggestion: add an agent-token HTTP test using `CredentialCeremonyHelpers.RegisterAgentKeyAndIssueTokenAsync`. It should cover: `feedback:read` lists (200) and is refused note, severity, and status (403); each write scope is refused the other writes; `ReplyToFeedback` refuses agent tokens.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminItemPage.razor:177
- Description: "Change status" and "Internal note" are gated on `admin.feedback.manage`, but their endpoints now require `feedback.status` and `feedback.note`. Those ids are not protected core, so the role editor can make the UI gate and the server policy disagree.
- Suggestion: gate each section on its endpoint's policy. Reply stays on `admin.feedback.manage`.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Single reviewer.
