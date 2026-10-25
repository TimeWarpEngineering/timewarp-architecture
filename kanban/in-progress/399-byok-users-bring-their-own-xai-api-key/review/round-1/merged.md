# Round 1 — merged findings
**Date:** 2026-10-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 3 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/features/agent-chat/xai-chat-upstream-server.cs:234
- Description: `LastMode` (`AsyncLocal<XaiByokMode>`) is set inside async `ResolveApiKeyAsync`
  and does not flow back to callers. `MissingKeyDetail` and `ResolvedMode` always read `Allow`.
  In production, Required still names the site key and shows the Admin → AI hint, and Off tells
  the user to set a Profile key that Off hides. A runfile shows the caller reads `0`. Only
  stub-based tests cover the per-mode text.
- Suggestion: Carry the mode on the caller-opened `KeySlot`, or return it from
  `IsConfiguredAsync`, or have the handlers read it from the resolver or the settings store. Add
  real-upstream tests for the Required and Off text and for `AdminHint` being null under
  Required.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/container-apps/web/features/agent-chat/xai-chat-upstream-server.cs:283
- Description: The singleton caches one chat client. Each key change puts the old client in
  `RetiredClients`, which is disposed only when the upstream is disposed. With per-user keys,
  users taking turns rebuild the client every turn and the list grows without bound. Results
  claims "cached per key", but the cache holds one key.
- Suggestion: A bounded per-key cache with eviction and safe disposal, or a per-turn client for
  user keys that is disposed after the turn. Add a test that alternating keys do not grow the
  retained client set without bound.
- Source: general
- Disposition notes:

### M3 — Severity: bug — Status: open
- File: source/container-apps/web/features/identity/complete-add-existing-passkey/complete-add-existing-passkey-handler-application.cs:168 (also entra-ticket-processor-application.cs:492)
- Description: The post-merge `DeleteUserKeyAsync` catches every exception, discards it
  (`_ = exception;`), suppresses CA1031, and logs nothing. That is an empty catch plus a
  suppression. It is also redundant: the EF merge already deletes the row in its transaction,
  and `DeleteForPrincipalAsync` already catches and logs storage failures. Rule silenced: CA1031.
- Suggestion: Remove the post-merge call and the suppressed catch from both callers, and rely on
  the transactional delete. Do not add a second catch-all.
- Source: general
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: source/container-apps/web/features/identity/ef-principal-store-infrastructure.cs:381
- Description: The row name is copied by hand from `UserXaiApiKeyNames.NameFor`. Nothing
  enforces that the two agree (AGENTS.md: generate one from the other, or add a build check).
- Suggestion: Move the name builder to a type both projects reference, or add a test that
  asserts the two strings are equal.
- Source: general
- Disposition notes:

### M5 — Severity: suggestion — Status: open
- File: source/container-apps/web/features/agent-chat/xai-api-key-resolver-application.cs:119
- Description: When the settings read fails, the resolver returns Allow and logs nothing. Under
  Required, Ask then falls back to the site key and bills the site, which that mode forbids.
- Suggestion: Log the failure. When the mode cannot be read, do not use the site key. Add a test
  with a settings store that throws.
- Source: general
- Disposition notes:

### M6 — Severity: nit — Status: open
- File: source/container-apps/web/features/agent-chat/user-xai-api-key-application.cs:9
- Description: The Design region says status does not unprotect, but `GetStatusAsync` calls
  `ReadAsync`, which unprotects on every status read.
- Suggestion: Reword the Design sentence to match the code.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- One reviewer, so there was nothing to merge. M3 covers both callers (the passkey merge
  handler and `EntraTicketProcessor`).
