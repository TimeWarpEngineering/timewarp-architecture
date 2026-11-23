# Convert AiPage assistant-name and BYOK forms

## Goal (implementer restatement)

The Admin → AI assistant-name form and the xAI key-policy form should each edit a command copied from the site-settings response, and a draft should count as dirty only when that command differs from the response it was copied from. `UpdateAssistantName.Command` and `UpdateXaiByokMode.Command` gain small `I*Details` interfaces. `GetSiteSettings.Response` implements both, so `Command.From` copies the one field and `CopyBeyondDetails` copies `Version`. `SiteSettingsState` keeps that response on `SiteSettings` beside the existing flattened fields.

`AiPage` deletes `SyncAssistantDraft`, `SyncByokDraft`, `SyncUnedited*`, and the `Synced*` fields. It does not assign a draft from the render block. After the page fetches, and again after a successful save of that form, it replaces the command with `From(SiteSettings)`. A newer response while the page is open arrives through `ShouldReRender`, because `OnParametersSet` does not run for a state action. A command that still matches the response it was copied from is replaced. A command that differs keeps the edit. If the stored field that edit changes is the same on the new response, the command takes the new `Version`. If that field changed too, the command keeps its old `Version`.

Acceptance:

- `UpdateAssistantName.Command.From(GetSiteSettings.Response)` copies the name and `Version`. `UpdateXaiByokMode.Command.From` copies the mode and `Version`.
- Opening Admin → AI loads the server name, mode, and version into both forms.
- An agent rename or key-policy update while the page is open refreshes an unedited form, including the other form's version.
- An unsaved edit remains when the other save advances `Version`, and that edit's command takes the new version.
- An unsaved edit whose own stored field also changed keeps the typed value and the old version.
- Saving the assistant name through the page replaces that form with the saved name and version, and keeps an unsaved key-policy edit. Saving the key policy through the page does the same for the mode, and keeps an unsaved name.
- Each form is shown when its copied source response is set. There is no separate loaded flag.
- The xAI API key form is unchanged.

## Description

`admin/ai/pages/AiPage.razor` forms at lines ~274 and ~400 copy flattened `SiteSettingsState`
fields with `SyncAssistantDraft` / `SyncByokDraft` plus `SyncUnedited*` dirty tracking via shadow
`Synced*` fields, and mutate a component field from the render block.

- Add small `I*Details` interfaces for `UpdateAssistantName.Command` and
  `UpdateXaiByokMode.Command` (or copy from the whole `GetSiteSettings.Response`).
- Delete the shadow dirty-tracking; dirty = working copy differs from stored Response.

## Depends on

- 489-001

## Checklist

- [x] Interfaces / copy source
- [x] Remove Sync* and Synced* code
- [x] Tests

## Results

`IAssistantNameDetails` and `IXaiByokModeDetails` are the copy sources. `UpdateAssistantName.Command` and `UpdateXaiByokMode.Command` implement them, and `GetSiteSettings.Response` implements both. Generated `From` copies the one field. `CopyBeyondDetails` copies `Version` from `GetSiteSettings.Response`.

`SiteSettingsState.SiteSettings` holds that response beside the flattened fields. Fetch assigns it. A successful assistant-name save, key-policy save, or authentication-policy save replaces the instance through `ReviseSiteSettings`, because `Version` is get-only.

`AiPage` binds `UpdateAssistantName.Command` and `UpdateXaiByokMode.Command`. `From` runs after the page fetch and after that form's successful save. A later `SiteSettingsState` action reconciles both drafts in `ShouldReRender`. An unedited command is replaced. An edited command stays, and takes the new `Version` when its own stored field is unchanged. The assistant form renders when `AssistantSource` is set, and the key-policy form renders when `ByokSource` is set. The xAI API key form is unchanged.

Review round 1: the page's own save is what moves the saved form onto the saved response. `SaveAssistantAsync` and `SaveByokAsync` accept that response with force after `ShouldReRender` has left the old version. The tests edit the other form, invoke those handlers, and read the rendered attributes.

### How to validate

Smoke: sign in as an admin and open Admin → AI. Wait until the assistant name and the Ask API key policy are filled. Change the policy to Off and leave it unsaved, then save a new assistant name. Change the assistant name and leave it unsaved, then save the policy. Reload Admin → AI.

Expect: both forms open on the server name, mode, and the same version. Saving the name keeps the unsaved Off policy and moves that form's version to the saved version. Saving the policy keeps the unsaved name and moves that form's version to the saved version. Reload shows the saved name and Off. The site xAI API key form still saves and clears a key, and the stored secret stays hidden.

Gates (fix pass, review round 1):

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- `source/container-apps/web/features/settings/update-assistant-name/update-assistant-name-tests.cs` — not edited this pass. Implementation pass: `dotnet run --file` passed, 24 tests, including `From_Should_CopyName_And_Version_From_GetSiteSettings`.
- `source/container-apps/web/features/settings/update-xai-byok-mode/update-xai-byok-mode-tests.cs` — not edited this pass. Implementation pass: `dotnet run --file` passed, 11 tests, including `From_Should_CopyMode_And_Version_From_GetSiteSettings`.
- `tests/container-apps/web/web-spa-integration-tests/features/admin/ai-page-assistant-draft-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). CI runs the project. `dotnet run --project tests/container-apps/web/web-spa-integration-tests -c Release --no-build -- --filter-class AiPage_Should_` — passed, 9 tests, including `Move_The_Assistant_Form_To_The_Saved_Response_And_Keep_A_Byok_Edit` and `Move_The_Byok_Form_To_The_Saved_Response_And_Keep_A_Name_Edit`.
- `ganda repo audit` — exit 0. 43 passed, 2 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing; gitignored local state, pre-existing). `analyzer-suppressions` passed: 843 suppressions, none above the baseline.

### Review

- Round 1, effort 2, roster: general. Counts at merge: 0 bug, 1 suggestion, 1 nit.
- M1 (suggestion, fixed): `AiPage_Should_` edits a field and invokes `SaveAssistantAsync` / `SaveByokAsync`. The saved form shows the saved value and version. The other form keeps its unsaved edit.
- M2 (nit, fixed): `AssistantDraftLoaded` and `ByokDraftLoaded` are gone. The markup renders each form when its source response is set.
- Round 2, effort 1, roster: general. Re-checked fix delta 44a850b72. M1 and M2 are fixed. No new findings.
- Final counts: bug 0, suggestion 1 fixed, nit 1 fixed, 0 open, 0 wontfix.
- Disposition: **clean** (2 rounds, final open count 0).
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Session

- Implementation: Grok 4.7, 2026-11-23. The assistant-name and key-policy forms copy `GetSiteSettings.Response` with `From`. A state action reconciles the open drafts in `ShouldReRender`, because `OnParametersSet` does not run for that re-render. An edit stays, and takes the new version when its own stored field is unchanged.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a208c8-fc85-7d03-8ddf-af336c82e51b, max-turns 500, Done — 2026-11-23T14:51:10Z
- Review oracle round 1: Claude Opus 5.5 (claude-opus-5-5), effort 2, roster general, turn cap 120, 2026-11-23. 2 open findings (M1 suggestion, M2 nit), fix pass requested.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 0c375f0b-0f7f-4d7b-be31-ab10bf662f28, max-turns 120, artifacts general.md, merged.md — 2026-11-23T14:53:33Z
- Fix pass: Grok 4.7, 2026-11-23. The forms render from the copied source. The page save tests invoke `SaveAssistantAsync` and `SaveByokAsync`, so a deleted force accept fails on the saved version.
- Review oracle round 2: Claude Opus 5.5 (claude-opus-5-5), effort 1, roster general, turn cap 80, 2026-11-23. Fix delta checked, M1 and M2 fixed, no new findings. Disposition clean.

## Parent

- 489

## Review

- Round 1: fix pass addressed M1 and M2.
- Round 2: Done
