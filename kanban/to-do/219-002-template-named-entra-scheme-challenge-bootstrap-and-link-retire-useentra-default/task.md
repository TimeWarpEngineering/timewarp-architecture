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

- [ ] Named `entra` scheme; identity-session default
- [ ] Challenge + OnTicketReceived link/bootstrap/sync-hit
- [ ] Config `Authentication:Entra:*`; obsolete `UseEntra` synonym
- [ ] Delete 104-021 “Entra owns default” branch
- [ ] Integration tests without a live tenant (fake OIDC handler / test double)

## Notes

RFC: `rfc/rfc.md` §3 fork 1, §4 D4/D10, §5 sequences, §7.1.

Depends on **219-001** (`EntraAccount` type + Restore + helpers) being merged.

SPA “Continue with Microsoft 365” may land here or as a thin button that hits the BFF challenge; no WASM MSAL as the session.

## Depends on

- 219-001

## Session

- Created: 3424101 (2026-09-14)
