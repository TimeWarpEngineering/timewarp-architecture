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

An agent can draft a page's edit form for a person to review and save. The agent never writes the inputs. Every effect is a TimeWarp.State action.

For each page-scoped submit action that opts in with `EditFormSubmit`, the tool list includes `Prefill.<Action>`. Its arguments are that command with every field optional. On the page, the tool stores an immutable suggestion (only the fields it was given) by executing a catalog action. The page copies those fields onto its working copy, then clears the suggestion through another action. If that working copy has unsaved edits, the person is asked first. Off the page, nothing is stored and the existing navigate offer is returned. Drafting does not touch the server, so the tool asks for approval only in Ask before editing, the same as navigate. Fields the agent filled show the existing Info status badge labeled Agent, on the profile form and on the design-system forms sample. In-app Ask and WebMCP see the same tools. Profile's UpdateProfile is the only consumer.

Acceptance: Profile stores, confirms, applies, and clears a suggestion; the prefill tool is generated from page scope, including the off-page navigate offer; approval follows the edit mode; a dirty form asks before apply; tests cover the tool list, schema, approval, both drivers, the suggestion state, and the badge; the marker choice is recorded under Results.

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

Profile's UpdateProfile is the only edit form that opts in, via `[EditFormSubmit]`. The tool is `Prefill.Profile.UpdateProfile`. Its schema is the command with every field optional. The tool executes `Profile.SetProfileSuggestion`, which stores the supplied JSON, a new id, and `Accepted = false`. It does not call UpdateProfile. The page copies only those fields onto the task-489 working copy, then dispatches `ClearProfileSuggestion`. If the working copy differs from the last snapshot, the page dispatches `ConfirmProfileSuggestion` first. Decline clears the suggestion when the id is unchanged. Accept sets `Accepted` and the page applies. A newer suggestion that arrives during the dialog is left alone. Off the page, the tool returns the existing `navigateTo` offer and stores nothing. Approval is required only while edit mode is Ask before editing and the person is on the page. In-app Ask and WebMCP both take the tool from `CatalogAgentToolSet.SelectAsync`.

Marker: `StatusBadge` with `BadgeStatus.Info` and the label `Agent`. That badge already uses the info tokens (`--twe-info-tint`, `--twe-on-info`, `--twe-blue`) through `twe-badge--info`. No new color. The label row is a flex row with `gap: var(--twe-space-2)`. The badge is on each Profile field the draft wrote, and on the display-name field of `FormsSample` (the forms sample already on `/DesignSystem`). `data-qa="AgentFilled"`. The badge hides when the person changes that field. No browser in this session, so the running page was not opened. `form-field-render-tests` asserts the badge markup.

JSON null is a null `JsonNode`, not `JsonValueKind.Null`. Normalize and apply treat that null reference as an explicit null and keep it on a nullable property.

### How to validate

Smoke: Sign in as someone who can write their profile. On Home, call `Prefill.Profile.UpdateProfile` with `{"displayName":"Ada"}`. On `/Profile`, call it again with only `displayName` and `notifications`. If the form already has unsaved edits, answer the dialog. Then change the display name by hand and save, or cancel.

Expect: Off `/Profile` the call returns a navigate offer to `/Profile` and stores nothing. On the page, Ask before editing prompts before anything is stored; Automatically edit stores without a prompt. Only the supplied fields change. The display name shows an Info badge labeled Agent until it is edited. The profile is not saved until the person submits. Decline on "Replace unsaved edits?" keeps what was typed. The confirm button is "Apply draft" and the cancel button is "Keep mine".

Gates. These test files are not standalone runfiles (no `#if !JARIBU_MULTI`). `./bin/dev test` does not take a file, so CI runs `web-spa-integration-tests`. Filtered `dotnet test -c Release --no-build --no-restore` from that project directory:

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- `form-field-render-tests.cs` — `--filter-class FormField_Should_` — passed, 2 tests.
- `profile-state-tests.cs` — `--filter-class FetchAndUpdate_Should_` — passed, 5 tests.
- `action-catalog-tests.cs` — `--filter-class ActionCatalog_Should` — passed, 10 tests.
- `catalog-agent-webmcp-tests.cs` — `--filter-method Prefill` — passed, 7 tests (the five methods in this file, plus `Auto_Open_Rename_Editor_Prefilled_For_Pending_Credential_Only` and `Rename_Prefill_Is_Current_Nickname_Else_Provider`). `--filter-method WebMcp_Registers_Page_Tools_And_Absent_Api_Is_A_NoOp` — passed, 1 test.
- `catalog-agent-test-fixture.cs` — no tests of its own. `--filter-method Select_Is_Page_Scoped_Permission_Filtered_And_Drops_Human` — passed, 1 test. `--filter-method In_App_And_WebMcp_Tool_Lists_Match_For_Every_Page_Principal_And_Edit_Mode` — passed, 1 test.
- `command-palette-tests.cs` — `--filter-method List_Permitted_Pages_And_Parameterless_Human_Commands` — passed, 1 test. `--filter-method Opt_Out_Only_Explicitly_With_A_Reason` — passed, 1 test.
- `design-system-page-contract-tests.cs` — `--filter-method Keep_Stored_Choices_On_The_Form_Fields_Only` — passed, 1 test.
- `ganda repo audit` — exit 0. 43 passed, 2 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing). Non-blocking. Not changed here.
