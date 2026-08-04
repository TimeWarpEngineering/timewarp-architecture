# Fix SPA sign-out when Entra/MSAL is not registered (RemoteAuthenticatorView)

## Parent

104

## Description

Profile menu **Sign out** crashes the WASM app when the default (non-Entra) identity path is active.

```
Unhandled exception rendering component: Cannot provide a value for property
'AuthenticationService' on type
'Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticatorView'.
There is no registered service of type
'IRemoteAuthenticationService`1[RemoteAuthenticationState]'.
```

**Root cause (confirmed in tree):**

- `Profile.razor` always signs out via MSAL helper:
  `NavigationManager.NavigateToLogout("authentication/logout")`
- That routes to `pages/Authentication.razor`, which always renders
  `<RemoteAuthenticatorView Action=@Action/>`
- After **104-021**, default SPA auth is **identity-session / passkey** (or mock) —
  `AddOidcAuthentication` / `IRemoteAuthenticationService` is only registered when
  `Authentication:UseEntra` is true
- **104-021** fixed unauthorized → `/Login` (not RemoteAuthenticatorView) but left
  **sign-out** on the Entra path

## Requirements

- Sign out works for each registered SPA auth mode without throwing:
  1. **Identity-session / passkey** (default non-mock)
  2. **Mock** auth (Development/Testing + `UseMock`)
  3. **Entra / MSAL** when `UseEntra=true` (keep RemoteAuthenticatorView logout)
- Identity-session sign-out must clear the **server identity-session cookie** (and SPA
  auth state), not only navigate client-side
- No dependency on `IRemoteAuthenticationService` when Entra is not registered
- Manual smoke + automated test or co-located runfile covering non-Entra sign-out
- `### How to validate` in Results before done (tw-agent-collaboration)

## Checklist

- [ ] Profile menu Sign out branches on auth mode (or shared sign-out service)
- [ ] Identity-session: call server logout/session-clear endpoint if one exists; else add it
- [ ] Mock: clear mock principal / navigate to home or Login without RemoteAuthenticatorView
- [ ] Entra: keep `NavigateToLogout("authentication/logout")` + RemoteAuthenticatorView
- [ ] `Authentication.razor` only used when Entra services are registered (or guard so it is never hit off-Entra)
- [ ] Tests green; `./bin/dev build` 0/0
- [ ] Results include `### How to validate`

## Notes

### Likely touch points

| File | Issue |
|------|--------|
| `web-spa/features/profiles/components/Profile.razor` | `HandleSignOut` → `NavigateToLogout("authentication/logout")` |
| `web-spa/pages/Authentication.razor` | Unconditional `RemoteAuthenticatorView` |
| Identity session | Need server logout for passkey cookie (`identity-session` scheme on web-server) |
| `program.cs` (SPA) | Auth registration branches from 104-021 |

### Related

- 104-016 passkey-first demo
- 104-021 Entra non-default / identity-session default
- RedirectToLogin already avoids RemoteAuthenticatorView; sign-out must match

### Depends on

None (bugfix on shipped default path).

## Session

- Created: 2026-08-04 (user repro: Sign out from menu → RemoteAuthenticatorView DI failure)
