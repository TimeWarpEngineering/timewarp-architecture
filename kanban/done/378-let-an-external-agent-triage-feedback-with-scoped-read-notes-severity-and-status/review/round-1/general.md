# Round 1 — general
**Date:** 2026-10-17
**Scope reviewed:** branch vs `master` (d1b316dc7). Product code, migration, SPA, and tests. The migration Designer was skimmed.

## Summary

The change does what the restatement and the task ask: there are four `feedback.*` permissions, none of them `admin.*`. They are on the agent scopes and the Administrator seed, and the migration grants them. Severity is a closed enum with a monotonic `SetSeverity`, a filer-invisible `SeverityChange` entry, and an allow-list `IsVisibleToFiler`. Status and severity invariants are separate. `ListFeedback` has a nullable `Severity` filter, and the admin list and detail have severity UI through `FeedbackAdminState`. Re-run here: triage runfile 41/41, attachment runfile 21/21, permission evaluator 19/19.

The main risk is in authorization. Two handlers that agent tokens can now reach check the caller principal against the human-session schemes no matter how the request authenticated. An agent principal that has a role assignment therefore gains human role grants, which breaks the task's "token scopes are the only grants" rule. No test drives a real agent bearer token through the new endpoints.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/features/feedback/download-feedback-attachment/download-feedback-attachment-handler-application.cs:86 (also source/container-apps/web/features/feedback/feedback-owner-names-application.cs:65)
- Description: `HasCrossOwnerReadAsync` checks `admin.feedback.read` and `feedback.read` on `IdentitySession` and `MockIdentitySession` for every caller before it checks `AgentToken`. `PermissionEvaluator` expands a human scheme through `EffectiveRolesResolver`, which is keyed only on principal id. Every principal gets Member, and `SetPrincipalRoles` has no `PrincipalKind.Agent` guard, so an admin can give an agent principal Administrator (the bootstrap list can do the same). That agent's bearer token, even one holding only `feedback:note`, then passes the human-scheme `admin.feedback.read` check and downloads any linked attachment. `FeedbackOwnerNames.CanReadProfilesAsync` has the same flaw. Before this task no agent token reached it. Now a `feedback:read` token on such a principal gets `OwnerDisplayName` filled on `ListFeedback`, `GetFeedbackForAdmin`, and every mutation response. The task says "Agents do not inherit human roles", "An agent without `feedback:read` cannot [download]", and "Owner display name stays null for this caller".
- Suggestion: evaluate permissions only under the scheme the request actually authenticated with. `IAgentCallerContext` / `IAgentPermissionScopeSource` already know whether the caller is an agent-token caller. For an agent-token request, check only `AgentToken`, and check the human schemes only for a human-session request. Apply this to both the download handler and `FeedbackOwnerNames`. Add a test: an agent principal that has an Administrator role assignment and a `feedback:note`-only token gets 404 on download, and with a `feedback:read` token gets a null `OwnerDisplayName`.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-server-integration-tests/features/feedback/admin-feedback-authorization-tests.cs
- Description: The scope rules are tested only at the seed level (`AgentScopePermissionSeed.Expand`) and by reading the `EndpointAuthorize` scheme strings with reflection. No test sends a real agent bearer token through the HTTP pipeline. Nothing checks that a `feedback:read` token gets 200 on `ListFeedback`, that it gets 403 on note, severity, and status, that a write scope gets 403 on the other writes, or that `ReplyToFeedback` refuses an agent token. Delivering exactly that is the purpose of the task. `CredentialCeremonyHelpers.RegisterAgentKeyAndIssueTokenAsync` already issues scoped tokens in this test project, so the test is cheap to add.
- Suggestion: add an HTTP agent-token test class that covers the token matrix from the task's Tests section. Include issuance accepting the four new scopes.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminItemPage.razor:177
- Description: "Set severity" is gated on `feedback.severity`, which matches its endpoint. "Change status" and "Internal note" are still gated on `admin.feedback.manage`, but their endpoints now require `feedback.status` and `feedback.note`. The `feedback.*` ids are not protected core, so the role editor can remove `feedback.status` from Administrator. The button then still shows and gets a 403, and a role holding `feedback.status` without `admin.feedback.manage` never sees the control. The repo rule is that two things that must agree should not depend on memory.
- Suggestion: gate "Change status" on `PermissionIds.FeedbackStatus` and "Internal note" on `PermissionIds.FeedbackNote`. Keep "Reply to filer" on `admin.feedback.manage`. The page-level `admin.feedback.read` gate can stay, as the task allows.
- Status: open
