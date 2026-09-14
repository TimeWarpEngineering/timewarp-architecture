# Round 1 — general
**Date:** 2026-09-14
**Scope reviewed:** commit `82d5b714` on `task/219-002-template-named-entra-scheme-challenge-bootstrap-an` vs `origin/master` (named entra OIDC scheme, challenge bootstrap/link, retire UseEntra default)

## Summary

The fold-in correctly keeps `identity-session` as DefaultScheme, registers named `AddOpenIdConnect("entra")` only when enabled, calls `HandleResponse()` before issuing the session cookie, gates bootstrap/sync-hit on TrustedTenants, and preserves lock #10 (passkey Primary, M365 Outline). Link 409 bodies omit foreign PrincipalIds; obsolete `UseEntra` only flips `Entra:Enabled`. Dominant risk: raw OIDC against the template’s default `TenantId=organizations` has no AAD multi-tenant issuer validator, so live callbacks fail before `OnTicketReceived` — and the FakeEntraHandler path cannot catch that.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/features/identity/entra-authentication-registration-server.cs:35
- Description: Authority is built as `{instance}/{tenant}/v2.0` with template default `Authentication:Entra:TenantId` = `organizations` (`appsettings.json`). Entra’s organizations discovery document advertises `"issuer":"https://login.microsoftonline.com/{tenantid}/v2.0"` (literal placeholder). `AddOpenIdConnect` leaves `TokenValidationParameters.ValidateIssuer` at its default (`true`) and never sets `IssuerValidator` / `AadIssuerValidator`. A real id_token carries a concrete `iss` (`…/{tid}/v2.0`), so middleware issuer validation fails and `OnTicketReceived` (including application-level `IssuerMatchesTenant`) never runs. This is the known reason Microsoft.Identity.Web exists for multi-tenant AAD; choosing raw OIDC without replacing that validator breaks the documented `organizations` + `TrustedTenants` configuration. FakeEntraHandler / `EntraTicketHttp.HandleTicketAsync` skip OIDC validation, so integration tests hide the failure.
- Suggestion: When `TenantId` is `organizations`/`common`/`consumers` (or otherwise multi-tenant), install an AAD-aware issuer validator (e.g. `Microsoft.IdentityModel.Validators.AadIssuerValidator`, or an equivalent that substitutes `{tenantid}` from the token’s `tid` and still rejects non-login.microsoftonline.com issuers). Keep the existing `IssuerMatchesTenant` pin as defense-in-depth. Add a registration/unit assertion that multi-tenant authorities configure `IssuerValidator`, or a test that feeds a configuration manager with the placeholder issuer and proves a concrete-tenant token is accepted only when `tid` ∈ TrustedTenants.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/features/identity/entra-ticket-processor-application.cs:179
- Description: Bootstrap does `Principal.Create` + `AddPrincipalAsync` then `AddCredentialAsync`. Under concurrent first-login for the same `tid:oid`, both finds miss, both create principals, and the loser hits the unique `(Type, Handle)` constraint (`InvalidOperationException` in both in-memory and EF stores). The catch returns 409 `Credential already registered` and leaves an orphan Human principal with no credential. Retry can sync-hit the winner, so it is recoverable, but the losing request fails after a successful Entra login and leaks a useless principal row.
- Suggestion: On `AddCredentialAsync` failure during bootstrap, re-`FindCredentialByHandleAsync`; if an active credential now exists, treat as sync-hit for that PrincipalId (and abandon/delete the just-created orphan). Alternatively create the credential in the same unit of work / only persist the principal after the credential insert succeeds.
- Status: open
