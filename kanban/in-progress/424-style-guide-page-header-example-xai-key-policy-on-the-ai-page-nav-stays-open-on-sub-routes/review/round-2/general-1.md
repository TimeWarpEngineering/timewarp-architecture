# Round 2 — general-1
**Date:** 2026-10-28
**Scope reviewed:** `git diff 0d475995b HEAD -- . ':!kanban'`, xAI key policy slice: `AiPage.razor`,
`site-settings-state.agent-page-facts.cs`, `ai-page-assistant-draft-tests.cs`, `catalog-agent-tests.cs`,
`skills/tw-deploy/SKILL.md`, `permission-ids-contracts.cs`, `clear-xai-api-key-playwright-tests.cs`.

## Summary

M1, M2, M4, M5, M14, and M15 are fixed. The M1 draft logic is correct in both directions. It also
holds on first render, before a snapshot exists, while a save is in flight, and when the draft's own
server field changed during an edit (the draft keeps the old Version, so it 409s and does not
overwrite). No new bugs. The branch that protects another user's change has no test.

## Prior findings

- **M1: fixed.** `SyncUneditedAssistantDraft` and `SyncUneditedByokDraft` adopt a newer
  `SiteSettingsState.Version` only when the state's own field still equals the synced value. If that
  field changed, an edited draft keeps its old Version, and an unedited draft re-syncs. Both guard on
  `*DraftLoaded && HasSnapshot`.
- **M2: fixed.** `tw-deploy/SKILL.md:384` says Required, Allow, or Off on Admin → AI, read-only when
  the host key is set.
- **M4: fixed.** The tests cover the stale snapshot (version 7 / Required), an agent rename and an
  agent policy update copied into unedited drafts, and a cross-form save in both directions
  (`Keep_A_Byok_Edit_When_The_Name_Save_Advances_The_Version`,
  `Keep_A_Name_Edit_When_The_Byok_Save_Advances_The_Version`). By reading, the cross-form tests fail
  without the fix.
- **M5: fixed.** The page facts add `xaiByokMode`. The Design region covers `UpdateXaiByokMode`, and
  the host key stays on `XaiApiKeyState`. `catalog-agent-tests.cs` asserts the fact.
- **M14: fixed.** The Purpose and Design regions describe the host-key test. Its data-qa names exist
  in `AiPage.razor`.
- **M15: fixed.** The `SettingsWrite` note names `UpdateAssistantName` and `UpdateXaiByokMode`.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/admin/ai-page-assistant-draft-tests.cs
- Description: no test covers the case where a draft is edited, its own server field changes, and
  the Version advances. In that case the draft must keep its old Version. A regression that drops the
  equality check and adopts the Version every time would pass all five tests. It would then silently
  overwrite another user's change.
- Suggestion: edit the policy draft to `Off`, then have the server report `Required` with a new
  Version (for example an agent `UpdateXaiByokMode`). Assert that `data-xai-byok-version` stays at the
  old value and the mode stays `Off`. Add the mirror test for the name draft.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor (`SyncUnedited*Draft`)
- Description: if another user changes the field A→B and then back to A between two renders, the
  edited draft adopts the new Version, and saving it overwrites the transient B. This is harmless,
  because the value is back where it started. It is the cost of having no per-field version.
- Suggestion: no change needed. Optionally note it in the Design region.
- Status: open

## Tests run

Run from `tests/container-apps/web/web-spa-integration-tests` after a Release build:
- `dotnet bin/Release/net11.0/web-spa-integration-tests.dll --filter-class "AiPage_Should_"`: 5/5
  passed.
- `--filter-class "CatalogAgent_Should"`: 47/47 passed.
- `./bin/dev` is stale (it reports `0d475995b`), so it was not used. No Playwright or Aspire host was
  run.
