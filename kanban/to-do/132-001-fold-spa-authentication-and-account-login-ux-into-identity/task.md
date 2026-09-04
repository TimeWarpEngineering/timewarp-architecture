# Fold SPA authentication and account login UX into identity

## Parent

132

## Description

Mechanical SPA rehome from task **132** disposition: collapse remaining near-synonym folders
`web-spa/features/authentication/` and `web-spa/features/account/` into
`web-spa/features/identity/` so the SPA matches the server identity umbrella (104-021 / 132).

This is naming + namespace only. Do not change Entra vs passkey vs mock behavior.

## Depends on

- 132

## Requirements

- Rehome `web-spa/features/authentication/*` (AuthenticationStateListener, AccountClaimsPrincipalFactoryWithRoles) under `web-spa/features/identity/`.
- Rehome `web-spa/pages/Authentication.razor` and `RedirectToLogin.razor` to identity (feature folder or `pages/` with identity namespace).
- Rehome `LoginPage` / `LogoutPage` under identity.
- Inspect `AccountState` (`Alias`, `WalletAddress`, `SessionToken`, `IsAuthenticated`): delete dead wallet demo fields if unused; otherwise keep only session fields under identity. Do not invent a fourth slice for leftover wallet state.
- Namespace `Features.Authentication` and `Features.Account` → `Features.Identity`. Update `_Imports.razor`, `global-usings.cs`, and tests (`tests/container-apps/web/web-spa-integration-tests/features/account/login-return-url-tests.cs` and any other Account/Authentication usings).
- **Keep routes:** `/authentication/{action}` (Entra `RemoteAuthenticatorView` convention), `/Login`, `/Logout`.
- **Keep** `web-spa/features/authorization/` (real concern; not part of this fold).
- **Keep** `web-spa/services/identity-session-*` and mock auth registration in the artifact `services/` folder (bootstrap, not a slice).
- No server contract moves. No `GetCurrentUser` rename. No `features/auth/` resurrection.
- Follow `tw-feature-placement` SPA exception (no layer-suffix grammar on Razor) and TWA0009 (`[CrossSliceReference]` on the listener/factory stay, retargeted to identity types as needed).

## Checklist

- [ ] Move authentication adapters + login/logout pages into identity
- [ ] Dispose or fold AccountState wallet fields
- [ ] Namespace + global usings + tests
- [ ] Confirm `/authentication/{action}`, `/Login`, `/Logout` unchanged
- [ ] `dev build` 0/0; SPA integration filter for login return-url

## Notes

Disposition and inventory: `kanban/…/132-review-auth-authentication-authorization-feature-folder-naming-and-placement/{disposition,inventory}.md`.

## Session

- Created: 3992340 (2026-09-04)

## Results

_Fill after implementation._
