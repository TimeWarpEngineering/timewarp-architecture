# Round 1 — general
**Date:** 2026-10-26
**Scope reviewed:** branch `task/418-move-the-assistant-name-setting-from-the-authentic` at
`4339ef879` vs merge-base `52fc38875` (master). Product paths under `source/` and `tests/`.

## Summary

The change moves the Assistant section from Admin → Authentication to Admin → AI. It adds an
`UpdateAssistantName` contract (PUT `api/settings/assistant`, `settings.write`, `[Operation]`,
name plus `Version`), a handler, a validator, and a `SiteSettingsState.UpdateAssistantName` action.
`AssistantName` comes off `UpdateSiteSettings`. The identity refresh handler now listens to the new
action. The contract, handler, agent catalog, refresh wiring, and tests match the task and the
implementer restatement. The one real defect is on the AI page. It reuses the site-settings
snapshot taken at sign-in and never refetches it, so the rename form's `Version` goes stale and the
page can return 409 on every retry until a full page reload.

Gates re-run in the claim worktree: `dev build` 0 warnings / 0 errors. `dev test --group fast`
14/14 projects passed. `dev test --group integration` 8/8 projects passed (web-server, web-spa
integration included). The co-located runfiles `update-assistant-name-tests.cs` (23/23) and
`update-site-settings-tests.cs` (11/11) passed. The Playwright group (`ask-sign-in-playwright-tests`
rename flow) was not re-run.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:102
- Description: `LoadAsync` fetches site settings only when `!SiteSettingsState.HasSnapshot`.
  `AuthenticationStateListener` already fetches them at sign-in, so on the AI page the snapshot is
  almost always present and the page never refetches. `AssistantCommand.Version` is the sign-in
  value. If anything else advanced `Version` after sign-in (another administrator, another tab,
  the agent in another tab), the first save returns 409. `AssistantNameSaveFailed` keeps the stale
  draft, so every retry also returns 409. Leaving and reopening Admin → AI does not help, because
  the snapshot still exists. Only a full reload recovers. Admin → Authentication avoids this
  because its `OnInitializedAsync` always dispatches `FetchSiteSettings` before
  `SyncDraftFromState`. The old Assistant section, on that page, therefore never had this problem.
  The same stale snapshot also shows an outdated name in the text box.
- Suggestion: Fetch site settings on every interactive init of the AI page, as AuthenticationPage
  does, then `SyncAssistantDraft`. Keep the `HasSnapshot` guard only for the xAI key status, or
  give that its own reason. Add a test that loads a stale `SiteSettingsState` snapshot, opens the
  AI page interactively, and checks that the draft `Version` matches the server.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:110
- Description: `AiPage.razor.cs` declares `AgentActions = [UpdateAssistantNameActionSet.Action]`,
  so the agent can rename the assistant while an administrator has Admin → AI open. A successful
  agent rename updates `SiteSettingsState.AssistantName` and `Version`. The page's
  `AssistantCommand` is copied only on init and after the page's own save, so the text box keeps
  the old name and the old `Version`. The next manual save then returns 409 (see Issue 1 for the
  stuck-retry consequence). The top bar shows the new name while the form on the same page shows
  the old one.
- Suggestion: Re-sync the draft when the stored `Version` moves past the draft's version and the
  user has not edited the field. Alternatively, on a 409 from `UpdateAssistantName`, refetch site
  settings so the page recovers by itself. The Issue 1 fix (refetch on init) plus a refetch on 409
  covers both.
- Status: open
