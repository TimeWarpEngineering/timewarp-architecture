# Round 1 — merged findings
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
- Description: The guide says unset `PublicOrigin` would "send Entra `http://…/signin-oidc` and write OIDC correlation cookies without `Secure`." After this change, correlation/nonce cookies are always `SecurePolicy.Always` on the `entra` scheme regardless of `PublicOrigin`. Unset `PublicOrigin` still breaks proxied Entra via an http `redirect_uri`, but it does not leave cookies without `Secure`. The compound claim mis-teaches operators about what remains broken.
- Suggestion: Split the two concerns. Keep “must set `PublicOrigin` for proxied deployments” for the http `redirect_uri`, and state separately that the named scheme forces Secure correlation/nonce cookies (or simply drop the “and write … without Secure” clause).
- Source: general
- Disposition notes: Split the sentence. Unset PublicOrigin still documents the http `redirect_uri` failure; a following sentence states the named scheme always writes Secure correlation/nonce cookies.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/identity/entra-authentication-registration-server.cs:15
- Description: Design says that behind http YARP the handler would emit SameSite=None cookies without Secure. On this repo’s TFM (ASP.NET Core 10 / package 10.0.11), `CorrelationCookie` and `NonceCookie` already default to `CookieSecurePolicy.Always` (SameAsRequest was the net8 default). The explicit `Always` assignments remain a good pin and match the checklist; the motivational wording is just slightly stale for net10.
- Suggestion: Rephrase to pin SecurePolicy.Always without implying the current framework would omit Secure.
- Source: general
- Disposition notes: Design now says the handler would emit an http redirect_uri, then pins SecurePolicy.Always so SameSite=None cookies stay Secure behind http without relying on framework defaults. Explicit assignments unchanged.

## Duplicates / conflicts

- Single reviewer (`general`); no findings to collapse.
- Independently re-verified: `OpenIdConnectOptions` 10.0.11 defaults `CorrelationCookie.SecurePolicy` and `NonceCookie.SecurePolicy` to `Always` with `SameSite=None`. auth.md:70 still claims unset PublicOrigin writes cookies without Secure.
