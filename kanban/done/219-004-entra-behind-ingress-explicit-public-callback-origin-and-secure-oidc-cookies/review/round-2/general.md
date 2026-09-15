# Round 2 — general
**Date:** 2026-09-15
**Scope reviewed:** re-verify M1/M2 after doc/Design fixes; scan fix delta on auth.md and entra-authentication-registration-server.cs Design region

## Summary

M1 and M2 are fixed. `auth.md` now separates the unset-`PublicOrigin` http `redirect_uri` failure from the named `entra` scheme always writing Secure correlation/nonce cookies. The Design region no longer implies the handler would omit Secure on this TFM; it states the http `redirect_uri` problem, then pins `SecurePolicy.Always` so SameSite=None cookies stay Secure behind http without relying on framework defaults. Explicit `Always` assignments are unchanged. The wording-only delta introduces no new defects.

## Issues
