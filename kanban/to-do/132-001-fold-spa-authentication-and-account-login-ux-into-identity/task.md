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

- [x] Move authentication adapters + login/logout pages into identity
- [x] Dispose or fold AccountState wallet fields
- [x] Namespace + global usings + tests
- [x] Confirm `/authentication/{action}`, `/Login`, `/Logout` unchanged
- [x] `dev build` 0/0; SPA integration filter for login return-url

## Notes

Disposition and inventory: `kanban/…/132-review-auth-authentication-authorization-feature-folder-naming-and-placement/{disposition,inventory}.md`.

## Session

- Created: 3992340 (2026-09-04)
- Implementer: grok session 01a07f99-f513-7810-89c0-c532ce507843 (2026-09-08)

## Results

Folded SPA `features/authentication/` and `features/account/` into `features/identity/` so the WASM client matches the server identity umbrella (104-021 / 132). Naming + namespace only: Entra / passkey / mock behavior, routes, and type names (`LoginPage`, `AuthenticationStateListener`, `AccountClaimsPrincipalFactoryWithRoles`) are unchanged.

**What moved (under `web-spa/features/identity/`)**

- Adapters at slice root: `AuthenticationStateListener.razor` + `.razor.cs`, `account-claims-principal-factory-with-roles.cs`
- Pages: `pages/login-page/LoginPage.razor(+.cs)`, `pages/LogoutPage.razor(+.cs)`, `pages/Authentication.razor` (`@page "/authentication/{action}"`), `pages/RedirectToLogin.razor`
- Test: `tests/container-apps/web/web-spa-integration-tests/features/identity/login-return-url-tests.cs`

**Namespaces**

- `Features.Authentication` and `Features.Account` → `Features.Identity`
- Dropped those usings from `web-spa/_Imports.razor` and `global-usings.cs` (`Features.Identity` was already present)

**AccountState**

- Deleted entirely. `Alias` / `WalletAddress` / `SessionToken` / `IsAuthenticated` had no product or test consumers (`NoSubAccountState` unused; login uses `PasskeyCeremonyClient.GetIsAuthenticatedAsync`). No leftover wallet slice.

**TWA0009**

- Listener: dropped `CrossSliceReference(typeof(CredentialsState))` (now same slice). Kept Profile + Authorization opt-outs.
- Factory: kept Authorization opt-out, retargeted to identity types.
- HomePage reason text now says Identity login.

**Kept**

- Routes `/authentication/{action}`, `/Login`, `/Logout`
- `web-spa/features/authorization/`
- `web-spa/services/identity-session-*` and mock auth registration
- No server contract moves, no `GetCurrentUser` rename, no `features/auth/`

**Test outcomes**

- `dotnet run tools/dev-cli/dev.cs -- build` → 0 Warning(s), 0 Error(s)
- `cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class GetSafeReturnUrl` → 4 passed

### How to validate

**Smoke**

```bash
test ! -d source/container-apps/web/projects/web-spa/features/authentication
test ! -d source/container-apps/web/projects/web-spa/features/account
test -d source/container-apps/web/projects/web-spa/features/identity
test -d source/container-apps/web/projects/web-spa/features/authorization
test -f source/container-apps/web/projects/web-spa/services/identity-session-authentication-registration.cs
rg -n '@page "/authentication/\{action\}"|\[Page\("/Login"\)\]|\[Page\("/Logout"\)\]' \
  source/container-apps/web/projects/web-spa --glob '*.{cs,razor}'
rg -n 'Features\.(Account|Authentication)' --glob '*.{cs,razor}' source tests || true
```

**Expect**

- First two `test ! -d` succeed; identity + authorization + identity-session service still present.
- Routes still declared at:
  - `features/identity/pages/Authentication.razor` → `/authentication/{action}`
  - `features/identity/pages/login-page/LoginPage.razor.cs` → `[Page("/Login")]`
  - `features/identity/pages/LogoutPage.razor.cs` → `[Page("/Logout")]`
- `rg` for `Features.Account` / `Features.Authentication` in `source/` and `tests/` prints nothing.
- Optional UI (`dotnet run tools/dev-cli/dev.cs -- run`): `/Login` is still the passkey card, `/Logout` the signed-out confirmation, `/authentication/login` still `RemoteAuthenticatorView`. Entra vs passkey vs mock is unchanged.

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)

cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class GetSafeReturnUrl
# expect: 4 passed (GetSafeReturnUrl_Should)
```

**Not in scope:** live WebAuthn ceremony, Entra MSAL sign-in, `GetCurrentUser` rename, server identity contracts.