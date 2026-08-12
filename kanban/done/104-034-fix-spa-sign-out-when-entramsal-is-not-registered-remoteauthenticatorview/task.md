# Fix SPA sign-out when Entra/MSAL is not registered (RemoteAuthenticatorView)

## Parent

104

## Description

Profile menu **Sign out** crashed WASM when default (non-Entra) identity path is active because
`NavigateToLogout` always rendered `RemoteAuthenticatorView` without `IRemoteAuthenticationService`.

## Requirements

- Sign out works for identity-session, mock, and Entra modes
- Identity-session clears server cookie
- No RemoteAuthenticatorView when Entra is off
- Tests + How to validate

## Checklist

- [x] Profile menu Sign out branches on auth mode (`SpaSignOutService`)
- [x] Identity-session: `POST api/identity/session/end` + cookie clear
- [x] Mock: end session + forceLoad Login
- [x] Entra: keep `NavigateToLogout("authentication/logout")`
- [x] Authentication.razor only used when Entra path navigates there
- [x] Tests green; `./bin/dev build` 0/0
- [x] Results include `### How to validate`

## Notes

### Implementation plan

1. Add `IBrowserSessionService.SignOutAsync` + cookie impl
2. `EndBrowserSession` contract/handler `POST api/identity/session/end`
3. `SpaSignOutService` mode branch
4. Profile.razor uses service
5. Integration tests EndBrowserSession_

## Session

- Created: 2026-08-04
- Implement + review: 2026-08-04 (orchestrate 104-034)

## Results

### Summary

Fixed default-path SPA sign-out: profile menu no longer always calls MSAL
`NavigateToLogout`. New **`SpaSignOutService`** branches:

| Mode | Behavior |
|------|----------|
| **UseEntra** | `NavigateToLogout("authentication/logout")` + RemoteAuthenticatorView |
| **Identity-session** | `POST api/identity/session/end` → clear cookie → notify ASP → `/Login` |
| **Mock** | End session (noop) + forceLoad `/Login` |

Server: `EndBrowserSession` + `CookieBrowserSessionService.SignOutAsync`.

### Files changed

| Path | Role |
|------|------|
| `platform/identity-host/i-browser-session-service-application.cs` | `SignOutAsync` port |
| `platform/identity-host/cookie-browser-session-service-server.cs` | Cookie sign-out |
| `features/identity/end-browser-session/*` | Contract + handler |
| `web-spa/services/spa-sign-out-service.cs` | Mode-aware SPA sign-out |
| `web-spa/features/profiles/components/Profile.razor` | Uses SpaSignOutService |
| `web-spa/program.cs` | DI register |
| `tests/.../end-browser-session-tests.cs` | Integration 2/2 |

### Key decisions

- Logout endpoint AllowAnonymous + idempotent (no session = success)
- Entra path unchanged (RemoteAuthenticatorView only when MSAL registered)
- CookieContainer in tests applies expired Set-Cookie after SignOut (browser parity)

### Build / tests

- `./bin/dev build`: **0/0**
- `dotnet test` web-server-integration-tests `--filter-class EndBrowserSession`: **2/2**

### Review

clean, effort 1 (`review/disposition.md`)

### How to validate

**Automated**
```bash
cd tests/container-apps/web/web-server-integration-tests
dotnet test -c Release -- --filter-class EndBrowserSession
# expect: 2/2 passed
./bin/dev build
# expect: 0/0
```

**Manual smoke (default non-Entra)**
1. `./bin/dev run` → open SPA (identity-session or mock as configured)
2. Sign in (passkey on `/Login`, or mock principal if UseMock)
3. Profile menu → **Sign out**
4. **Expect:** no console exception about `RemoteAuthenticatorView` / `IRemoteAuthenticationService`
5. **Expect:** lands on `/Login` (or reload); Profile shows Sign-in; `GET /api/identity/session` → `isAuthenticated: false`

**Entra (optional)**
- Set `Authentication:UseEntra=true` and MSAL config → Sign out still uses `authentication/logout` + RemoteAuthenticatorView

**Not in scope:** progressive profile; server-side ticket revocation beyond cookie expiry.
