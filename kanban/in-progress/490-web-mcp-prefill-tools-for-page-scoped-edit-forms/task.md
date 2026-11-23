# WebMCP prefill tools for page-scoped edit forms

## Description

Let an agent draft a form for a person to review and submit, without controlling form inputs
directly. Every effect stays an action.

- For each page-scoped submit action in `[Page(AgentActions=...)]` whose page has an edit
  form, generate a `Prefill.<Action>` agent tool. Schema = the Command schema with every field
  optional.
- The tool dispatches a `Prefill<X>` action that stores an immutable *suggestion* in state
  (legal: via an action). The page applies only the supplied fields onto its working copy
  (from the edit-form pattern task), then dispatches an action to clear the suggestion.
- If the working copy has unsaved edits, ask before applying.
- Off-page: reuse the existing `navigateTo` offer from `CatalogAgentToolSet`.
- Approval: no server effect, so prompt only in "Ask before editing" mode (like `navigate`).
- Mark agent-filled fields visibly. Implementer picks the marker from existing design-system tokens and components (tw-blazor-css-strategy; no new colors), records the choice in Results, and shows it on the /DesignSystem page (groom 2026-11-23).
- Same tools available to in-app Ask and WebMCP (`web-mcp-publisher.cs`).

Background (research 2026-11-14): vercel-labs/ui (UI SDK) lets agents drive widgets
(`snapshot`/`query`/`act`). We rejected widget control; the only real gap it exposed was the
"agent drafts, person commits" handoff, which this task covers.

## Goal (implementer restatement)

An agent can draft the profile edit form for a person to review and save. The agent never writes the inputs. Every effect is a TimeWarp.State action.

`Prefill.Profile.UpdateProfile` is generated from the page-scoped `UpdateProfile` submit. Its arguments are that command with every field optional. On the page the tool stores an immutable suggestion through a catalog action. The page copies only those fields onto its working copy, then clears the suggestion through another action. Off the page, nothing is stored and the existing navigate offer is returned. Drafting does not touch the server, so approval is asked only in Ask before editing, the same as navigate. Fields the agent filled show the existing Info badge labeled Agent. In-app Ask and WebMCP see the same tool.

Round-2 acceptance:

- A suggestion that arrives while the profile fetch is still running stays in state. The page applies it only after `CopyCommandFromProfile` has copied a loaded profile into the working command.
- Page tests cover a clean apply with no prompt, a dirty form that asks, decline keeping the typed value, the Agent badge until the person edits that field, and the load race.
- Arguments that match no field return `drafted: false`, name the accepted keys, and store nothing.
- The replace prompt says the draft replaces unsaved changes on the form.
- Prefill approval treats the tool as page-bound. The branch for a tool that is not page-bound is gone.

## Depends on

- 489-002

## Checklist

- [x] Suggestion state + apply/clear on Profile (first consumer)
- [x] Tool generation from page scope
- [x] Approval / edit-mode handling
- [x] Dirty-form conflict handling
- [x] Tests (catalog-agent-webmcp-tests)

## Related follow-ups (not in scope)

- Analyzer: every ID-typed parameter on an agent-visible action has a
  `[ReferenceableResource]` list query with matching `IdTypeName`.
- `readOnlyHint` / `untrustedContentHint` on `WebMcpToolDescriptor`.
- Redaction markers for what `page_context` reads off the page.

## Results

Profile's UpdateProfile is the only edit form that opts in, via `[EditFormSubmit]`. The tool is `Prefill.Profile.UpdateProfile`. Its schema is the command with every field optional. The tool executes `Profile.SetProfileSuggestion`, which stores the supplied JSON, a new id, and `Accepted = false`. It does not call UpdateProfile. The page copies only those fields onto the task-489 working copy, then dispatches `ClearProfileSuggestion`. It does not read the suggestion until `CopyCommandFromProfile` has copied a loaded profile into the working command, so a draft that arrives during the fetch stays in state. If the working copy differs from the last snapshot, the page dispatches `ConfirmProfileSuggestion` first. The question says the draft replaces unsaved changes on the form. Decline clears the suggestion when the id is unchanged. Accept sets `Accepted` and the page applies. A newer suggestion that arrives during the dialog is left alone. Arguments that match no field, including an empty object, are not a draft: the call returns `drafted: false`, names the accepted keys, and stores nothing. Unknown keys are ignored only when at least one key matches. Off the page, the tool returns the existing `navigateTo` offer and stores nothing. Approval is required only while edit mode is Ask before editing and the person is on the page. Prefill approval treats the tool as page-bound. In-app Ask and WebMCP both take the tool from `CatalogAgentToolSet.SelectAsync`.

Marker: `StatusBadge` with `BadgeStatus.Info` and the label `Agent`. That badge already uses the info tokens (`--twe-info-tint`, `--twe-on-info`, `--twe-blue`) through `twe-badge--info`. No new color. The label row is a flex row with `gap: var(--twe-space-2)`. The badge is on each Profile field the draft wrote, and on the display-name field of `FormsSample` (the forms sample already on `/DesignSystem`). `data-qa="AgentFilled"`. The badge hides when the person changes that field. No browser in this session, so the running page was not opened. `form-field-render-tests` asserts the badge markup.

JSON null is a null `JsonNode`, not `JsonValueKind.Null`. Normalize and apply treat that null reference as an explicit null and keep it on a nullable property.

### How to validate

Smoke: Sign in as someone who can write their profile. On Home, call `Prefill.Profile.UpdateProfile` with `{"displayName":"Ada"}`. On `/Profile`, call it again with only `displayName` and `notifications`, including once while the profile section is still loading. If the form already has unsaved edits, answer the dialog. Then change the display name by hand. Call it with `{}` and with `{"nope":1}`.

Expect: Off `/Profile` the call returns a navigate offer to `/Profile` and stores nothing. A draft that arrives while the profile is loading stays stored and is applied only after the loaded profile is the working copy. On the page, Ask before editing prompts before anything is stored; Automatically edit stores without a prompt. Only the supplied fields change. The display name shows an Info badge labeled Agent until it is edited. The profile is not saved until the person submits. The replace question says "An agent drafted new profile values. Applying them replaces unsaved changes on this form." Decline keeps what was typed. The confirm button is "Apply draft" and the cancel button is "Keep mine". An empty object or an unknown key returns `drafted: false`, names the accepted keys (`displayName`, `email`, `language`, `region`, `theme`, `notifications`), and stores nothing. A signed-in browser was not opened. The page tests render `ProfilePage` on an interactive HTML host and are the substitute.

Gates for the round-2 fix pass. These test files are not standalone runfiles (no `#if !JARIBU_MULTI`). `./bin/dev test` does not take a file, so CI runs `web-spa-integration-tests`. Filtered `dotnet test -c Release --no-build --no-restore` from that project directory:

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- `profile-page-prefill-tests.cs` — `--filter-class ProfilePage_Should_` — passed, 5 tests.
- `profile-state-tests.cs` — `--filter-class FetchAndUpdate_Should_` — passed, 5 tests.
- `catalog-agent-webmcp-tests.cs` — `--filter-method Prefill` — passed, 8 tests (the six methods in this file, including `Prefill_With_No_Matching_Field_Does_Not_Store_A_Draft`, plus `Auto_Open_Rename_Editor_Prefilled_For_Pending_Credential_Only` and `Rename_Prefill_Is_Current_Nickname_Else_Provider`).
- `ganda repo audit` — exit 0. 43 passed, 2 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing; gitignored local state, pre-existing). Not changed here.

### Review

Round 1 (effort 3, roster: general): 1 bug, 2 suggestions, and 2 nits. The fix pass addressed M1–M5. Round 2 has not run. Files: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

## Session

- Fix pass for review round 1: the page waits until a loaded profile is the working copy before it applies a draft. Arguments that match no field are not stored. The replace question names unsaved changes on the form. Prefill approval is page-bound. Page tests call `OnAfterRenderAsync` because the static HTML renderer skips it. 2026-11-24
- Review oracle round 1: Claude Code (claude-opus-5-5), effort 3, roster general. 5 open findings (M1 to M5), so a fix pass is requested. 2026-11-24
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a209ef-f624-7cb0-9bd6-1d85e22351ca, max-turns 500, Done — 2026-11-23T20:34:56Z
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session ade0fea0-0949-474c-9b54-72673af3d539, max-turns 200, artifacts general.md, merged.md — 2026-11-23T20:37:50Z

## Review

- Round 1: fix pass addressed M1, M2, M3, M4, M5. Round 2 has not run.
