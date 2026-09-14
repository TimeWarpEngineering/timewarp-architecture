# Round 1 — general
**Date:** 2026-09-14
**Scope reviewed:** branch vs origin/master — TimeWarp.Identity EntraAccount + Restore (219-001)

## Summary

RFC 219 Decision 2 A′ lands cleanly in `TimeWarp.Identity`: `CredentialType.EntraAccount = 3`, canonical `EntraAccountHandle` / `EntraIssuerMaterial` helpers, type-dependent `PublicMaterial` Purpose/Design, and `Credential.Restore()` as the one-shot inverse of `Revoke()`. Store port stays unchanged (Find-by-handle still returns revoked rows; Restore is domain + `UpdateCredentialAsync`). Existing passkey/agent verify hosts already look up by `CredentialType.Passkey` / `AgentKey`, so Entra rows cannot silently enter WebAuthn/AgentKey verify. Risk is low; identity unit tests are 195/0 green.

## Issues
