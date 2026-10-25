# Round 1 — general
**Date:** 2026-10-26
**Scope reviewed:** branch vs merge-base `8b1230367` (all 62 product files; migration Designer skimmed)

## Summary

The change adds a per-user xAI key (row `user.{id}.xai.api-key`, per-user Data Protection
purpose), a Required / Allow / Off site switch, a resolver that puts the host key first, a
probe before save, and deletion on principal merge. Storage, contracts, Profile UI, and the
resolver ladder match the decisions. Two defects are in the singleton upstream: the mode that
picks the "which key" sentence never reaches the caller, and the one-slot client cache now
churns and leaks under per-user keys. The merge-delete path adds a silent catch-all.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/features/agent-chat/xai-chat-upstream-server.cs:234
- Description: `LastMode` is an `AsyncLocal<XaiByokMode>` set inside the async method
  `ResolveApiKeyAsync`. An AsyncLocal value set inside an async method does not flow back to
  the caller (the file's own Design region says this about `OpenTurn`). `CompleteAgentChat`
  (`Upstream.MissingKeyDetail` after `IsConfiguredAsync`), `GetAgentChatConfiguration`
  (`Upstream.ResolvedMode` / `MissingKeyDetail`), and `CompleteAsync` line 155 all read the
  default, which is `Allow`. In production, Required still shows "…or ask an admin to set the
  site key." plus the Admin → AI hint, and Off tells the user to set a Profile key while
  Profile hides that section. Checklist item "Ask says which one to set … depending on the
  policy" is not met. Verified with a runfile: set inside an awaited async method, read after
  the await in the caller, prints `0`. The handler tests pass only because they use stubs
  that return the mode directly. The integration test only asserts the detail omits the key.
- Suggestion: Carry the mode on the caller-opened slot (`KeySlot.Mode`, like `KeySlot.Key`),
  or return it from `IsConfiguredAsync`, or let the handlers read the mode from
  `IXaiApiKeyResolver` / the settings store. Add an integration test on the real upstream:
  Required with no user key → 503 detail is `XaiByokCopy.SetYourKey` and configuration
  `AdminHint` is null; Off → detail starts with `XaiByokCopy.AskAdmin`.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/features/agent-chat/xai-chat-upstream-server.cs:283
- Description: The singleton keeps one cached `IChatClient` (`RealClient` / `CachedKey`). Each
  key change moves the old client into `RetiredClients`, which is disposed only when the
  upstream is disposed. Before BYOK the key changed only when an admin rotated it. Now two
  users with different keys taking turns change the key on every turn: each turn builds a new
  OpenAI client and adds one to `RetiredClients`. The list grows without bound for the life of
  the process. The integration test shows this: four turns produce four `Create` calls. The
  Results text "The chat client is cached per key" is not what the code does.
- Suggestion: Cache per key with a bound and eviction (for example a small LRU or
  `MemoryCache` keyed by a hash of the key, disposing evicted clients after in-flight calls
  finish), or build a per-turn client for user-origin keys and dispose it when the turn ends.
  Add a test where alternating keys do not grow the retired list without bound.
- Status: open

### Issue 3 — Severity: bug
- File: source/container-apps/web/features/identity/complete-add-existing-passkey/complete-add-existing-passkey-handler-application.cs:168
- Description: `DeleteUserKeyAsync` catches every exception, discards it with
  `_ = exception;`, and silences CA1031 with `[SuppressMessage]`. That is an empty catch with
  an analyzer suppression, and nothing is logged. The same code is in
  `entra-ticket-processor-application.cs:492`. It is also redundant:
  `EfPrincipalStore.MergePrincipalAsync` already removes the row in the merge's
  `SaveChanges`, and `UserXaiApiKey.DeleteForPrincipalAsync` already catches and logs storage
  failures. Rule silenced: CA1031.
- Suggestion: Remove the post-merge call and the suppressed catch from both callers, and rely
  on the transactional delete in the merge (keep `DeleteForPrincipalAsync` for a future
  delete-user path). If a second delete is kept, let `DeleteForPrincipalAsync` own its error
  handling and do not add a second catch-all.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/features/identity/ef-principal-store-infrastructure.cs:381
- Description: The merge builds the row name by hand (`"user." + id + ".xai.api-key"`), a
  copy of `UserXaiApiKeyNames.NameFor`. The comment says the project cannot reference
  web-contracts. AGENTS.md: when two things must agree, generate one from the other or add a
  build check. If the name changes in one place, merge stops deleting the key and no test
  fails.
- Suggestion: Put the name builder in a type both projects reference (for example beside
  `ProtectedSecret` in the secrets platform namespace) and have `UserXaiApiKeyNames.NameFor`
  call it, or add a test that asserts the two produce the same string.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/container-apps/web/features/agent-chat/xai-api-key-resolver-application.cs:119
- Description: When reading `SiteSettings` throws, `ReadModeAsync` returns `Allow` and logs
  nothing. Under Required, a failed settings read lets Ask fall back to the site key and
  bill the site, which the Required decision forbids ("user key only, no fallback").
  `UserXaiApiKey.ReadModeAsync` at least logs the failure type.
- Suggestion: Log the failure. When the mode cannot be read, do not use the site key: skip
  the site tier, or return no key with a storage problem. Add a test for a settings store
  that throws.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/features/agent-chat/user-xai-api-key-application.cs:9
- Description: The Design region says "status does not unprotect just to render", but
  `GetStatusAsync` calls `ReadAsync` (line 125), which unprotects the row on every status
  read so it can tell `configured` from `needsReplacement`.
- Suggestion: Reword the Design sentence. For example: the suffix comes from the store, and
  status unprotects only to tell a readable row from an unreadable one.
- Status: open
