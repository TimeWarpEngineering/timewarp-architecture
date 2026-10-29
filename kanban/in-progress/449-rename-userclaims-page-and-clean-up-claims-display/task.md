# Rename UserClaims page and clean up claims display

## Description

`Developer/UserClaims` (`UserClaims.razor` + `UserClaimsPage.razor[.cs]`) shows live
`AuthenticationState` claims but labels them `Claims from the ID token`, and renders a raw
`{Name} is authenticated.` line at the top. No ID token reaches WASM in the
identity-session path — permissions/roles are projected from `GET api/identity/session`
(`IdentitySessionAuthenticationStateProvider`). Clean up naming, route/Title, and display.

Source: https://arch.timewarp.work/Developer/UserClaims thread 2026-10-28.

## Checklist

- [x] Rename/align files: `UserClaims.razor` component vs `UserClaimsPage.razor` page — follow `tw-blazor` / `tw-feature-placement` conventions
- [x] Review route `/Developer/UserClaims`, `Title = "User Claims"`, `[Page(...)]` metadata (policy `DeveloperClaimsRead`, NavGroup/Order, Description)
- [x] Fix heading `Claims from the ID token` → `Claims from AuthenticationState` (with source subtitle: identity-session via `GetCurrentSession` / mock when `UseMock`)
- [x] Clean up `AuthMessage` (`{Name} is authenticated.` / `The user is NOT authenticated.`) — ugly, remove or restyle into page header/empty state
- [x] Fix `else` branch rendering literal `_authMessage` (`UserClaims.razor:79`) and empty-`ReturnClaims`-means-all filter
- [x] Verify admin + developer views; `dev build` 0 warnings/errors

## Goal (implementer restatement)

The developer claims page mislabels its data: it says "Claims from the ID token" even though
the SPA never holds an ID token. The claims come from Blazor `AuthenticationState`, built by
`IdentitySessionAuthenticationStateProvider` (GET api/identity/session) or by
`MockAuthenticationStateProvider` when `Authentication:UseMock` is on. Rename the page and
component so they are distinct and describe what they show, label the data source honestly,
drop the raw "{Name} is authenticated." line, fix the `else` branch that prints the literal
text `_authMessage`, and remove the dead `ReturnClaims` filter (empty means all).

Acceptance criteria:

- Page and component no longer share the `UserClaims` stem: page `ClaimsPage`
  (`features/developer/pages/ClaimsPage.razor[.cs]`), component `ClaimList`
  (`features/developer/components/ClaimList.razor`), matching `ServicesPage` / `ServiceList`.
- Route `/Developer/Claims`, Title `Claims`; `[Page]` keeps policy `DeveloperClaimsRead`,
  NavGroup `Developer`, NavOrder 340, Description names `AuthenticationState`.
- Page header description says the claims come from `AuthenticationState` and names the
  source provider (identity session via GetCurrentSession, or mock when UseMock is on).
- No `AuthMessage`; no literal `_authMessage`; no `ReturnClaims` filter. Claims render as a
  Type/Value table; no claims → `EmptyState`; signed out → `EmptyState`.
- Page registry test and comments that referenced `UserClaims` updated.
- `dev build` 0 warnings / 0 errors; `ganda repo audit` clean.

## Session

- Created: ses_e7bddb50dffeHlwyQEr0G4OEzc (2026-10-28)
- 2026-10-30 implementer (claude-opus-5-5, ganda task work): rename + display cleanup, gates green.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session abbc8da5-7380-48cf-b4ac-f129f7a9c206, max-turns 500, Done — 2026-10-29T19:46:43Z
- 2026-10-30 review oracle (claude-fable-5-1, ganda task work, effort 2, roster general, turn cap 120): round 1, 0 bugs, 0 suggestions, 1 nit wontfix; disposition accepted-exceptions. Gates re-run in worktree: build 0/0, fast 14/14, web-spa-integration 569/569, audit passes.

## Results

- Renamed page `UserClaimsPage` → `ClaimsPage` (`features/developer/pages/ClaimsPage.razor[.cs]`)
  and component `UserClaims` → `ClaimList` (`features/developer/components/ClaimList.razor`),
  the same page/list split as `ServicesPage` / `ServiceList`.
- `[Page]`: route `/Developer/Claims`, Title `Claims`, Description now names
  `AuthenticationState`; policy `DeveloperClaimsRead`, NavGroup `Developer`, NavOrder 340 kept.
  The old `/Developer/UserClaims` URL is gone (developer-only diagnostic page; no redirect).
- Page header description (TimeWarpPage `Description` slot, `data-qa="ClaimsSource"`) reads
  "Claims from AuthenticationState. Source: identity session (GetCurrentSession, GET
  api/identity/session)." or "... Source: mock authentication (Authentication:UseMock)." when
  the registered provider is `MockAuthenticationStateProvider`.
- `ClaimList`: removed `AuthMessage`, the literal `_authMessage` else branch, and the dead
  `ReturnClaims` filter. Claims render in a `twe-table` Type / Value table
  (`data-qa="ClaimList"`); URI claim types show their last segment, with the full type as the cell
  title. No claims → `EmptyState` "No claims"; signed out → `EmptyState` "Not signed in".
  Purpose/Design comment added.
- Updated `page-registry-tests.cs` (`UserClaimsPage` → `ClaimsPage`) and three comments that
  pointed at "UserClaims".
- Gates: `dev build` 0 warnings / 0 errors; web-spa-integration-tests 569/569; `dev test -g fast`
  14/14 projects; `ganda repo audit` passes (one advisory `kanban-landed` warning about other
  tasks' claims with no worktree: 070-001, 104-024..026, 123, 141, 198, 244 — board state outside
  this task, so it was not changed here).
- I did not do a live browser check of the admin and developer views here (headless, and workers do
  not run AppHost). Use the steps below.

### Review

- Rounds: 1. Roster: general (review oracle, claude-fable-5-1), effort 2.
- Final counts: bug 0 / suggestion 0 / nit 1 (wontfix). Open: 0.
- Disposition: **accepted-exceptions**. M1 (nit): the "Verify admin + developer views" checklist
  tick has no live browser check behind it; workers must not run the AppHost in a task worktree,
  Results disclose the gap, and the "How to validate" steps are the operator smoke. No product
  change requested. Restatement matches the task; no misread.
- Reviewer re-verified gates in this worktree: `dev build` 0 warnings / 0 errors, `dev test -g fast`
  14/14, web-spa-integration-tests 569/569, `ganda repo audit` passes (same `kanban-landed`
  advisory). Repo sweep for `UserClaims` / `User Claims` outside `kanban/`: no hits.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.

### How to validate

Smoke: `dev run`, sign in as an account holding `DeveloperClaimsRead` (admin, or a developer
role), open Developer → Claims (`/Developer/Claims`). Repeat with `Authentication:UseMock=true`
in Development.

Expect: Title "Claims"; the header line says "Claims from AuthenticationState" and names identity
session (or mock with UseMock); there is no "… is authenticated." line; a Type / Value table lists
`nameidentifier`, `timewarp:principal_id`, `role` (RoleId GUIDs), and permission claims. An account
without `DeveloperClaimsRead` does not see the nav item and gets Forbidden at the route. The old
`/Developer/UserClaims` returns Not Found.

## Notes

Live claims source, not token:
`features/developer/components/UserClaims.razor:37-53`,
`services/identity-session-authentication-state-provider.cs:57-94`,
`features/identity/get-current-session/get-current-session-handler-application.cs:46-55`.
Mock path synthesizes `AuthenticationState` locally
(`services/mocks/mock-authentication-state-provider.cs:30-46`).
