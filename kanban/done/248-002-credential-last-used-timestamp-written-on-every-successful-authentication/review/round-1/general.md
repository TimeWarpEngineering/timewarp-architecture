# Round 1 — general
**Date:** 2026-09-23
**Scope reviewed:** branch task/248-002-… vs master

## Summary

The change adds `Credential.LastUsedAt` + monotonic `MarkUsed(now)` to the aggregate, a
`CredentialUsageRecorder` singleton that writes every ceremony call and coalesces per-request
bearer validation to one write per 5-minute window per credential, and threads `CredentialId`
through `AgentTokenGrant`/`IAgentTokenStore` so the bearer validators know which key to stamp.
I re-read the recorder's claim/CAS logic (`TryClaim`, `WriteAsync`, `PruneIfLarge`), the
aggregate's `MarkUsed`, the revoke-vs-mark-used race (both orderings, at the library seam and the
handler seam), both web and api bearer handlers' call sites, the EF mapping/migration/snapshot,
the `AgentTokenGrant.CredentialId` threading through issuance/validation/in-memory store, the SPA
presenter's relative-time boundaries, and the reconciled Design regions on every touched file.
Risk is low: the concurrency logic is lock-free but race-safe (CAS via `ConcurrentDictionary`,
no double-claim, no retry storm), the advisory-drop-vs-revoke rule is correctly implemented and
covered by tests at both seams (revoke-first and stamp-first orderings), both bearer handlers
call the coalesced path, and the migration/snapshot/mapping are consistent. No bugs found.

## Issues

No issues found. Reviewed and confirmed correct by reading the actual code and call sites (not
merely trusting the task's Results section):

- `CredentialUsageRecorder.TryClaim` (source/libraries/timewarp-identity/credentials/credential-usage-recorder.cs:126-149)
  is a correct lock-free CAS loop: `TryAdd` on first claim, `TryUpdate(id, now, last)` compare-and-swap
  thereafter; a losing CAS retries the loop and re-observes the just-written timestamp, so it cannot
  double-claim or double-write within the window. The claimed slot is retained even when the
  subsequent store write is dropped (`WriteAsync` returning false does not roll back the claim),
  matching the "dropped write keeps its claim slot" design and bounding staleness to one interval.
- The advisory race rule (`ConcurrencyConflictException` → dropped, never retried; any other
  exception propagates) is implemented exactly as documented, in both `RecordAsync` and
  `RecordCoalescedAsync` via the shared private `WriteAsync`. Verified against
  `RevokeCredential.Handler`'s retry loop (source/container-apps/web/features/identity/revoke-credential/revoke-credential-handler-application.cs)
  and against both orderings in tests/libraries/timewarp-identity-tests/credential-usage-recorder-tests.cs
  and the handler-seam runfile source/container-apps/web/features/identity/credential-last-used-tests.cs.
- Both bearer handlers call the coalesced path: web's
  source/container-apps/web/features/identity/agent-token-authentication-scheme-server.cs:113-114
  and api's source/container-apps/api/platform/identity-host/agent-token-authentication-handler-server.cs:86-87
  both call `UsageRecorder.RecordCoalescedAsync(PrincipalStore, grant.CredentialId, ...)` after the
  principal-liveness check, both registered via `AddSingleton<CredentialUsageRecorder>()` in their
  respective store modules.
- `AgentTokenGrant.CredentialId` is threaded correctly: `Issue` signature change in
  `IAgentTokenStore`/`InMemoryAgentTokenStore` (source/libraries/timewarp-identity/tokens/*.cs),
  the private `Entry` record struct carries it, `Validate` round-trips it onto `AgentTokenGrant`,
  and `CompleteAgentTokenIssuance.Handler` passes `credential.Id` at issuance. All call sites
  (product and tests) were updated consistently — no stale 3-arg `Issue` overload remains.
- EF mapping (`credential-entity-type-configuration-infrastructure.cs`) declares
  `builder.Property(credential => credential.LastUsedAt)` as a plain nullable column; the migration
  `20260923110932_AddCredentialLastUsedAt.cs` adds a nullable `timestamp with time zone` column and
  the `PostgresDbContextModelSnapshot.cs` reflects the same `DateTimeOffset?` property — consistent
  triad, no drift.
- SPA `RelativeTime` (credential-row-presenter.cs) correctly buckets minute/hour/day/30-day
  boundaries (verified `age < X` vs `age >= X` at each transition) and reads any future instant —
  clock skew of any magnitude — as "just now" by construction (`age < 1 minute` is true whenever
  `age` is negative), which matches its documented behavior and is covered by
  `Relative_Time_Buckets_Singular_Future_And_Absolute_Fallback` in
  tests/container-apps/web/web-spa-integration-tests/features/identity/credential-row-presenter-tests.cs.
  `CreatedText`/absolute fallback use `CultureInfo.InvariantCulture` explicitly, avoiding
  host-locale drift.
- `#region Purpose`/`Design` regions were reconciled on every touched file, not left stale: the
  104-028 "zero Update* calls" notes on both ceremony handlers were explicitly superseded with new
  prose explaining the one new `Update*` call each now makes; `credential.cs`'s Design region gained
  a "Last-used (task 248-002)" paragraph describing monotonicity and the advisory-write ownership
  split with the recorder; the in-memory stores modules' Design regions explain why the recorder is
  registered as a singleton alongside scoped-swappable stores.
- No template-conditional (`#if` / `<!--#if`) regions touch any edited file; the entire
  `postgres/migrations/` tree is excluded from template output at the `.template.config/template.json`
  file-glob level (not via in-file directives), so no directive discipline was needed there.
- Tests use Jaribu + Shouldly exclusively (no Fixie/xUnit/FluentAssertions found in any new/edited
  test file); new test files are kebab-case and sit in the co-located/suite locations the
  conventions specify.
- Test coverage matches every bullet in the task's Tests requirement: ceremony writes (passkey +
  agent-token issuance), per-request coalescing (two calls inside the interval ⇒ one write),
  revoke-vs-mark-used race in both orderings without an exception surfacing, and a `GetCredentials`
  round-trip (used + never-used rows, including the wire-level `"lastUsedAt":"..."` / explicit
  `null` assertions).
