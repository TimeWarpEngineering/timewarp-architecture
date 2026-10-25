# Round 2 — merged findings
**Date:** 2026-10-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/agent-chat/xai-chat-upstream-server.cs:240
- Description: The BYOK mode was set in an `AsyncLocal` after an await and never reached the caller.
- Source: general (round 1)
- Disposition notes: Fixed in `fa3e2acfe`. The mode is now a field on the caller-opened `KeySlot`.
  `GetAgentChatConfiguration` opens the slot. Real-upstream tests cover Required and Off.

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/agent-chat/xai-chat-upstream-server.cs:304
- Description: Per-user keys rebuilt the one cached client on every change, and the retired list
  grew without bound.
- Source: general (round 1)
- Disposition notes: Fixed in `fa3e2acfe`. The cache holds up to 8 clients, one per key. Each has
  a use count, and an evicted client is disposed after its last turn. Tests cover 2 alternating
  keys and 12 distinct keys.

### M3 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/identity/complete-add-existing-passkey/complete-add-existing-passkey-handler-application.cs (also entra-ticket-processor-application.cs)
- Description: An empty catch with a CA1031 suppression around a redundant post-merge delete.
- Source: general (round 1)
- Disposition notes: Fixed in `fa3e2acfe`. The call, the catch, and the suppression are removed.
  The EF merge transaction is the delete.

### M4 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/identity/ef-principal-store-infrastructure.cs:381
- Description: The row name was a hand copy of `UserXaiApiKeyNames.NameFor`.
- Source: general (round 1)
- Disposition notes: Fixed in `fa3e2acfe`. The store calls `NameFor`.

### M5 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/agent-chat/xai-api-key-resolver-application.cs:134
- Description: A settings read that threw fell back to Allow without a log, which billed the site
  key under Required.
- Source: general (round 1)
- Disposition notes: Fixed in `fa3e2acfe`. The resolver logs the exception type and returns
  Required. A test covers it.

### M6 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/agent-chat/user-xai-api-key-application.cs:8
- Description: The Design region said status does not unprotect.
- Source: general (round 1)
- Disposition notes: Fixed in `fa3e2acfe`. The wording now matches the code.

## Duplicates / conflicts

- None. Round 2 found no new issues in the fix delta.
