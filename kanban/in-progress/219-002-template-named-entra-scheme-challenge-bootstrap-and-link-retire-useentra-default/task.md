# Template: named entra scheme, challenge bootstrap and link, retire UseEntra default

## Parent

219

## Description

Fold-in of architecture RFC 219 fork 1 mechanics + **D4** + **D10**. Register Entra as a **named** OIDC scheme (`entra`), never DefaultScheme. identity-session stays default. Challenge for bootstrap (anonymous, trusted tenant) and link (authenticated). Retire the 104-021 `Authentication:UseEntra` default-scheme branch.

## Requirements

- `AddAuthentication(IdentitySessionDefaults.Scheme)` always; Entra via `AddOpenIdConnect("entra")` **or** `AddMicrosoftIdentityWebApp(..., openIdConnectScheme: "entra", cookieScheme: null)`
- Do **not** call `AddMicrosoftIdentityWebAppAuthentication` (it steals DefaultScheme)
- `OnTicketReceived`: validate `tid`/`oid`/`iss`; `HandleResponse()`; attach or bootstrap; `SignInAsync(identity-session)`; redirect. `prompt=select_account`
- Trusted tenants + `AllowBootstrap` config (`Authentication:Entra:*`)
- `GET api/identity/entra/challenge?mode=link|bootstrap&returnUrl=`
- Link: identity-session required; AddCredential on caller; 409 if handle owned elsewhere (no oracle)
- Bootstrap: anonymous; `tid` in TrustedTenants; Find-by-handle active → session; else Principal.Create + EntraAccount
- Synonym `UseEntra` may map to `Entra:Enabled` with an obsolete log for one version — must **never** restore default-scheme Entra
- Lock #10 preserved: Entra is not the priority path
- Tests: scheme registration; challenge requires auth for link; bootstrap refused for untrusted tid; duplicate handle 409

## Checklist

- [x] Named `entra` scheme; identity-session default
- [x] Challenge + OnTicketReceived link/bootstrap/sync-hit
- [x] Config `Authentication:Entra:*`; obsolete `UseEntra` synonym
- [x] Delete 104-021 “Entra owns default” branch
- [x] Integration tests without a live tenant (fake OIDC handler / test double)

## Notes

RFC: `rfc/rfc.md` §3 fork 1, §4 D4/D10, §5 sequences, §7.1.

Depends on **219-001** (`EntraAccount` type + Restore + helpers) being merged.

SPA “Continue with Microsoft 365” may land here or as a thin button that hits the BFF challenge; no WASM MSAL as the session.

## Depends on

- 219-001

## Session

- Created: 3424101 (2026-09-14)
- Implementer: grok session 01a0a063-633a-7921-b7fb-68be47e9ccac (2026-09-14)

## Results

Fold-in of RFC 219 fork 1 + D4 + D10 into the template: Entra is a named OIDC challenge scheme; identity-session stays DefaultScheme.

**Implemented**
- `AddAuthentication(IdentitySessionDefaults.Scheme)` always; `AddOpenIdConnect("entra")` only when `Authentication:Entra:Enabled` (or obsolete `UseEntra`) is true
- Do not call `AddMicrosoftIdentityWebAppAuthentication`; `Microsoft.Identity.Web` is no longer referenced by web-server
- `OnTicketReceived`: validate `tid`/`oid`/`iss`; `HandleResponse()`; link / sync-hit / bootstrap; `SignInAsync(identity-session)`; redirect. `prompt=select_account`
- `GET /api/identity/entra/challenge?mode=link|bootstrap&returnUrl=` — link requires identity-session (401 otherwise); bootstrap is anonymous
- Link: `AddCredential` on the caller; 409 if the `tid:oid` handle is owned elsewhere (no foreign PrincipalId in the problem)
- Bootstrap: `tid` ∈ TrustedTenants; active Find-by-handle → session (sync-hit); else `Principal.Create` + `EntraAccount` when `AllowBootstrap`
- Obsolete `Authentication:UseEntra` maps to `Entra:Enabled` and logs once; never restores Entra as DefaultScheme
- SPA: “Continue with Microsoft 365” on `/Login` (bootstrap) and “Link Microsoft 365” on `/Settings` (link). Passkey remains the primary CTA (lock #10). No WASM MSAL session

**Files (selected)**
- `source/container-apps/web/projects/web-server/program.cs` — always identity-session default; named entra when enabled
- `source/container-apps/web/features/identity/entra-authentication-registration-server.cs`
- `source/container-apps/web/features/identity/entra-ticket-processor-application.cs`
- `source/container-apps/web/features/identity/entra-ticket-http-server.cs`
- `source/container-apps/web/features/identity/challenge-entra/`
- `source/container-apps/web/features/identity/entra-authentication-options-application.cs`
- `source/container-apps/web/platform/identity-host/entra-link-defaults-server.cs`
- `source/container-apps/web/projects/web-spa/features/identity/pages/login-page/LoginPage.razor`
- `source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor`
- Deleted: SPA `Authentication.razor` (`RemoteAuthenticatorView`), `account-claims-principal-factory-with-roles.cs`, `Microsoft.Identity.Web` and `Microsoft.Authentication.WebAssembly.Msal` package references
- Tests: `entra-scheme-registration-tests.cs`, `entra-challenge-tests.cs` (fake OIDC handler, no live tenant)

**Decisions / deviations**
- Chose raw `AddOpenIdConnect("entra")` rather than `AddMicrosoftIdentityWebApp(..., cookieScheme: null)` so DefaultScheme cannot be stolen
- TrustedTenants gates bootstrap **and** sync-hit so an untrusted tenant never issues a session; link does not re-check tenant (caller already has identity-session)
- Revoked Entra handle is not Restored at ticket time (403); Graph Restore remains a later product path
- Challenge is a hand-written FastEndpoint (redirect, not mediator JSON). `MarkResponseStart` after Challenge/Redirect so FastEndpoints does not replace the status with 204
- `UseEntra` remains in appsettings as a one-version synonym (`false`); AzureAd / AzureAdB2C leftover placeholders stay commented as legacy, unused

**Test outcomes**
- `dotnet run tools/dev-cli/dev.cs -- build`: 0 Warning(s), 0 Error(s)
- `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Entra`: 12 passed, 0 failed
- `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class MockAuth`: 2 passed, 0 failed

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class EntraSchemeRegistration_
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class EntraChallenge_
```

**Expect**
- `EntraSchemeRegistration_`: default host has DefaultScheme `identity-session` and no `OpenIdConnectHandler`; with `Authentication:Entra:Enabled=true` the named scheme `entra` is `OpenIdConnectHandler` and DefaultScheme stays `identity-session`; obsolete `Authentication:UseEntra=true` registers named entra and still does not become DefaultScheme
- `EntraChallenge_`: `mode=link` without identity-session → 401; untrusted `tid` bootstrap → 403 title `Untrusted tenant`; duplicate handle link → 409 title `Credential already registered` with no foreign PrincipalId in the body; trusted-tenant bootstrap → 302 + identity-session `Set-Cookie`; sync-hit reuses the existing principal; authenticated link attaches `EntraAccount` on the caller

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Entra
# expect: passed, failed 0 (12 succeeded in this implementer run)
```

**Not in scope:** live Entra tenant / app registration / Graph delta (no confidential client round-trip). Soft-prompt passkey after Entra session is 219-003.
