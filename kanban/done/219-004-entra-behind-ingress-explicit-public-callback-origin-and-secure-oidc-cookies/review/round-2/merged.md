# Round 2 — merged findings
**Date:** 2026-09-15
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/wwwroot/auth.md:70
- Description: Unset PublicOrigin was documented as also writing OIDC cookies without Secure. After the scheme pin, that compound claim is false.
- Suggestion: Split http `redirect_uri` from Secure cookies.
- Source: general (round 1); re-verified round 2
- Disposition notes: `auth.md` now documents the http `redirect_uri` failure when PublicOrigin is unset, then separately that the named `entra` scheme always writes Secure correlation/nonce cookies.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/identity/entra-authentication-registration-server.cs:15
- Description: Design implied the handler would emit SameSite=None cookies without Secure; net10 defaults those cookies to SecurePolicy.Always.
- Suggestion: Pin wording without implying the current framework omits Secure.
- Source: general (round 1); re-verified round 2
- Disposition notes: Design now states the http `redirect_uri` problem and pins SecurePolicy.Always so SameSite=None cookies stay Secure behind http without relying on framework defaults. Explicit Always assignments unchanged.

## Duplicates / conflicts

- Single reviewer (`general`); no new findings. Prior M1/M2 carried with updated status.
