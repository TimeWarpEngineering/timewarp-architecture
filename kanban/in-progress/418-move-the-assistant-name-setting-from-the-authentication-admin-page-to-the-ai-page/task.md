# Move the Assistant name setting from the Authentication admin page to the AI page

## Description

Steve, 2026-10-26, with a screenshot of the Assistant section: "Why is assistant Admin UX on the
Authentication Page and not the AI page?"

Task 361 put an **Assistant** section (Assistant name; the avatar is generated from the name) on
Admin → Authentication (`web-spa/features/admin/site-settings/pages/AuthenticationPage.razor`). It
was there only because `AssistantName` is a `SiteSettings` field saved by the same
`UpdateSiteSettings` command. The assistant is an AI concern. Admin → AI
(`web-spa/features/admin/ai/pages/AiPage.razor`) already owns the site xAI key, so the assistant
identity belongs there. Authentication should hold only sign-in policy.

## Requirements

- **Move the UI.** Remove the Assistant `FormSection` from the Authentication page and add it to
  the AI page. Keep the same copy, `data-qa="AssistantSettings"`, and validation
  (`AssistantNameRules`).
- **Give it its own command.** The section should not keep riding on `UpdateSiteSettings`: the AI
  page would have to round-trip every authentication field to save a name. Add a dedicated
  site-settings command (for example `UpdateAssistantName`, with name and `Version` for the same
  optimistic concurrency) per `tw-web-api-contracts`. Authorize it with
  `PermissionIds.SettingsWrite`, add `[Operation]`, and give it its own TimeWarp.State action.
  Remove `AssistantName` from `UpdateSiteSettings.Command` and its details and validator.
  `GetSiteSettings` and `GetAssistantIdentity` keep returning the name.
- **Keep the live refresh.** `AgentSurfaceState`'s `AssistantIdentityRefreshHandler` reloads the
  identity after a successful `UpdateSiteSettings`. Point it at the new command, so the top-bar
  button, Ctrl-K row, and panel still update without a reload, whether the page or the agent
  saved.
- **Agent surface.** The agent can rename the assistant today through
  `SiteSettings.UpdateSiteSettings`. Expose the new action instead, and update the action catalog
  test (`action-catalog-tests.cs`) and any WebMCP or agent-page-facts listings.
- **Update the Purpose and Design regions** on both pages and on the site-settings state files
  (drop the "Task 361: an Assistant section…" note from AuthenticationPage).

## Goal (implementer restatement)

The assistant's name is an AI setting, so Admin → AI owns the Assistant section and Admin →
Authentication keeps only sign-in policy. Saving a name is its own `UpdateAssistantName` command
(name plus `Version`, `settings.write`, `[Operation]`), not a field on `UpdateSiteSettings`.
`GetSiteSettings` and `GetAssistantIdentity` still return the name. A successful rename, from the
AI page or from the agent action, reloads the public identity so the top-bar button, Ctrl-K row,
and Ask panel update without a full page reload. The agent catalog exposes the new action instead
of renaming through `UpdateSiteSettings`.

The AI page's draft has to be the server's current `Version`, not the copy sign-in already
loaded. Opening Admin → AI fetches site settings every time, the same way Authentication does,
then copies that snapshot into the form. A SPA test seeds a stale snapshot and checks that the
draft version matches the server after the page opens. While the page is open, an agent rename
that advances `Version` copies the new name and version into the form when the field is still
unedited, so the form and the top bar stay on the same name and the next save is not a 409.

## Checklist

- [x] Assistant section on the AI page, removed from Authentication
- [x] `UpdateAssistantName` contract, endpoint, handler, and validator; `AssistantName` out of
      `UpdateSiteSettings`
- [x] State action and identity refresh wired to the new command
- [x] Agent action catalog and tests updated
- [x] Ingress golden regenerated if routes changed (`dev regenerate-generated`)
- [x] Tests: endpoint (403 without `SettingsWrite`, version conflict 409, invalid name 400),
      SPA integration (the AI page renames, and the button and panel update; the Authentication
      page has no Assistant section), and existing assistant-identity tests green
- [x] `dev build` 0/0
- [x] Admin → AI refetches site settings on every interactive open, then copies that `Version`
- [x] An unedited draft follows an agent rename's new name and `Version`
- [x] SPA test: stale snapshot, then the draft version matches the server after open

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-26)
- Implementation: grok task-work (2026-10-26). The product commit's attest skipped
  signing because `runfile-executable` saw `update-assistant-name-tests.cs` and the restaged
  `update-site-settings-tests.cs` without the executable bit. Restored both. Other live claims
  (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244) have no usable worktree. Left alone;
  those claims are not this task.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-26T08:52:19Z
- Review oracle round 1 (2026-10-26): claude-opus-5-5, effort 3, roster general. Fix pass
  requested for M1 and M2.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-26T08:59:36Z
- Fix pass (implement-fix-2, 2026-10-26): interactive Admin → AI always fetches site settings,
  and an unedited draft copies an agent rename's new name and version.

## Results

Admin → AI owns the Assistant section (`data-qa="AssistantSettings"`, same copy and
`AssistantNameRules` validation). Admin → Authentication no longer renders it. Saving a name is
`UpdateAssistantName` (PUT `api/settings/assistant`, `settings.write`, operation
`assistant-name.update`, name plus `Version`). `UpdateSiteSettings` no longer accepts
`AssistantName`. `GetSiteSettings` and `GetAssistantIdentity` still return the stored name. A
successful rename, from the AI page or from `SiteSettings.UpdateAssistantName`, reloads the
public identity, so the top-bar button, Ctrl-K row, and Ask panel update without a page reload.

`dev regenerate-generated` rewrote the ingress prefix golden and left it unchanged. The new route
stays under the existing `api/settings` prefix.

Opening Admin → AI fetches site settings on every interactive init, including when sign-in
already loaded a snapshot, and copies that name and `Version` into the form. While the page is
open, a render copies a newer stored name and `Version` into the draft when the field is still
the last synced value. A name the person has typed is left in place.

### How to validate

Smoke: Sign in as an administrator. Change any other site setting so the settings `Version`
advances, then open Admin → AI without a full reload. Change Assistant name from Akili to Zawadi
and click Save assistant name. With Admin → AI still open and the name field untouched, ask the
agent to rename the assistant to Nia. Open Admin → Authentication on the same session.

Expect: The first save succeeds (no 409) and the top-bar button, Ctrl-K Ask row, and Ask panel
use Zawadi without a full page reload. After the agent rename, the Assistant name field and its
version show Nia, and the top bar reads "Ask Nia", without a reload. Admin → Authentication has
no Assistant section. A member PUT to `api/settings/assistant` returns 403, a stale version
returns 409, and a name that contains markup returns 400.

### Review

Round 1 (general, effort 3): 1 bug and 1 suggestion. Fix pass implemented both. Round 2 has not
run. The reviewer re-ran the gates in the claim worktree before the fix: `dev build` 0/0, the
fast group 14/14, the integration group 8/8, and both settings runfiles. The Playwright group
was not re-run. Artifacts: `review/review-framework.md`, `review/round-1/general.md`,
`review/round-1/merged.md`.

- M1 (bug): fixed. Interactive init always dispatches `FetchSiteSettings`, then copies the
  snapshot into the draft. `AiPage_Should_.Replace_A_Stale_Snapshot_With_The_Server_Version`
  seeds version 1 and asserts the form attribute is the server's version 7.
- M2 (suggestion): fixed for an unedited field. The render copies the stored name and version
  when they advance and the field still matches the last sync.
  `AiPage_Should_.Copy_An_Agent_Rename_Into_An_Unedited_Draft` covers that. A typed edit stays.

## Review

- Round 1: findings M1 and M2 implemented on this branch. Round 2 has not run.

## Notes

Requested by Steve 2026-10-26.
