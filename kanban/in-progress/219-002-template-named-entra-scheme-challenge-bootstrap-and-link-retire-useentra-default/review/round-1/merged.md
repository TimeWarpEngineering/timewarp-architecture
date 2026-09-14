# Round 1 — merged findings
**Date:** 2026-09-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/identity/entra-authentication-registration-server.cs:35
- Description: Authority is built as `{instance}/{tenant}/v2.0` with template default `Authentication:Entra:TenantId` = `organizations`. Entra’s organizations discovery document advertises `"issuer":"https://login.microsoftonline.com/{tenantid}/v2.0"` (literal placeholder). `AddOpenIdConnect` leaves `TokenValidationParameters.ValidateIssuer` at its default (`true`) and never sets `IssuerValidator`. A real id_token carries a concrete `iss` (`…/{tid}/v2.0`), so middleware issuer validation fails and `OnTicketReceived` never runs. RFC 219 D10 / config sketch documents `organizations` + `TrustedTenants`. FakeEntraHandler skips OIDC validation, so integration tests hide the failure.
- Suggestion: Install an Entra v2 issuer validator that accepts `iss` == `EntraIssuerMaterial.FromTenantId(tid)` (same pin as `EntraTicketProcessor.IssuerMatchesTenant`). Handles `organizations`/`common`/`consumers` and single-tenant GUID authorities. Keep the application-level pin as defense-in-depth. Assert on `OpenIdConnectOptions.TokenValidationParameters.IssuerValidator` in scheme-registration tests; unit-test accept/reject of the validator.
- Source: general
- Disposition notes: `EntraIssuerValidator.Validate` wired as `TokenValidationParameters.IssuerValidator` (always). Accepts `iss == EntraIssuerMaterial.FromTenantId(tid)` from JsonWebToken tid or JwtSecurityToken claims. Tests: `Organizations_Tenant_Should_Install_Entra_Issuer_Validator` + `entra-issuer-validator-tests.cs`.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/identity/entra-ticket-processor-application.cs:179
- Description: Bootstrap does `Principal.Create` + `AddPrincipalAsync` then `AddCredentialAsync`. Concurrent first-login for the same `tid:oid` can miss both finds, create two principals, and the loser hits unique `(Type, Handle)` (`InvalidOperationException`). The catch returns 409 and leaves an orphan Human principal. Retry can sync-hit the winner, but the losing request fails after a successful Entra login.
- Suggestion: On `AddCredentialAsync` failure during bootstrap, re-`FindCredentialByHandleAsync`; if an active credential now exists, treat as sync-hit for that PrincipalId. `IPrincipalStore` has no delete-principal port — do not invent one; document that the losing principal row is abandoned. Cover with a processor test using a fake store that throws on first credential insert then returns the winner on re-find.
- Source: general
- Disposition notes: Bootstrap catch re-Finds; active winner is sync-hit (missing/inactive still refused). Losing principal row abandoned (no delete port). Test: `EntraTicketProcessor_.Bootstrap_Given_.AddCredential_Race_Should_Sync_Hit_Winner_Principal`.

## Duplicates / conflicts

- Single reviewer (`general`); no findings to collapse.
