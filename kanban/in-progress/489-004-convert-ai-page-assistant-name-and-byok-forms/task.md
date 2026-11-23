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

`AiPage` binds `UpdateAssistantName.Command` and `UpdateXaiByokMode.Command`. `From` runs after the page fetch and after that form's successful save. A later `SiteSettingsState` action reconciles both drafts in `ShouldReRender`. An unedited command is replaced. An edited command stays, and takes the new `Version` when its own stored field is unchanged. The xAI API key form is unchanged.

### How to validate

Smoke: sign in as an admin and open Admin → AI. Wait until the assistant name and the Ask API key policy are filled. Change the policy to Off and leave it unsaved, then save a new assistant name. Change the assistant name and leave it unsaved, then save the policy. Reload Admin → AI.

Expect: both forms open on the server name, mode, and the same version. Saving the name keeps the unsaved Off policy and moves that form's version to the saved version. Saving the policy keeps the unsaved name and moves that form's version to the saved version. Reload shows the saved name and Off. The site xAI API key form still saves and clears a key, and the stored secret stays hidden.

Gates:

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- `dotnet run --file source/container-apps/web/features/settings/update-assistant-name/update-assistant-name-tests.cs` — passed, 24 tests, including `From_Should_CopyName_And_Version_From_GetSiteSettings`.
- `dotnet run --file source/container-apps/web/features/settings/update-xai-byok-mode/update-xai-byok-mode-tests.cs` — passed, 11 tests, including `From_Should_CopyMode_And_Version_From_GetSiteSettings`.
- `tests/container-apps/web/web-spa-integration-tests/features/admin/ai-page-assistant-draft-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). CI runs the project. `dotnet run --project tests/container-apps/web/web-spa-integration-tests -c Release --no-build -- --filter-class AiPage_Should_` — passed, 7 tests.
- `ganda repo audit` — exit 0. 42 passed, 3 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing; gitignored local state, pre-existing). `analyzer-suppressions` passed: 843 suppressions, none above the baseline.

## Session

- Implementation: Grok 4.7, 2026-11-23. The assistant-name and key-policy forms copy `GetSiteSettings.Response` with `From`. A state action reconciles the open drafts in `ShouldReRender`, because `OnParametersSet` does not run for that re-render. An edit stays, and takes the new version when its own stored field is unchanged.

## Parent

- 489
