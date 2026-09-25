# Round 1 — merged findings
**Date:** 2026-09-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: wontfix (superseded — fixed in round 2)
- File: source/container-apps/web/features/identity/add-passkey/add-passkey-handler-application.cs:82
- Description: AddPasskey accepts a challenge started for a new account (pending id present), so the
  added passkey can carry a phantom account's fingerprint in its stored name.
- Suggestion: refuse such challenges with ChallengeInvalid; update the shared test helper.
- Source: general
- Disposition notes: wontfix (orchestrator). Cosmetic only — the attach target is always the
  authenticated caller, so no security or account-binding impact; the shipped SPA always sends
  ForCurrentAccount=true. Refusing would hard-fail AddPasskey for any SPA bundle cached from before
  this deploy (it sends the default Start) until the browser reloads — a worse outcome than a
  mislabeled passkey name. Revisit if AddPasskey ever needs to trust the name.

## Duplicates / conflicts

- None (single reviewer).
