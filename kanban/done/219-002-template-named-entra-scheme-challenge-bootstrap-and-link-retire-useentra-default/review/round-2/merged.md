# Round 2 — merged findings
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
- File: source/container-apps/web/features/identity/entra-authentication-registration-server.cs:52; source/container-apps/web/features/identity/entra-issuer-validator-server.cs
- Description: Multi-tenant `organizations` authority metadata issuer is a `{tenantid}` placeholder; raw `AddOpenIdConnect` default issuer validation rejected concrete `iss` and never reached `OnTicketReceived`.
- Suggestion: `EntraIssuerValidator` pin `iss` to `EntraIssuerMaterial.FromTenantId(tid)`.
- Source: general
- Disposition notes: Re-verified in round 2. `IssuerValidator` wired; `ValidateIssuer` not disabled; JsonWebToken + JwtSecurityToken tid paths; live options test for TenantId=`organizations`. No Microsoft.Identity.Web.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/identity/entra-ticket-processor-application.cs:201-225
- Description: Concurrent bootstrap could 409 after a successful Entra login and leave an orphan principal.
- Suggestion: On `AddCredentialAsync` unique-handle failure, re-Find and sync-hit an active winner.
- Source: general
- Disposition notes: Re-verified in round 2. Fake store exercises miss → add principal → throw → re-find winner. Losing principal row abandoned (no delete-principal port).

## Duplicates / conflicts

- Single reviewer (`general`); prior M1/M2 IDs carried forward. No new findings.
