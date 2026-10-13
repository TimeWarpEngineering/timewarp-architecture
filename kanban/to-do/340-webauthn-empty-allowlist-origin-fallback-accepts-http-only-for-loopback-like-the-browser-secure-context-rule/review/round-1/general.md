# Round 1 — general
**Date:** 2026-10-13
**Scope reviewed:** branch vs origin/master (11 files) plus call sites of `WebAuthnOrigin.IsAllowed`
(`webauthn-registration.cs:116`, `webauthn-authentication.cs:82`) and other loopback checks.

## Summary

The fallback now requires host == RP id first, then accepts https, or http only when
`LoopbackHost.IsLoopback(host)`. A non-empty `Origins` list is still exact and ordinal. Both
ceremonies go through the one `WebAuthnOrigin` check, so registration and authentication behave the
same. The Entra `PublicOrigin` validator now uses the shared predicate, which drops its private copy
and also accepts `[::1]`. That is consistent with the browser rule. Tests cover every case the spec
lists, plus RP-id mismatch on loopback and non-web schemes. Risk is low. The http path cannot widen
to non-loopback hosts, because both the host-equality check and the loopback check must pass.
Verified: `web-authn-options-validator` rejects IP-literal RP ids, so in the host only `localhost`
reaches the http path. The IP cases only matter for direct library callers, where they are harmless
and covered by tests.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/features/identity/web-authn-options-application.cs:81
- Description: the edited Design-region sentence runs to 136 columns, which breaks the
  wrapping (~100 cols) the rest of the region uses.
- Suggestion: rewrap the line and split the production sentence onto the next line.
- Status: open
