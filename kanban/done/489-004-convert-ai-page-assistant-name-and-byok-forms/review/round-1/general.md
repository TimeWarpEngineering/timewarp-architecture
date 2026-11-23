# Round 1 — general
**Date:** 2026-11-23
**Scope reviewed:** commit 6da30734e, product paths: the two new `I*Details` contracts, `GetSiteSettings.Response`, the `UpdateAssistantName` and `UpdateXaiByokMode` contracts and their tests, `AiPage.razor`, `SiteSettingsState` (state, fetch, and the three update handlers), and `ai-page-assistant-draft-tests.cs`.

## Summary

The change does what the task and the restatement ask. Both commands implement a one-field `I*Details` interface. `GetSiteSettings.Response` implements both. `CopyBeyondDetails` copies `Version` only from that response. `SiteSettingsState.SiteSettings` holds the fetched response, and each successful save replaces it through `ReviseSiteSettings`. I checked the positional argument order against the `Response` constructor, and it matches. `AiPage` no longer assigns anything from the render block. Reconciliation runs in `ShouldReRender` and compares each command with the response it was copied from. The seven existing `AiPage_Should_` integration tests pass unchanged against the new code, and they cover the agent-update and cross-form reconcile paths. The recorded gates (build, both contract runfiles, the integration project, and the audit) cover the change.

The restatement matches the task. The task asked that dirty mean "differs from the stored Response". The implementation compares with the response the command was copied from (`AssistantSource` / `ByokSource`). That is the only reading that keeps an unedited draft from looking dirty when a newer response arrives, so it is not a misread. The two findings below are small: one path has no test, and two flags are redundant.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:236
- Description: The page's own-save path has no test. When the user saves their own edit, `ShouldReRender` sees a dirty command whose own field changed on the server, so it keeps the old `Version` (the case `Keep_The_Old_Name_Version_When_The_Server_Name_Changes` asserts). Only the `AcceptAssistant(stored, force: true)` call in `SaveAssistantAsync`, and `AcceptByok(stored, force: true)` in `SaveByokAsync`, then moves the saved form to the new response. Every integration test dispatches `UpdateAssistantNameActionSet` / `UpdateXaiByokModeActionSet` directly and never runs these handlers. If someone deleted the force call, every test would still pass, and the next save from that form would get a 409. The restatement names this step ("again after a successful save of that form, it replaces the command with `From(SiteSettings)`"), and only the manual smoke in `### How to validate` covers it.
- Suggestion: Add an `AiPage_Should_` case that edits the name, runs the page's submit handler, and asserts that `data-assistant-name` and `data-assistant-version` show the saved response. Add the same case for the key-policy form. The test can run the handler by reflection, the same way it already reads `AssistantCommand` (`SaveAssistantAsync` / `SaveByokAsync` on the dispatcher). Also assert that an unsaved edit in the other form is kept.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:63
- Description: `AssistantDraftLoaded` and `ByokDraftLoaded` are leftover shadow state. Each one becomes true at exactly the point where `AssistantSource` / `ByokSource` is first assigned, and neither the flag nor the source is ever reset. So `AssistantDraftLoaded == (AssistantSource is not null)` always holds, and the same goes for the key-policy pair. The task's intent is to delete the shadow tracking, and these flags duplicate a fact the source reference already holds.
- Suggestion: Delete both fields. Render on `AssistantSource is not null` / `ByokSource is not null`, or use expression-bodied `AssistantDraftLoaded => AssistantSource is not null` properties so the markup stays the same.
- Status: open
