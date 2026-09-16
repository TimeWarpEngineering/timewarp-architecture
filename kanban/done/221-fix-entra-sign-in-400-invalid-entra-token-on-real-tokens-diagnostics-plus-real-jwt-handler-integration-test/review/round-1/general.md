# Round 1 — general
**Date:** 2026-09-16
**Scope reviewed:** branch task/221-fix-entra-sign-in-400-invalid-entra-token-on-real vs origin/master (product + tests)

## Summary

The change correctly keeps `iss` on the named `entra` OIDC principal (`ClaimActions.Remove("iss")` plus an `OnTokenValidated` copy from `SecurityToken.Issuer`), stashes the local return path on `AuthenticationProperties.Items`, and ships the required TryRead / Warning / named-400 / boot-version diagnostics plus a real-JWT `OpenIdConnectHandler` round-trip test. Risk is low: the fix is scheme-scoped, constraints (`identity-session` DefaultScheme, `MapInboundClaims=false`, no forwarded headers / RP-ID) are preserved, and call sites for the new `TryRead` / `InvalidEntraToken` / `EntraTicketProcessor` logger shapes are consistent. Re-checked against ASP.NET Core 10.0.11: default `DeleteClaim("iss")` runs after `OnTokenValidated` on empty JSON when user-info is off, and `RemoteAuthenticationHandler` nulls `Properties.RedirectUri` before `OnTicketReceived` (so the Items stash is required).

## Issues
