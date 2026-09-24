# Round 1 — general
**Date:** 2026-09-24
**Scope reviewed:** same as framework; plus call sites of StartPasskeyRegistration.Command (PasskeyCeremonyClient → Login/Passkeys pages; CredentialsState.AddPasskey), PasskeyRegistrationCeremony.TryCompleteAsync (Complete + AddPasskey), IWebAuthnChallengeStore implementations (only InMemoryWebAuthnChallengeStore).

## Summary

The change replaces the fixed "TimeWarp user" WebAuthn name with a per-account name derived from a
new `PrincipalFingerprint`. New-account ids are pre-allocated at Start and kept server-side on the
existing challenge-store entry, returned only through the one-time consume, and Complete mints the
principal with that id (refusing challenges started for the current account). The id never touches
the wire, the name/claim pipeline (session response → SPA claim, hosted prerender derivation →
Settings) is consistent, and Design regions are reconciled. Risk is low; one consistency gap below.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/features/identity/add-passkey/add-passkey-handler-application.cs:82
- Description: AddPasskey ignores `materials.PendingPrincipalId`, so a challenge minted by the
  default (new-account) Start can still be attached to the signed-in caller. The authenticator then
  stores a name carrying a phantom account's fingerprint, breaking "every passkey of an account has
  the same name" for that credential. Complete refuses the mirror case; AddPasskey does not.
- Suggestion: refuse (uniform ChallengeInvalid 400) when `PendingPrincipalId` is non-null, and move the
  shared test helper (`BuildPasskeyAttestationAsync`) to start with ForCurrentAccount + cookie for the
  AddPasskey callers.
- Status: open
