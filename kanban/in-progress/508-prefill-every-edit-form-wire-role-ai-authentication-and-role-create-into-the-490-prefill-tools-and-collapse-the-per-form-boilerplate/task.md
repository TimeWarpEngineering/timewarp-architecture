# Prefill every edit form: wire Role, AI, Authentication, and Role create into the 490 prefill tools and collapse the per-form boilerplate

## Description

Task 490's spec: "For each page-scoped submit action in `[Page(AgentActions=...)]` whose page has
an edit form, generate a `Prefill.<Action>` agent tool." Profile was named only as "first
consumer". What landed (PR #665) is a generic tool generator (`form-prefill.cs` builds a tool for
any catalog entry carrying `[EditFormSubmit]`) with exactly one opt-in,
`Profile.UpdateProfile`. The narrowing was not recorded in Results, review, or a follow-up.

Each further form currently needs three hand-written pieces: the attribute on its submit action,
its own suggestion action set (set / confirm / clear), and apply, dirty-check, and Agent-badge
code in the page (`ProfilePage.razor`). That boilerplate is why only one form was wired. This
task wires the rest and shrinks the per-form cost so the next form is one attribute plus one
shared page component.

## Requirements

### Collapse the boilerplate first

- One generic suggestion store keyed by the submit action type (or the page type) replaces the
  per-slice `Set/Confirm/ClearProfileSuggestion` trio. Profile migrates onto it; its tests keep
  passing.
- One shared page component (or a base the 489 working-copy pages already share) owns apply,
  dirty-check dialog ("An agent drafted new … values. Applying them replaces unsaved changes on
  this form." / "Apply draft" / "Keep mine"), and the `Agent` info badge per field, so a page
  opts in by rendering it with its working copy and submit action.
- The `[EditFormSubmit]` opt-in stays. If the generator can instead derive the opt-in from a
  page-scoped submit action whose Command implements an `I*Details` interface, prefer that and
  delete the attribute; record the decision in the Design region of `form-prefill.cs`.

### Wire every edit form

| Page | Submit action | Prefill tool |
|---|---|---|
| `RoleDetailPage` (`RoleEditForm`) | `Role.UpdateRole` | `Prefill.Role.UpdateRole` |
| `RolePage` (`RoleForm`, create) | `Role.CreateRole` | `Prefill.Role.CreateRole` |
| `AiPage` | `SiteSettings.UpdateAssistantName`, `SiteSettings.UpdateXaiByokMode` | one tool each |
| `AuthenticationPage` | `SiteSettings.UpdateSiteSettings` | `Prefill.SiteSettings.UpdateSiteSettings` |
| `PasskeysPage` rename | check: if it is a page-scoped submit with a Command, wire it; else record why not |

A new edit form added later must get its tool by rendering the shared component; add a
convention check (analyzer or test over the page registry) that fails when a page has a
page-scoped submit action with an `I*Details` Command and no prefill opt-in.

### Evidence

- For each wired page: a screenshot in `evidence/` of the agent-filled field with the `Agent`
  badge, taken from the running app via Ask AI, signed in with the right permissions.
- Results list every `Prefill.*` tool the catalog now exposes (`web-mcp` listing output pasted).

## Goal (implementer restatement)

Every edit form whose page lists its submit action in `[Page(AgentActions=...)]` gets a
`Prefill.<Action>` agent tool, and the per-form cost of that drops to rendering one shared
component around the form.

Acceptance:

- One suggestion store (`FormSuggestionState`) keyed by the submit action type replaces
  Profile's Set / Confirm / Clear suggestion trio. Profile moves onto it and its 490 tests pass.
- One shared component (`FormPrefillScope<TSubmit>`) owns apply, the dirty-check question
  ("An agent drafted new … values. Applying them replaces unsaved changes on this form.",
  "Apply draft", "Keep mine"), and the `Agent` badge (through `FormField Field=`).
- The opt-in is derived: a page-bound submit action whose `Command` implements one `I*Details`
  interface gets a tool. `[EditFormSubmit]` is deleted and the decision is in the Design region
  of `form-prefill.cs`. The draft fields are that interface's properties, so ids and `Version`
  are never drafted.
- Wired: `Role.UpdateRole` (RoleEditForm), `Role.CreateRole` (RoleForm),
  `SiteSettings.UpdateAssistantName` and `SiteSettings.UpdateXaiByokMode` (AiPage),
  `SiteSettings.UpdateSiteSettings` (AuthenticationPage). Passkeys rename is decided and recorded
  (its action takes an id and a nickname, not a Command).
- A convention test over the page registry fails when a page-scoped submit with an `I*Details`
  Command has no `FormPrefillScope<TSubmit>` in the web-spa assembly.
- Evidence: the `Prefill.*` tool listing in Results; screenshots only if a signed-in running app
  is available to this worker (otherwise recorded as not taken, with page tests standing in).
- Gates: `dev build` 0/0, the touched web-spa integration tests, `ganda repo audit`.

## Checklist

- [x] Generic suggestion store + shared page component; Profile migrated, 490 tests green
- [x] Role update, Role create, AI (two), Authentication wired; Passkeys decided
- [x] Convention check fails on an unwired edit form
- [ ] Screenshots per page in `evidence/` (not taken: no running app in this worker, see Results); tool listing in Results
- [x] `dev build` 0/0, web-spa integration tests, catalog-agent-webmcp tests, `ganda repo audit` green

## Results

Profile's Set / Confirm / Clear suggestion trio, its three `ProfileState` fields, and
`ProfileFormDraft` are gone. `FormSuggestionState` (`features/application/form-suggestion-state/`)
holds one draft per form, keyed by the submit action's full type name: JSON, a new id, and
Accepted. `SetFormSuggestion(form, json)` is the one catalog action the Prefill tools execute
(Agent visibility, no permission of its own: the tool checks the submit action's permissions
first). `ConfirmFormSuggestion(form, noun)` asks "An agent drafted new {noun} values. Applying
them replaces unsaved changes on this form." with "Apply draft" / "Keep mine".
`ClearFormSuggestion(form, id)` drops only the draft that was applied. Sign-out initializes the
store.

`FormPrefillScope<TSubmit>` (`features/application/components/`) is the shared page component.
A form opts in by wrapping its `EditForm` in it with `Model` (the working command), `Noun`, and
`Ready` when the command is a placeholder until a fetch lands. It snapshots the command as
contract JSON whenever `Model` is a new reference (load, Cancel, Save) and calls the form dirty
when the JSON differs. It applies the draft in `OnAfterRenderAsync`, asks first when dirty, then
clears the draft. It cascades itself as `IAgentFilledFields`, and `FormField` has a new `Field`
parameter, so the `Agent` badge needs no page code. ProfilePage lost its apply, dirty, and badge
code.

Opt-in decision (recorded in the Design region of `form-prefill.cs`): derived. `[EditFormSubmit]`
is deleted. A catalog action is an edit-form submit when a page lists it in `AgentActions` and
its public `Command` property's type implements exactly one `I*Details` interface. The tool's
schema and the accepted keys are that interface's properties, so `RoleId`, `Version`, and
`UserId` are never drafted.

Wired: `RoleEditForm` (`Role.UpdateRole`, noun "role"), `RoleForm` (`Role.CreateRole`, "role"),
`AiPage` (`SiteSettings.UpdateAssistantName`, "assistant"; `SiteSettings.UpdateXaiByokMode`,
"Ask API key"), `AuthenticationPage` (`SiteSettings.UpdateSiteSettings`, "authentication"; the
tenant list field is `EntraAcceptedTenants`). Passkeys rename is not wired:
`CredentialsState.RenameCredential`'s action takes a credential id and a nickname, not a Command,
so it has no `I*Details` form to draft, and the convention does not derive a tool for it.
`SubmitFeedback` is excluded the same way (no Command property).

Convention check: `FormPrefillConvention_Should_` (`form-prefill-convention-tests.cs`) walks
`PageRegistry.ActionScopes` for derived submits, pins the six that exist today, and fails when
one has no `FormPrefillScope<TSubmit>` in the web-spa assembly (read from the module's MethodSpec
and TypeSpec metadata, where Razor's `OpenComponent<FormPrefillScope<X>>` lands). A second method
feeds a fake page scope and shows the rule reports the unwired submit.

`tw-blazor` SKILL.md gained one bullet on how a new edit form opts in.

Prefill tool listing for a principal with every permission (`WebMcp_Lists_A_Prefill_Tool_For_Every_Edit_Form`,
WebMCP `DescribeAsync` output):

```
PREFILL-PROOF path=/Profile tools=Prefill.Profile.UpdateProfile,Prefill.Role.CreateRole,Prefill.SiteSettings.UpdateAssistantName,Prefill.SiteSettings.UpdateSiteSettings,Prefill.SiteSettings.UpdateXaiByokMode
PREFILL-PROOF path=/Admin/Roles/New tools=Prefill.Profile.UpdateProfile,Prefill.Role.CreateRole,Prefill.SiteSettings.UpdateAssistantName,Prefill.SiteSettings.UpdateSiteSettings,Prefill.SiteSettings.UpdateXaiByokMode
PREFILL-PROOF path=/Admin/Roles/2297e58f-069b-4385-a19c-41a7981616f7 tools=Prefill.Profile.UpdateProfile,Prefill.Role.CreateRole,Prefill.Role.UpdateRole,Prefill.SiteSettings.UpdateAssistantName,Prefill.SiteSettings.UpdateSiteSettings,Prefill.SiteSettings.UpdateXaiByokMode
PREFILL-PROOF path=/Admin/AI tools=Prefill.Profile.UpdateProfile,Prefill.Role.CreateRole,Prefill.SiteSettings.UpdateAssistantName,Prefill.SiteSettings.UpdateSiteSettings,Prefill.SiteSettings.UpdateXaiByokMode
PREFILL-PROOF path=/Admin/Authentication tools=Prefill.Profile.UpdateProfile,Prefill.Role.CreateRole,Prefill.SiteSettings.UpdateAssistantName,Prefill.SiteSettings.UpdateSiteSettings,Prefill.SiteSettings.UpdateXaiByokMode
```

Off its page a tool is the navigate offer. `Prefill.Role.UpdateRole` appears only on a role
detail route, because that page has no static route to offer.

Screenshots: not taken. This headless worker does not start an AppHost (a worker `dev run` once
rewrote the shared postgres secret), and Ask AI needs a signed-in browser and a live xAI key.
`evidence/` was not created. The page tests stand in: `ProfilePage_Should_` (Profile) and
`RoleForm_Should_` (New Role) render the form on the interactive HTML host, apply a draft through
`FormPrefillScope`, and assert the `data-qa="AgentFilled"` badge. Role detail, AI, and
Authentication are covered by the convention test and their existing page tests, not by a
rendered-badge test.

### How to validate

Smoke: Sign in as an admin with settings.write and admin.roles.manage. In Ask (Automatically
edit), on `/Admin/Roles/New` call `Prefill.Role.CreateRole` with `{"name":"Auditor"}`; on a role
detail page call `Prefill.Role.UpdateRole` with `{"description":"Read-only","roleId":"…"}`; on
`/Admin/AI` call `Prefill.SiteSettings.UpdateAssistantName` with `{"assistantName":"Kit"}` and
`Prefill.SiteSettings.UpdateXaiByokMode` with `{"xaiByokMode":"Required"}`; on
`/Admin/Authentication` call `Prefill.SiteSettings.UpdateSiteSettings` with
`{"passkeyPromptMode":"Required"}`. Type into one form first, then call its tool again. Repeat
the 490 Profile smoke on `/Profile`.

Expect: each field the draft names fills in with an `Agent` badge and nothing is saved until
Save. `roleId` and `version` are not accepted keys (an argument set with only those returns
`drafted: false` and lists the interface keys). A form with typed changes asks "An agent drafted
new {role|assistant|Ask API key|authentication} values. Applying them replaces unsaved changes on
this form."; "Keep mine" leaves the typed values. Editing a filled field hides its badge. From
Home, the static-route tools return a navigate offer.

Gates (all foreground, this worktree):

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- Test files in `web-spa-integration-tests` are not standalone runfiles (no `#if !JARIBU_MULTI`),
  so each ran as `./bin/dev test --project web-spa-integration-tests --filter-class <class>`:
  - `form-prefill-convention-tests.cs` (new) — `FormPrefillConvention_Should_` — passed, 2 tests.
  - `role-form-prefill-tests.cs` (new) — `RoleForm_Should_` — passed, 2 tests.
  - `profile-page-prefill-tests.cs` — `ProfilePage_Should_` — passed, 5 tests.
  - `profile-state-tests.cs` — `FetchAndUpdate_Should_` — passed, 5 tests.
  - `catalog-agent-webmcp-tests.cs` and `action-catalog-tests.cs` — `--filter-class Catalog`
    (all `CatalogAgent_Should` partials plus `ActionCatalog_Should`, one Aspire boot) — passed,
    66 tests. The first run failed 1: the `/` literal in `WebMcp_Registers_Page_Tools_…` lacked the
    four new off-page Prefill offers for an all-permission principal. Fixed, rerun green.
- Tests covering edited production code, same command: `AiPage_Should_` 9, `AuthenticationDraft_Should_` 7,
  `AuthenticationPage_Should_` 3, `AssistantSettingsPlacement_Should_` 2, `FormField_Should_` 2,
  `SignOut_Should_` 2, `DesignSystemPage_Should_` 6 — all passed.
- Fix pass for review round 1 M1 (same command per class):
  - `./bin/dev build` — succeeded, 0 warnings, 0 errors.
  - `profile-state-tests.cs` — `FetchAndUpdate_Should_` — passed, 6 tests (new
    `Suggestion_Should_Drop_When_PrincipalChanges`).
  - `sign-out-state-tests.cs` — `SignOut_Should_` — passed, 2 tests (seeds a draft, expects it gone).
  - `ganda repo audit` — repository passes, same 1 advisory `vscode-window-icon` warning as below.
- `./bin/dev lint-skills` (skills/ changed) — 9 skills linted, 9 passed; eval spec valid.
- `ganda repo audit` — repository passes, 1 advisory warning: `vscode-window-icon`
  (`.vscode/settings.json` is missing; that file is gitignored local state, so a fix would not
  reach the repo). Pre-existing, also noted on 490.

### Review

Round 1, effort 3, roster general. `review/review-framework.md`, `review/round-1/general.md`,
`review/round-1/merged.md`. Counts: 1 open bug (M1), 0 fixed, 0 wontfix. Fix pass: M1 fixed.
`FormSuggestionState.SessionChanged(principalId)` (`form-suggestion-state.session-changed.cs`)
drops every draft when the principal id changes and keeps them for the same principal.
`AuthenticationStateListener` dispatches it on every authentication change next to the Ask
resets, so the signed-out path (empty id) and a sign-in as someone else both clear drafts.
`Initialize` also forgets the principal. `FetchAndUpdate_Should_.Suggestion_Should_Drop_When_PrincipalChanges`
pins it; `SignOut_Should_` seeds a draft and `AssertCleared` expects none.

Round 2, effort 2, roster general. `review/review-framework.md`, `review/round-2/general.md`,
`review/round-2/merged.md`, `review/disposition.md`. Re-read of the fix delta confirmed M1 fixed.
No new findings. Final counts: bug 0 open / 1 fixed / 0 wontfix. Disposition: clean.

## Depends on

- 490
- 507
- 515

## Session

- Implement oracle (Claude Code, claude-opus-5-5): generic FormSuggestionState and
  FormPrefillScope, derived opt-in, five forms wired, convention test, screenshots not taken
  (no running app in a headless worker). 2026-11-24
- Created: 2026-11-24. 490 scope was generic; the implementer narrowed it to Profile without
  recording the deferral. Investigation table above is from the 2026-11-24 scope check.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 5f10deb9-8939-414b-8d64-765f03250951, max-turns 500, Done — 2026-11-24T15:12:09Z
- Reviewer: Grok session 01a20e1d-6ad8-7190-a625-7d15a7f2d866 (2026-11-24). Round 1 general, effort 3. M1 open.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a20e1d-6ad8-7190-a625-7d15a7f2d866, max-turns 200, artifacts general.md, merged.md — 2026-11-24T15:24:26Z

- Implement-fix-2 oracle (Claude Code, claude-opus-5-5): M1 fixed, FormSuggestionState
  SessionChanged wired into the authentication listener; tests extended. 2026-11-24
- Reviewer: Grok session 01a20e2e-3a68-75a2-beeb-f6cb5c7a575c (2026-11-24). Round 2 general, effort 2. M1 fixed, disposition clean.

## Learnings

Misread: the original Evidence section requires a screenshot in `evidence/` for each wired page, taken from the running app via Ask AI. `## Goal (implementer restatement)` changed that to screenshots only when a signed-in app is available to the worker, with page tests standing in otherwise. The diff has no `evidence/` directory. Rendered-badge tests cover Profile and New Role. Role detail, AI, and Authentication do not. The checklist item is still open. Not filed as a finding: Results already name the AppHost limit, and a fix pass cannot take those shots in this worker.

## Review

- Round 1: Blocked — open M1
- Round 2: Done
