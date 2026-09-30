# Every SPA user interaction is a TimeWarp.State action (audit, convert, enforce)

## Description

Rule (Steve, 2026-10-01): **all user interactions in web-spa are TimeWarp.State actions.** A
component event handler (`OnClick`, `OnSubmit`, `@onkeydown`, …) may only dispatch a generated
ActionSet method, and at most sequence several of them. The work itself lives in the action's
handler: navigation, full-page redirects to BFF endpoints, JS interop, API calls, and state
changes. Only purely presentational local UI state is exempt: hover, focus, whether a local
details panel is expanded, and binding the text in an input box.

Why: actions are the one seam that the action catalog (the Ctrl-K palette, and agents via
WebMCP later), Redux DevTools, and headless tests can all see. A method on a page is invisible to
all of them. Trigger case: **Link Microsoft 365** is a `SettingsPage` method that does a
forceLoad navigation to the BFF challenge (`SettingsPage.razor.cs`, RFC 219 D10), so it never
appears in the palette.

## Requirements

1. **Audit.** Walk every `.razor` / `.razor.cs` under `source/container-apps/web/projects/web-spa`
   and list each event handler that does more than dispatch actions. Look for
   `NavigationManager.NavigateTo`, `forceLoad`, `IJSRuntime` / JS modules, `HttpClient` / API
   services, and direct state mutation. Record the list in Notes with file:line and a
   disposition (convert / exempt-with-reason).
2. **Convert** every non-exempt interaction to an action on the owning feature's State. Page
   methods become thin dispatches.
   - **Link Microsoft 365** becomes `CredentialsState.LinkMicrosoft365`. Its handler performs the
     same forceLoad challenge navigation (`mode=link`, same return URL behavior).
   - Keep the existing sequencing rules (handlers do not nest dispatch; components sequence
     several actions), TWA0022 (no direct mediator `Send`), and TWA0025 (outcomes go through
     NotificationState).
3. **Catalog.** Add `[CatalogAction]` to converted actions that a person or agent would
   meaningfully run: Human / Agent / Both, the same permissions as the page gate, and a real
   Description. Link Microsoft 365 is `Visibility = Human`, `Permissions = CredentialManageSelf`.
   It is always listed for principals with that permission. When it doesn't apply (already
   linked, or Entra sign-in disabled site-wide), the challenge flow reports that. A catalog
   "is this available now" predicate is out of scope; note any case that would want one.
4. **Enforce.** Propose, in the task, an analyzer shaped like TWA0022 (gated on the Blazor WASM
   SDK) that flags razor event handlers or component methods calling `NavigationManager`,
   `IJSRuntime` or API clients directly, with an opt-out attribute that requires a reason.
   **Stop before implementing the analyzer.** Record the proposed rule, its false-positive risks
   and the opt-out shape, then leave it as an open question for Steve. Steve decides
   architecture rules.
5. **Skill.** State the rule, with its reasoning, in the owning SPA skill. Use the one that
   covers state/actions and pages (check `skills/`). Skills are public: write the rule and its
   reasoning only, no history.

## Checklist

- [ ] Audit list recorded in Notes (file:line, disposition)
- [ ] Link Microsoft 365 → `CredentialsState.LinkMicrosoft365` + `[CatalogAction]`; Settings
      button dispatches it; it appears in the Ctrl-K palette
- [ ] All other non-exempt interactions converted; Purpose/Design regions reconciled
- [ ] `[CatalogAction]` on converted user-meaningful actions
- [ ] Analyzer proposal written up as an Open Question (not implemented)
- [ ] Skill updated with the rule
- [ ] Tests: co-located Jaribu or the existing web-spa integration suites. Each converted action
      is dispatched headless and asserts its effect (navigation target, state change); the
      catalog lists Link Microsoft 365 with the right visibility and permission
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke` (the template ships these pages),
      `ganda repo audit`
- [ ] Do **not** start an AppHost (`dev run`, `aspire run`, or `dotnet run` of the AppHost);
      record the browser check as not performed
- [ ] Implementation review; host `open-pr`

## Session

- Created: 512235 (2026-09-30)

## Notes

- If the audit finds many interactions, split conversion into children by feature (for example
  settings/credentials, admin, application shell) and keep this task as the parent. Parent-done
  requires every child done.
- Memory discipline: run builds and tests serially and call `dotnet build-server shutdown`
  before finishing.

## Results

*(fill when done)*

### How to validate

*(required before done)*
