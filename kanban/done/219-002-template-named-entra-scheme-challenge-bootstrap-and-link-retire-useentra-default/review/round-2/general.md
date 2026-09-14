# Round 2 — general
**Date:** 2026-09-14
**Scope reviewed:** post-fix delta for M1/M2 plus scan for new defects

## Summary

Re-verified the M1/M2 fix delta against product code and tests. `EntraIssuerValidator.Validate` is wired on the named OIDC scheme with `ValidateIssuer` left at its default, JsonWebToken and JwtSecurityToken tid paths both covered, and the organizations scheme-registration test invokes the live options validator. Bootstrap `AddCredentialAsync` race recovery re-Finds and sync-hits an active winner without inventing delete-principal. `dotnet test -c Release -- --filter-class Entra` reported 19 passed / 0 failed. No new defects found in the fix files.

## Prior issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/identity/entra-authentication-registration-server.cs:52; source/container-apps/web/features/identity/entra-issuer-validator-server.cs; tests/container-apps/web/web-server-integration-tests/features/identity/entra-scheme-registration-tests.cs; tests/container-apps/web/web-server-integration-tests/features/identity/entra-issuer-validator-tests.cs
- Re-verification: `AddNamedEntraScheme` assigns `options.TokenValidationParameters.IssuerValidator = EntraIssuerValidator.Validate` and never sets `ValidateIssuer = false`. Validator accepts only `iss == Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(tid))` (ordinal); `TryReadTenantId` covers `JsonWebToken.TryGetPayloadValue("tid", out string?)` and `JwtSecurityToken.Claims`; wrong issuer / whitespace iss / missing tid throw `SecurityTokenInvalidIssuerException`. Unit tests cover both token shapes plus reject paths; `Organizations_Tenant_Should_Install_Entra_Issuer_Validator` builds the live `OpenIdConnectOptions` for TenantId=`organizations` and invokes the installed validator on matching and wrong issuers. web-server references `Microsoft.AspNetCore.Authentication.OpenIdConnect` only — no `Microsoft.Identity.Web` PackageReference added for this fix (CPM pin remains unused by the web project).
- Status: fixed

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/identity/entra-ticket-processor-application.cs:201-225; tests/container-apps/web/web-server-integration-tests/features/identity/entra-ticket-processor-tests.cs
- Re-verification: Bootstrap catch on `InvalidOperationException` re-`FindCredentialByHandleAsync`; active non-revoked winner with present active principal returns that `PrincipalId` (sync-hit); missing principal → `AuthenticationFailed`; inactive → `Quarantined`; missing/revoked winner falls through to `CredentialAlreadyRegistered` (409) — matches Design. No delete-principal path. Fake `RacePrincipalStore` exercises first-find miss → `AddPrincipal` → `AddCredential` throw → re-find winner; asserts `FindCredentialCalls == 2`, `AddPrincipalCalls == 1`, `AddCredentialCalls == 1`, and result is the winner id. `NoOpPrincipalRoleStore` implements all three `IPrincipalRoleStore` members; `RacePrincipalStore` implements the full `IPrincipalStore` surface.
- Status: fixed
