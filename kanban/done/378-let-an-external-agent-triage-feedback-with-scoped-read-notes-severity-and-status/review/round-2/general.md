# Round 2 — general
**Date:** 2026-10-17
**Scope reviewed:** fix delta `e3a18ed06..f6384d665` (commit "fix: keep agent feedback grants on the token scheme"), re-verifying round-1 M1–M3

## Summary

The fix pass routes the cross-owner download check and `FeedbackOwnerNames` through
`IAgentPermissionScopeSource`. That port returns scopes only when `HttpContext.User`'s primary
identity was authenticated by the agent-token scheme for the same principal
(`AgentCallerContext` checks `AuthenticationType`). An agent-token caller is therefore evaluated
on `AgentToken` only, and a human session on the two human schemes only. Real bearer-token HTTP
tests cover the scope matrix and a role-assigned agent principal. The detail page gates each
section on its endpoint's policy; the SPA registers a claim policy for every `PermissionIds` id,
so the new gates resolve. No new defects found in the delta.

## Re-verification

- M1 — fixed. `download-feedback-attachment-handler-application.cs:93` and
  `feedback-owner-names-application.cs:83` select the scheme from the authenticated caller.
  Covered by `Download_Should_IgnoreHumanRolesWhenTheCallerIsAnAgentToken`,
  `AgentToken_WithAdministratorRole_Should_NotReadOwnerNames`, and
  `Role_Assigned_Agent_Should_Not_Inherit_Human_Grants` (HTTP).
- M2 — fixed. `Scoped_Given_Agent_Bearer_Tokens` issues real tokens through
  `CredentialCeremonyHelpers.RegisterAgentKeyAndIssueTokenAsync`. It checks read → 200 list and
  403 on note, severity, and status. Each write scope gets 403 on the list and the other writes
  and 200 on its own write. Reply is 401 for every agent token, including one with all four scopes.
- M3 — fixed. `FeedbackAdminItemPage.razor:177` / `:209` gate on `FeedbackStatus` /
  `FeedbackNote`; reply stays on `AdminFeedbackManage` (`:194`); severity on `FeedbackSeverity` (`:159`).

## Gates re-run by the reviewer

- `feedback-attachment-tests.cs` 22/22
- `feedback-triage-tests.cs` 42/42
- `web-server-integration-tests --filter-class AdminFeedbackAuthorization` 9/9 (Release)

## Issues

None.
