# Let an external agent triage feedback with scoped read, notes, severity, and status

## Goal (implementer restatement)

An agent principal, authenticated only by a bearer token, can pull the feedback board and triage items. The token's scopes are the only grants. Assigning that principal a human role does not add grants on an agent-token request.

Four scopes each expand to one non-admin permission: `feedback:read` → `feedback.read` (list, open, thread including internal notes, attachment download), `feedback:note` → `feedback.note` (append an internal note), `feedback:severity` → `feedback.severity` (set severity), and `feedback:status` → `feedback.status` (change status under the existing rules, including the filer email on Planned, Completed, and Declined). Administrator receives all four beside the existing `admin.feedback.read` and `admin.feedback.manage`. Member and Operator do not. Reply to the filer stays on `admin.feedback.manage` and a human session. The agent does not receive `admin.profiles.read`, so the owner display name stays empty and only `OwnerPrincipalId` is visible.

Severity is a closed enum (`Unset`, `Informational`, `Low`, `Medium`, `High`, `Critical`), default `Unset` at file time and for existing rows. The filer never sends it and never sees it. Setting severity is the review: a defined value other than the current one, never back to `Unset`, recorded as a filer-invisible `SeverityChange` activity. It does not change status, and a status change does not set severity. A severity change sends no email. `ListFeedback` treats an omitted severity as every item and `Severity=Unset` as only the unreviewed queue.

The admin list and detail pages show severity, filter to `Unset`, and set it through `FeedbackAdminState`. Filer list and permalink do not render it. On the detail page, change-status is gated on `feedback.status`, the internal note on `feedback.note`, and the reply on `admin.feedback.manage`.

Done when the domain, scope seed, endpoint policies, list filter, admin UI, and the tests named in Requirements all hold, `AgentScopeSeed_Should_NotIntersectAdminPermissions` still passes, and the round-1 findings are closed: a role-assigned agent is evaluated only under the agent-token scheme (a `feedback:note` token cannot download through `admin.feedback.read`, and a `feedback:read` token does not see owner names through `admin.profiles.read`); an HTTP test drives real bearer tokens through list, note, severity, status, and reply; and the detail page gates each section on that endpoint's policy.

## Description

A repo agent should pull feedback and triage it. It does not get a push, a webhook, or a kanban card. It is an agent principal with a bearer token. The token's scopes are the only grants. Agents do not inherit human roles (`PermissionEvaluator` expands agent tokens only through `AgentScopePermissionSeed`).

The agent can read the board, append internal notes, set a filer-invisible severity, and change status. Setting severity is the review. It does not move status. Status changes keep today's rules, including the filer email on Planned, Completed, and Declined.

Steve, 2026-10-16, after a design pass on the feedback feature. Pull over push. Severity is the review marker, hidden from the filer, and monotonic (nothing returns to unset).

## Requirements

### Scopes and permissions

Add four agent scopes and four permissions. The permissions are not `admin.*`. `AgentScopePermissionSeed` must not grant `admin.*` (pinned by `AgentScopeSeed_Should_NotIntersectAdminPermissions`). Do not put the agent in the Administrator role.

| Scope | Expands to | Allows |
| --- | --- | --- |
| `feedback:read` | `feedback.read` | List and open every item, including body, thread (internal notes included), and attachment download. |
| `feedback:note` | `feedback.note` | Append an internal note. |
| `feedback:severity` | `feedback.severity` | Set severity. |
| `feedback:status` | `feedback.status` | Change status, same rules as an admin. |

Follow the credential split: one permission, two grant paths. `AgentScopes` plus `AgentScopePermissionSeed` for the token. `RolePermissionSeed` grants all four to **Administrator** only, beside the existing `admin.feedback.read` / `admin.feedback.manage`. Do not grant them to Member or Operator. They are not protected core.

Do not delete `admin.feedback.read` or `admin.feedback.manage`. The SPA admin gate can keep using them. The API capabilities the agent shares are the new ids.

`ReplyToFeedback` stays `admin.feedback.manage` and human-session only. The agent must not email or reply to the filer except by a status change, which already emails on Planned, Completed, and Declined. Do not grant `admin.profiles.read`. Owner display name stays null for this caller. The agent sees `OwnerPrincipalId` only.

### Endpoints

Add `AuthenticationSchemeNames.AgentToken` next to the human session schemes. Switch the policy to the new permission (Administrator holds it, so the admin UI keeps working):

- `ListFeedback`, `GetFeedbackForAdmin`: `feedback.read`
- `AddFeedbackNote`: `feedback.note`
- `ChangeFeedbackStatus`: `feedback.status`
- New `SetFeedbackSeverity`: `PUT api/admin/feedback/{id}/severity`, policy `feedback.severity`. Body is the severity. `Unset` is 400. The current value is 409. Success returns the admin view. No email.

`DownloadFeedbackAttachment` has no policy today. The handler allows the owner or `admin.feedback.read`. Also allow `feedback.read`, and add the agent-token scheme. A filer can still download their own files. An agent without `feedback:read` cannot.

`ListFeedback` is `[ReferenceableResource("feedback", …)]`. The Ask picker authorizes that policy. After the policy rename, `@feedback` still appears for an Administrator because the role holds `feedback.read`. Do not make the in-app Ask agent a triage bot.

`ListFeedback.Query` gains an optional `FeedbackSeverity? Severity`. Omitted means every severity. `Unset` is a real filter (the agent's queue), not "no filter". A non-nullable enum would collapse those. Use a nullable query value and test both.

### Severity

Closed enum, domain and contracts, member-for-member, mapped by name (same rule as `FeedbackKind` / `FeedbackStatus`):

- `Unset` (default at `File`)
- `Informational` (thanks, duplicate, not a defect)
- `Low`
- `Medium`
- `High`
- `Critical`

The filer never sends it and never sees it. `GetFeedback` and `ListMyFeedback` omit the property. Admin list, admin detail, and the agent view include it.

Named mutation on `FeedbackItem`, same shape as `ChangeStatus`:

- Defined value, not `Unset`, not the current value.
- Records a `SeverityChange` activity (from, to, who, when). No body.
- Does not change `Status`. A status change does not set severity.

`IsVisibleToFiler` is today `Kind != InternalNote`, so a new kind would leak to the filer. Make it an allow-list: `StatusChange`, `AdminReply`, `FilerReply`. `InternalNote` and `SeverityChange` stay hidden. `FeedbackContractMap.FilerThread` must drop severity entries.

Do not overload the status columns. A severity entry has from/to severity and null status and body. A status entry has from/to status and null severity. Invariants cover both.

Existing rows migrate to `Unset`. That means "not reviewed yet", which is honest for items filed before this task.

### Admin UI

`/Admin/Feedback` shows severity and can filter to `Unset`. `/Admin/Feedback/{id}` can set it. Both go through a `FeedbackAdminState` action (the page does not call the endpoint itself). The filer's list and permalink do not render it.

### Tests

- Domain: set severity, refuse `Unset`, refuse the current value, status unchanged, filer projection omits the entry and the property.
- Scope seed: each new scope expands to its one permission and to nothing in `AdminPermissions`. A `feedback:read` token does not note, set severity, or change status. Each write scope refuses the other writes and refuses `ReplyToFeedback`.
- List filter: omitted severity returns every item; `Severity=Unset` returns only the unreviewed.
- Human Administrator still lists, notes, sets severity, and changes status. A Completed change still reports the filer email. A severity change never does.
- Filer JSON for get and list-mine has no severity field. Filer download of their own attachment still works.

## Checklist

- [x] `AgentScopes` + `AgentScopePermissionSeed` + `PermissionIds` for the four scopes, not `admin.*`
- [x] Administrator role seed gains the four permissions; Member and Operator do not
- [x] Endpoints accept agent tokens on the new policies; reply stays human-only
- [x] Attachment download allows `feedback.read` without becoming world-readable
- [x] `FeedbackSeverity` on the item, migration default `Unset`, filer contracts omit it
- [x] `SetFeedbackSeverity` mutation, activity kind, filer-invisible, no status side effect, no email
- [x] `ListFeedback` filters `Unset` as a value distinct from omitted
- [x] Admin list and detail show and set severity; filer pages do not
- [x] Tests listed above
- [x] `AgentScopeSeed_Should_NotIntersectAdminPermissions` still passes
- [x] Cross-owner download and owner names follow the authenticated scheme
- [x] Agent bearer HTTP matrix, including reply refused on the human schemes
- [x] Detail page gates status on `feedback.status` and notes on `feedback.note`

## Review

- Round 1: fix pass implemented M1, M2, and M3. Round 2 has not run.

## Notes

### Decisions (2026-10-16)

1. **Pull, not push.** No webhook, outbox, or doorbell in this task. The item stays the system of record. The agent lists `Severity=Unset` when it wants work.
2. **Severity completes the review.** The queue is unset severity, not `Status=New`. A human may already have moved the status.
3. **The two fields do not couple.** Setting severity does not move status. Moving status does not set severity. `Planned` / `Completed` / `Declined` still email the filer, including when the caller is the agent. That is a deliberate status call, not a side effect of review.
4. **Monotonic review.** Nothing returns to `Unset`, same reason nothing returns to `New`. A later change (`Low` → `High`) appends another hidden activity entry.
5. **Not a full admin.** Read does not include profile names or filer email. Notes are internal. Replies to the filer stay on `admin.feedback.manage`.

### Not in scope

- Creating a kanban task or claiming an item onto the board (to-do 331 is that human workflow: developer claim, email to Amina, notification on file). Do not implement 331 here.
- Kind reclassification, assignee, tags, duplicate merge, admin delete (task 314 decision 5/6).
- The in-app Ask agent triaging the board.
- A push when an item is filed.

### Code grounding

- Aggregate and the deferred assignee/tags/reclassification note: `source/container-apps/web/features/feedback/feedback-item-domain.cs`.
- Status email rule: `feedback-status-rules-contracts.cs` (`NotifiesFiler` is Planned, Completed, Declined). `UnderReview` and `InProgress` do not email.
- Visibility: `feedback-activity-domain.cs` (`IsVisibleToFiler`). Filer thread: `feedback-contract-map-application.cs`.
- Admin list policy is human-session only today: `list-feedback/list-feedback-contracts.cs` (`admin.feedback.read`).
- Note and status policies: `add-feedback-note-contracts.cs`, `change-feedback-status-contracts.cs`.
- Download is scheme-only; the handler does the owner-or-admin check: `download-feedback-attachment-contracts.cs`.
- Agent grants: `source/libraries/timewarp-identity/tokens/agent-scopes.cs`, `platform/authorization/agent-scope-permission-seed-application.cs`, `permission-evaluator-application.cs`. Known scopes are rejected at issuance when absent from `AgentScopes`.
- Permission catalog: `features/authorization/permission-ids-contracts.cs`. `feedback.file.self` is already documented as not an agent scope. Leave it that way.
- Admin UI: `web-spa/features/admin/feedback/`.

## Session

- Created: 248635 (2026-10-16)
- Implementation (2026-10-17): scoped pull triage. Four `feedback.*` permissions (not `admin.*`) on the agent token and on Administrator only. Severity is the review marker, default `Unset`, filer-invisible, and independent of status. Migration `20261017135945_AddFeedbackSeverity` adds the columns and the Administrator role grants. The explicit `Microsoft.Extensions.Options` package directive was removed from `permission-evaluator-tests.cs` because NU1510 treats it as already available through the project reference.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-17T14:12:40Z
- Review oracle round 1 (2026-10-17): claude (claude-opus-5-5), session not reported. Effort 3, roster general. Found 1 bug (M1) and 2 suggestions (M2, M3), all open. A fix pass is requested. See `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-17T14:16:30Z
- Fix pass (2026-10-17): implement-fix-2. Download and owner-name checks use `IAgentPermissionScopeSource` so an agent-token caller is evaluated only on the agent-token scheme. HTTP tests drive real bearer tokens. The detail page gates status and notes on their endpoint policies. Reply stays on `admin.feedback.manage`.

## Results

An agent principal triages by listing `Severity=Unset`, appending an internal note, setting severity, or changing status. Each of those is its own scope. Setting severity does not move status and does not email. A Completed status change still emails the filer. The filer JSON has no severity field. `admin.feedback.read` and `admin.feedback.manage` stay, and reply stays on a human session.

A role assigned to the agent principal does not add grants on an agent-token request. Download and owner display names ask `IAgentPermissionScopeSource`: a non-null scope list means the agent-token scheme only, and null means the human session schemes only. A `feedback:note` token whose principal is Administrator cannot download another filer's attachment. A `feedback:read` token with that same role lists the item and leaves `OwnerDisplayName` null. On the detail page, change-status is `feedback.status`, the internal note is `feedback.note`, and reply stays `admin.feedback.manage`.

`ganda repo audit`: 32 passed, 0 failed. Release builds of the SPA, server, and infrastructure test projects compiled with 0 warnings.

The admin detail page was not clicked in a browser. The Razor compile covers the three policy gates. An Administrator holds all three permissions, so the sections still render for that role. `dev template-smoke` was not run.

### Review

- Round 1 (effort 3, roster general). Counts at review time: bug 1 open, suggestion 2 open, nit 0. The fix pass implements all three. Round 2 has not run.
- M1: download and `FeedbackOwnerNames` evaluate only the authenticated scheme. A role-assigned agent is covered by the attachment runfile and by the HTTP test.
- M2: `Scoped_Given_Agent_Bearer_Tokens` drives real bearer tokens. `feedback:read` lists (200) and is refused the three writes (403). Each write scope is refused the list and the other writes (403) and succeeds on its own write (200). Reply is 401 for every agent token, including one that holds all four feedback scopes, because the endpoint schemes are the human sessions.
- M3: `FeedbackAdminItemPage` gates status and notes on `PermissionIds.FeedbackStatus` and `PermissionIds.FeedbackNote`. Reply stays on `PermissionIds.AdminFeedbackManage`.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

### How to validate

**Smoke:**

```bash
dotnet run source/container-apps/web/features/feedback/feedback-triage-tests.cs
dotnet run source/container-apps/web/features/feedback/feedback-attachment-tests.cs
dotnet run source/container-apps/web/platform/authorization/permission-evaluator-tests.cs
dotnet run source/container-apps/web/features/authorization/permission-ids-tests.cs
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class FeedbackAdminActions && dotnet test -c Release --no-build -- --filter-class AskReference
cd ../web-server-integration-tests && dotnet test -c Release -- --filter-class AdminFeedbackAuthorization
cd ../web-infrastructure-tests && dotnet test -c Release -- --filter-class Feedback_Postgres
```

Run each `dotnet test` from that project directory so the local Microsoft.Testing.Platform runner is selected. Postgres needs Docker.

**Expect:**

- Triage runfile 42/42, including omitted severity returning every item, `Severity=Unset` returning only the unreviewed, a severity change sending no mail, a Completed change still mailing the filer, filer JSON with no severity field, each new scope expanding to one permission and nothing in `AdminPermissions`, and an Administrator-role agent token leaving the owner display name null.
- Attachment runfile 22/22. The owner still downloads their own file. An agent with `feedback.read` can download after the file is linked. An agent with only `feedback.note` cannot. A human with `admin.feedback.read` still can. An unlinked draft stays owner-only. An Administrator-role principal authenticated as an agent with only `feedback:note` gets 404, and the same role on a human session still downloads.
- Permission evaluator 19/19, including `AgentScopeSeed_Should_NotIntersectAdminPermissions`. Permission catalog 8/8. The four new ids are not `admin.*` and are not protected core. This fix pass did not re-run those two runfiles.
- SPA state 10/10. The severity filter reaches `ListFeedback.Query`, and set-severity stores the returned item. Ask references 9/9. `@feedback` stays on `feedback.read`, which Administrator holds. This fix pass did not re-run the SPA suite. The SPA Release build is 0 warnings.
- HTTP admin feedback 9/9. An Administrator lists, notes, sets severity, and changes status. A session without `feedback.read` is forbidden on the list. Setting severity without `feedback.severity` is forbidden. `feedback:read` lists and is refused note, severity, and status. Each write scope is refused the other writes. Reply is 401 for an agent token. A role-assigned `feedback:note` token cannot download, and a role-assigned `feedback:read` token sees no owner display name.
- Postgres feedback 8/8 against Testcontainers. `MigrateAsync` applies the `Unset` default, and an update appends a severity activity without a status. This fix pass built the infrastructure test project and did not re-run the live Postgres suite.
