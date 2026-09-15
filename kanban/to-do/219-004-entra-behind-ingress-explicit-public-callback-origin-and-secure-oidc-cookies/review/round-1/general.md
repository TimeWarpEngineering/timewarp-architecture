# Round 1 — general
**Date:** 2026-09-15
**Scope reviewed:** branch `task/219-004-entra-behind-ingress-explicit-public-callback-orig` vs `origin/master` — product commit `d39c0dcb` under `source/container-apps/` and `tests/container-apps/web/web-server-integration-tests/features/identity/`

## Summary

Optional `Authentication:Entra:PublicOrigin` correctly overrides OIDC `redirect_uri` on both challenge (`OnRedirectToIdentityProvider`) and code redemption (`OnAuthorizationCodeReceived`), composed as `{origin}{CallbackPath}` with request-derived behaviour preserved when unset. Correlation/nonce cookies are pinned to `CookieSecurePolicy.Always`; `identity-session` stays DefaultScheme; no `UseForwardedHeaders` and no auto-copy from `Ingress:PublicUrl`. Validator, LocalReturnUrl open-redirect refusal, docs table, and Entra tests (35 passed) match the brief. Residual risk is documentation accuracy around the cookie half of the root cause on net10, not the product wiring.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/wwwroot/auth.md:70
- Description: The guide says unset `PublicOrigin` would "send Entra `http://…/signin-oidc` and write OIDC correlation cookies without `Secure`." After this change, correlation/nonce cookies are always `SecurePolicy.Always` on the `entra` scheme regardless of `PublicOrigin`. Unset `PublicOrigin` still breaks proxied Entra via an http `redirect_uri`, but it does not leave cookies without `Secure`. The compound claim mis-teaches operators about what remains broken.
- Suggestion: Split the two concerns. Keep “must set `PublicOrigin` for proxied deployments” for the http `redirect_uri`, and state separately that the named scheme forces Secure correlation/nonce cookies (or simply drop the “and write … without Secure” clause).
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/features/identity/entra-authentication-registration-server.cs:15
- Description: Design says that behind http YARP the handler would emit SameSite=None cookies without Secure. On this repo’s TFM (ASP.NET Core 10 / package 10.0.11), `CorrelationCookie` and `NonceCookie` already default to `CookieSecurePolicy.Always` (SameAsRequest was the net8 default). The explicit `Always` assignments remain a good pin and match the checklist; the motivational wording is just slightly stale for net10.
- Suggestion: Rephrase to “pin SecurePolicy.Always (net8 defaulted to SameAsRequest; pin anyway so SameSite=None cookies stay Secure behind http)” without implying the current framework would omit Secure.
- Status: open
