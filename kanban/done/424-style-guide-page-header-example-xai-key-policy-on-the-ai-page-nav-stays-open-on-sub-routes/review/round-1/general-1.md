# Round 1 — general-1
**Date:** 2026-10-28
**Scope reviewed:** item 2, the xAI key policy (`XaiByokMode`) move. `UpdateXaiByokMode` contracts,
handler, and tests; `UpdateSiteSettings` changes; site-settings SPA state; `AiPage.razor(.cs)`;
`AuthenticationPage.razor`; endpoint, catalog-agent, and Playwright tests.

## Summary

The server side matches task 418's `UpdateAssistantName`: `[Operation]`, `SettingsWrite`,
an optimistic `Version`, a 409 on a stale Version or a concurrency conflict, a 503 when the table
is missing, and a write to the mode only. `UpdateSiteSettings` no longer carries or writes the
mode, and the endpoint test that an Authentication save keeps the policy would have failed on the
old code. The host-key read-only view uses `XaiApiKeyState.ManagedByHost`, the same check the
resolver uses. The defect is on the AI page. It now has two forms that share one `Version`, and an
edited draft never takes a newer Version.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:162-216
- Description: `SyncUneditedAssistantDraft` and `SyncUneditedByokDraft` return early when the draft
  is edited, so its `Version` stays where it was. Steps: load `/Admin/AI` at v5, change the policy
  select without saving, then save the assistant name (the row is now v6). Saving the policy sends
  v5 and gets a 409. It keeps getting a 409 until the user reverts the select or reloads. The
  reverse also happens: an unsaved name edit, then a policy save. The commands each write one
  field, so the page itself causes the conflict. Before this change the AI page had only one
  versioned form.
- Suggestion: when `SiteSettingsState.Version` advances and the server value of this draft's own
  field still equals its synced value, copy the new Version into the draft (`ByokCommand.Version`,
  `SyncedByokVersion`) and keep the edit. Do the same for the assistant draft. A real concurrent
  change to the same field still gets a 409.
- Status: open

### Issue 2 — Severity: bug
- File: skills/tw-deploy/SKILL.md:384
- Description: this skill ships in generated apps and still says the BYOK policy is set "on Admin →
  Authentication". It is now on Admin → AI, and it is read-only when the host key is set.
- Suggestion: say Admin → AI and that the host key makes it read-only. Run `dev lint-skills`.
- Status: open

### Issue 3 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/admin/ai-page-assistant-draft-tests.cs
- Description: task 418's draft-sync render tests were not extended to the policy form. The page
  renders `data-xai-byok-version`, but no test reads it. No SPA test checks `XaiByokModeSaveFailed`,
  and no browser test saves the policy. The spec asks to reuse 418's handling of refreshing the
  form after an update, but no test covers it, and none covers Issue 1.
- Suggestion: add render tests in the same style:
  - a stale snapshot is replaced by the server Version;
  - an agent `UpdateXaiByokMode` is copied into an unedited draft;
  - after an assistant rename, the policy draft takes the new Version.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/settings/site-settings-state/site-settings-state.agent-page-facts.cs:5-37
- Description: the agent can now run `UpdateXaiByokMode`, but the page facts it reads have no
  `xaiByokMode`, so it cannot read or report the current policy. Task 418 added `assistantName`
  here. The Design region lists only `UpdateSiteSettings` and `UpdateAssistantName`.
- Suggestion: add `["xaiByokMode"]`, and optionally say whether the host manages the key. Update
  the Design region and extend the `/Admin/AI` assertions in `catalog-agent-tests.cs`.
- Status: open

### Issue 5 — Severity: nit
- File: tests/container-apps/web/web-spa-playwright-tests/clear-xai-api-key-playwright-tests.cs:1-12
- Description: the Purpose and Design regions describe only the clear-key flow. They don't mention
  the new host-key test (`XaiChatOptions.ApiKey` setup) or its check that Authentication no longer
  has the section.
- Suggestion: update the regions (`tw-agent-context-regions`).
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/features/authorization/permission-ids-contracts.cs:26-27
- Description: the `SettingsWrite` design note lists what it gates as "/Admin/AI plus
  UpdateAssistantName". It leaves out `UpdateXaiByokMode`.
- Suggestion: add it.
- Status: open
