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

## Checklist

- [ ] Assistant section on the AI page, removed from Authentication
- [ ] `UpdateAssistantName` contract, endpoint, handler, and validator; `AssistantName` out of
      `UpdateSiteSettings`
- [ ] State action and identity refresh wired to the new command
- [ ] Agent action catalog and tests updated
- [ ] Ingress golden regenerated if routes changed (`dev regenerate-generated`)
- [ ] Tests: endpoint (403 without `SettingsWrite`, version conflict 409, invalid name 400),
      SPA integration (the AI page renames, and the button and panel update; the Authentication
      page has no Assistant section), and existing assistant-identity tests green
- [ ] `dev build` 0/0

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-26)

## Notes

Requested by Steve 2026-10-26.
