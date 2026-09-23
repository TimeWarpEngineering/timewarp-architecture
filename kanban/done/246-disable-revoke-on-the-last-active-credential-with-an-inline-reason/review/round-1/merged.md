# Round 1 — merged findings
**Date:** 2026-09-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

None raised. The general reviewer verified (and the review oracle independently spot-checked):

- Count parity: `CredentialsState.ActiveCredentialCount` counts `IsActive` (= `!IsRevoked`) across the whole `GetCredentials` snapshot, the same set `RevokeCredential.Handler` counts via `ListCredentialsAsync(includeRevoked: false)`.
- Hint is rendered as a visible `<p>` under the button in `CredentialList.razor`, not a tooltip.
- No leftover `Delete*` / `OnDelete` / `DeletePasskey` identifiers in `source/` or `tests/` (the only hit is the intended negative assertion in `protected-page-deep-link-tests.cs`).
- Post-revoke flip is state-driven (Revoke → Fetch) and covered by a dedicated fact.
- New tests are Jaribu + Shouldly, C-create host, consistent with the sibling analytics SPA test host; Purpose/Design regions reconciled on every touched file.

## Duplicates / conflicts

- None (single reviewer).
