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

## Re-verification (review oracle, effort 1, general, 2026-09-25)

- Re-read the fix delta (commit e0d7fa8a vs bf3fc1e6). The refusal comes after the challenge is consumed and before any
  credential write. The shipped SPA add-passkey path sends `ForCurrentAccount = true`
  (`credentials-state.add-passkey.cs`). The sign-up path goes through Complete, not AddPasskey, so it is unaffected.
  The Design regions match the code.
- Tests re-run: `PasskeyAccountName` 7/7, `Credential*` 29/29 (web-server-integration-tests, Release).
- M1 confirmed **fixed**. No new findings. Final open count 0.
