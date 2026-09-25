# Round 2 — merged findings
**Date:** 2026-09-25
**Sources:** fix loop (cockpit reversal of round-1 M1 wontfix)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/identity/add-passkey/add-passkey-handler-application.cs
- Description: AddPasskey accepted a challenge started for a new account (pending id present), so the
  added passkey could carry a phantom account's fingerprint in its stored name.
- Resolution: AddPasskey now refuses a challenge with a PendingPrincipalId with the uniform 400
  ChallengeInvalid (same problem Complete returns for the mirror case), after the ceremony consumes
  the challenge and before any credential is written. The round-1 wontfix rationale (cached
  pre-deploy SPA bundles) does not apply: this is a template with no deployed pre-change clients
  (Steve, 2026-09-25). Design regions reconciled (AddPasskey contract + handler, registration
  ceremony). Test helper `CredentialCeremonyHelpers.BuildPasskeyAttestationAsync` gained a
  `sessionCookie` parameter that starts ForCurrentAccount; every AddPasskey call site passes it.
  New test: `PasskeyAccountName_.Returns_.BadRequest_And_No_Credential_Given_New_Account_Challenge_Used_For_AddPasskey`.

## Duplicates / conflicts

- None.
