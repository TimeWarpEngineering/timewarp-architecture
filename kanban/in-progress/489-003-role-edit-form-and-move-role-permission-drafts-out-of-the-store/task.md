# Role edit form and move role permission drafts out of the store

## Goal (implementer restatement)

The admin role screens edit a copy of the role the server returned. Unsaved checkbox choices stay on the page that shows them.

`RoleState` fetches `GetRole` and keeps that result as one `GetRole.Response` (`RoleSnapshot`). Only the fetch and the update handlers assign it. New Role shows the create form and the edit form side by side. Both use `RoleDetailsValidator`. The edit form builds `UpdateRole.Command` with `UpdateRole.Command.From` when the loaded role's id, name, or description differ from the values it last copied, on Cancel, and after Save. It does not copy on every render. `From` also copies `RoleId`, because that id is not an `IRoleDetails` field. The role detail page uses the same form.

Permission and agent-scope checkboxes do not read or write `RoleState`. The detail page keeps those sets. Save sends `SetRolePermissions` or `SetRoleAgentScopes` from the set that was saved. A successful save recopies only that set. A failed save leaves both page sets alone and leaves `RoleSnapshot` on the lists the fetch stored.

`UpdateRole` stores the name and description from the command that was sent. Typing in the form while that request is in flight does not replace the stored text.

`PrincipalDetailPage` does the same for role and agent-scope checkboxes. `PrincipalState` has no draft sets. Opening a principal stores the detail. The page copies the ids and scopes into its own sets. Save sends the command for the list that was saved and recopies only that list. A failed role save leaves the opened principal in place.

Acceptance:

- `GetRole` stores the response. `UpdateRole` changes that object's name and description to the text that was sent and keeps its permission and agent-scope lists.
- Create and edit sit side by side on New Role. Both validate with `RoleDetailsValidator`. Cancel restores the stored role. Save sends `UpdateRole`. An agent rename of the open role replaces the form text. A membership save does not.
- Role detail checkboxes start from the stored role. Toggling them does not change `RoleState`. Saving one list sends that list and recopies only that list. A failed save does not reload either list, leaves the success flag false, and leaves `RoleSnapshot` on the fetched lists.
- Principal detail checkboxes start from the opened principal. Saving roles or scopes sends that page list and recopies only that list. A failed role save leaves the success flag false and leaves the opened principal in place. `SetPrincipalAgentScopes` sends the page's scopes. The store has no draft sets.

## Description

- Add a `GetRole` fetch to the SPA (`GetRole.Response : IRoleDetails`) and an edit form next to
  the create-only `RoleForm`, using `UpdateRole.Command.From(IRoleDetails)`. Shows create vs
  edit side by side with the shared `RoleDetailsValidator`.
- Move `RoleState.DraftPermissionIds` / `DraftAgentScopes` out of the store into a working copy
  owned by the page (or a pending action); save dispatches the command built from it.
- Same treatment for `PrincipalState` role/scope checkbox drafts on `PrincipalDetailPage`.

## Depends on

- 489-002

## Checklist

- [x] GetRole fetch + RoleState holds whole Response
- [x] Role edit form
- [x] Permission/scope drafts out of RoleState
- [x] Principal role/scope drafts out of PrincipalState
- [x] Tests

## Results

`RoleState` stores `GetRole.Response? RoleSnapshot`. `GetRole` assigns that response. `UpdateRole` rebuilds it from the command that was sent, not the form's live command, and keeps the previous permission and agent-scope lists when the role id matches. `SetRolePermissions` and `SetRoleAgentScopes` are no longer draft requests: the generated handler sends the command the page built, and success replaces that list on the open snapshot. A failed save leaves the success flag false and leaves the fetched lists on `RoleSnapshot`.

`RoleEditForm` builds `UpdateRole.Command` with `UpdateRole.Command.From` when the loaded role's id, name, or description differ from the values last copied, on Cancel, and after Save. A membership save keeps name and description, so it does not rebind. `CopyBeyondDetails` copies `RoleId` from `GetRole.Response`. New Role shows `RoleForm` and `RoleEditForm` side by side (`PageSplit`). The edit column loads the Administrator role. Role detail uses the same form when the caller can manage the role. Permission and agent-scope sets live on `RoleDetailPage`. The load copies both. A successful save recopies only the list that was saved. A failed membership save does not copy the snapshot back over those sets.

`PrincipalState` no longer stores `DraftRoleIds` or `DraftAgentScopes`. `PrincipalDetailPage` copies role ids and agent scopes from the opened principal. Save sends `SetPrincipalRoles` or `SetPrincipalAgentScopes` built from those sets, then reopens the principal and recopies only the list that was saved. A failed role save leaves the opened principal in place.

### How to validate

Smoke: sign in with `admin.roles.manage` and `admin.principals.manage`. Open `/Admin/Roles/New`. Change the Administrator name and click Cancel, then change it and click Save. Open that role's detail page. Toggle one permission and one agent scope, then click the permission Save. Toggle a role and an agent scope on a principal, then click the role Save.

Expect: New Role shows Create and Edit side by side. Both forms validate name and description. Cancel restores the stored Administrator name. Save keeps that name and leaves the permission and agent-scope lists as they were. On the detail page, the permission Save keeps the unsaved agent-scope toggle. The principal role Save keeps the unsaved agent-scope toggle. A reload before Save restores the stored checkboxes. A read-only roles principal sees the description and disabled checkboxes, not the edit form. An agent rename of the open role replaces the form text. A failed membership save leaves the fetched lists in the store.

The SPA was not exercised in a browser in this walk. The page copy and the form rebind have no render test. The store tests cover the sent command, the failed-save snapshot, and `SetPrincipalAgentScopes`.

Gates (fix pass):

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- `tests/container-apps/web/web-spa-integration-tests/features/admin/roles/role-state-contract-actions-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). `./bin/dev test` does not take a file. CI runs the project. `dotnet run --project tests/container-apps/web/web-spa-integration-tests -c Release --no-build -- --filter-class RoleState_Contract_Actions_Should` — passed, 8 tests.
- `tests/container-apps/web/web-spa-integration-tests/features/admin/principal-state-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). CI runs the project. `dotnet run --project tests/container-apps/web/web-spa-integration-tests -c Release --no-build -- --filter-class PrincipalActions_Should_` — passed, 9 tests.
- `ganda repo audit` — exit 0. 43 passed, 2 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing; gitignored local state, pre-existing). `analyzer-suppressions` passed: 843 suppressions, none above the baseline.

### Review

- Round 1 (effort 3, general reviewer). Findings: 0 bugs, 4 suggestions (M1 to M4) implemented in this fix pass, 1 nit wontfix (M5). Round 2 has not run.
- Review files: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

## Session

- Implementation: Grok 4.7, 2026-11-23. The role store keeps one `GetRole.Response`. New Role shows create and edit side by side. Permission, agent-scope, and principal checkbox drafts live on the page. Save sends the command the page built.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a20899-0a3b-7033-8938-225fdcd97bf0, max-turns 500, Done — 2026-11-23T13:54:31Z
- Review oracle round 1: Claude Opus 5.5 (claude-opus-5-5), effort 3, roster general, 2026-11-23.
  Four suggestions are open (M1 to M4) and need a fix pass. M5 is wontfix: it follows the 489-002 ProfilePage pattern.
  The restatement matches the task, so there is no misread.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 58166031-ebf7-4142-9083-ff9357a85ea1, max-turns 200, artifacts general.md, merged.md — 2026-11-23T13:57:26Z
- Fix pass: Grok 4.7, 2026-11-23. A successful save recopies only the list that was saved. `UpdateRole` stores the command that was sent. The edit form rebinds when the snapshot name or description changes. Failed membership saves and `SetPrincipalAgentScopes` are covered by the state tests. M5 stays wontfix.

## Parent

- 489

## Review

- Round 1: M1, M2, M3, and M4 implemented in the fix pass. M5 stays wontfix. Round 2 has not run.
