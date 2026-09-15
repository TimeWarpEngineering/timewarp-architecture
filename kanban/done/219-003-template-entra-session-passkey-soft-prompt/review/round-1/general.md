# Round 1 — general
**Date:** 2026-09-15
**Scope reviewed:** branch `task/219-003-template-entra-session-passkey-soft-prompt` vs `origin/master` (commit `f62f386c` — `feat(identity): Entra-session passkey soft-prompt after login`). Product files under `source/container-apps/web/projects/web-spa/` and `tests/container-apps/web/web-spa-integration-tests/features/identity/passkey-soft-prompt-tests.cs`.

## Summary

RFC 219 D8 is implemented as a Type-list soft prompt only: `PasskeySoftPrompt.ShouldShow` keys off active `EntraAccount` without active `Passkey`, with null snapshot and dismiss hiding the banner. `TimeWarpPage` (Outside) composes identity-slice `AddPasskeyPrompt`; CTA reuses `CredentialsState.AddPasskey` (StartPasskeyRegistration + WebAuthn + AddPasskey + re-fetch). Home remains Anonymous; Profile/Settings keep permission policies — dismiss is UX/`sessionStorage` only, cleared on sign-out. Risk is low; required unit coverage (8/8) holds shown/hidden/dismissed and non-gate route policies.

## Issues
