# Rename UserClaims page and clean up claims display

## Description

`Developer/UserClaims` (`UserClaims.razor` + `UserClaimsPage.razor[.cs]`) shows live
`AuthenticationState` claims but labels them `Claims from the ID token`, and renders a raw
`{Name} is authenticated.` line at the top. No ID token reaches WASM in the
identity-session path — permissions/roles are projected from `GET api/identity/session`
(`IdentitySessionAuthenticationStateProvider`). Clean up naming, route/Title, and display.

Source: https://arch.timewarp.work/Developer/UserClaims thread 2026-10-28.

## Checklist

- [ ] Rename/align files: `UserClaims.razor` component vs `UserClaimsPage.razor` page — follow `tw-blazor` / `tw-feature-placement` conventions
- [ ] Review route `/Developer/UserClaims`, `Title = "User Claims"`, `[Page(...)]` metadata (policy `DeveloperClaimsRead`, NavGroup/Order, Description)
- [ ] Fix heading `Claims from the ID token` → `Claims from AuthenticationState` (with source subtitle: identity-session via `GetCurrentSession` / mock when `UseMock`)
- [ ] Clean up `AuthMessage` (`{Name} is authenticated.` / `The user is NOT authenticated.`) — ugly, remove or restyle into page header/empty state
- [ ] Fix `else` branch rendering literal `_authMessage` (`UserClaims.razor:79`) and empty-`ReturnClaims`-means-all filter
- [ ] Verify admin + developer views; `dev build` 0 warnings/errors

## Session

- Created: ses_e7bddb50dffeHlwyQEr0G4OEzc (2026-10-28)

## Notes

Live claims source, not token:
`features/developer/components/UserClaims.razor:37-53`,
`services/identity-session-authentication-state-provider.cs:57-94`,
`features/identity/get-current-session/get-current-session-handler-application.cs:46-55`.
Mock path synthesizes `AuthenticationState` locally
(`services/mocks/mock-authentication-state-provider.cs:30-46`).
