# Credential last-used timestamp written on every successful authentication

## Description

Record when a credential was last used to authenticate and show it in the credential list and
the revoke confirmation. This is the one signal that answers "is this the passkey I actually
use" before revoking. Child of 248; lands after 248-001.

## Requirements

- **Domain.** `Credential` gains `LastUsedAt` (nullable) with a named mutation (`MarkUsed(now)`),
  never a public setter (aggregate pattern, `tw-aggregate-pattern`).
- **Write points.** Set it on every successful assertion / validation for every credential kind:
  passkey authentication (`complete-passkey-authentication` handler), agent-key token issuance
  (`complete-agent-token-issuance`), and — decide and document — agent-token validation on each
  request (`agent-token-authentication-scheme-server.cs`). For the per-request path, do NOT write
  on every call: coalesce (write at most once per N minutes per credential) so the hot path stays
  read-mostly; record the interval in the Design region. Passkey sign-in is per-ceremony and
  writes every time.
- **Concurrency.** RevokeCredential uses snapshot + optimistic `Version` retry (see its Design
  region). `MarkUsed` must not fight it: a lost race on a last-used write is dropped, never
  retried into a revoke (last-used is advisory). State the rule in the Design region and test it
  (concurrent revoke + mark-used ⇒ revoke wins, no exception surfaces to the caller).
- **Persistence.** EF mapping + migration (postgres flag); in-memory store parity.
- **Surface.** `GetCredentials.CredentialSummary.LastUsedAt`; `CredentialList` shows "Last used
  <relative>" or "Never used"; revoke confirmation includes it; the disabled-last-credential hint
  from task 246 is unaffected.
- **Tests.** Co-located Jaribu: passkey sign-in sets LastUsedAt; agent token issuance sets it;
  per-request validation coalesces (two calls inside the interval ⇒ one write); revoke-vs-mark-used
  race; GetCredentials round-trip. Existing identity suites green.
- Gates: `dev build` 0/0, `dev test`, `dev template-smoke`.

## Checklist

- [x] `LastUsedAt` + `MarkUsed` on the aggregate
- [x] Write points: passkey sign-in, agent-token issuance, coalesced per-request validation
- [x] Race rule vs revoke documented and tested
- [x] Mapping + migration + in-memory parity
- [x] Summary field + list + revoke confirmation
- [x] Gates

## Depends on

- 248-001

## Notes

- Parent: 248.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-23)
- Implemented: `ganda task work 248-002` headless implementer (Claude Fable 5.1), 2026-09-23, in the
  claim worktree on branch `task/248-002-credential-last-used-timestamp-written-on-every-su`.

## Results

### Decisions

- **Per-request agent-token validation DOES write, coalesced.** `CredentialUsageRecorder`
  (`timewarp-identity/credentials/credential-usage-recorder.cs`) is a process singleton owning a
  per-credential "last write" map. `RecordCoalescedAsync` claims the slot and writes only when the
  previous write is older than `DefaultCoalesceInterval` = **5 minutes**; every other request in the
  window is a dictionary lookup with no store round-trip. Ceremony paths (`RecordAsync`) write every
  time and share the same window, so issue-then-use costs one write. The interval is stated once (the
  recorder's Design region + constant); both bearer handlers (web and api) call the coalesced path.
- **Race rule: last-used is advisory; revoke wins.** A last-used `UpdateCredentialAsync` that throws
  `ConcurrencyConflictException` is dropped (returns false), never retried. Revoke-first → the stale
  stamp is dropped and the row stays revoked; stamp-first → RevokeCredential's own retry loop
  re-Gets and revokes on attempt two, keeping the stamp. Any other store exception propagates (an
  outage must not hide behind "advisory"). Documented in the recorder's and `Credential`'s Design
  regions; tested at the library seam and at the handler seam (both orderings).
- **`MarkUsed(now)` is monotonic and takes the instant from the caller** so coalescing and tests are
  deterministic; it carries no revoke guard (the authentication ladder rejects revoked credentials
  before any write point runs).
- **`AgentTokenGrant` / `IAgentTokenStore.Issue` now carry the issuing `CredentialId`** — the bearer
  validator has no other way to know which key to stamp. It is not a claim and is not re-verified at
  validation time (revoking a key still does not invalidate already-issued short-lived tokens —
  unchanged posture).

### What landed

- **Domain (`timewarp-identity`)**: `Credential.LastUsedAt` (nullable, private set) + `MarkUsed(now)`;
  private ctor / `Snapshot` copy it, so `InMemoryPrincipalStore` and `EfPrincipalStore` parity is by
  construction. New `CredentialUsageRecorder`. `AgentTokenGrant.CredentialId`; `Issue` signature.
- **Write points**: `CompletePasskeyAuthentication.Handler` and `CompleteAgentTokenIssuance.Handler`
  call `RecordAsync` after Verify + quarantine and before session/token issuance;
  `AgentTokenAuthenticationHandler` (web) and api's parity handler call `RecordCoalescedAsync` after
  the principal-liveness check. Design regions reconciled (the old "zero Update* calls" notes are
  superseded).
- **Infrastructure**: EF mapping `LastUsedAt` (nullable timestamptz); migration
  `20260923110932_AddCredentialLastUsedAt` + snapshot. `CredentialUsageRecorder` registered as a
  singleton in `InMemoryIdentityStoresModule` (web) and `AgentBearerStoresModule` (api); it takes
  `IPrincipalStore` per call, so the postgres scoped swap is unaffected.
- **Surface**: `GetCredentials.CredentialSummary.LastUsedAt` (last ctor param, default null; mock
  factory shows one used and one never-used row). `CredentialRowPresenter.LastUsedText` /
  `RelativeTime` ("just now", "N minutes/hours ago", "yesterday", "N days ago", "on M/d/yyyy" past
  30 days); `CredentialList` renders `data-qa="CredentialLastUsed"` under Created and the revoke
  confirmation reads "…, created X, last used 2 hours ago | never used, fingerprint Z." The task-246
  last-credential hint is untouched.
- **Tests**: identity lib `credential-last-used-tests.cs` (6) and `credential-usage-recorder-tests.cs`
  (9: every-ceremony write, two-inside-interval ⇒ one write, after-interval writes, shared window,
  per-credential windows, lost race dropped without throwing, revoked/unknown no-op, non-race errors
  propagate, defaults); token-store round-trip of `CredentialId`. Co-located runfile
  `source/container-apps/web/features/identity/credential-last-used-tests.cs` (4: revoke-first,
  stamp-first with retry, request path after revoke, GetCredentials round-trip). Integration:
  `PasskeyAuthentication_.Stamps_Credential_LastUsedAt_Given_Valid_Authentication`,
  `AgentToken_.Stamps_Key_LastUsedAt_On_Issuance_And_Coalesces_Bearer_Validation` (two bearer calls
  after issuance ⇒ Version unchanged; `lastUsedAt` on the wire). Contracts round-trip (used + null),
  EF model mapping, SPA presenter (3 new) and list render assertions.

### Gates (run 2026-09-23 in the claim worktree)

- `ganda repo audit` — passes all checks.
- `./bin/dev build` — 0 warnings / 0 errors.
- `./bin/dev test` — every suite green, exit 0 (web-jaribu aggregator 199, web-server-integration 254,
  web-spa-integration 57, web-infrastructure 57, web-contracts 44, timewarp-identity 243, …).
- `./bin/dev template-smoke` — SmokeDefault, SmokeNoPostgres, SmokeNoApi all OK (exit 0); package-mode, package-ID rewrite and tier-1 JARIBU_MULTI guard checks passed; exemplar runfiles 5/5, 5/5, 2/2 standalone.
- `./bin/dev check-version` — source 2.0.0-beta.20 is already ahead of NuGet 2.0.0-beta.19; no bump needed.

### How to validate

**Smoke**

```bash
./bin/dev build
dotnet run source/container-apps/web/features/identity/credential-last-used-tests.cs
cd tests/libraries/timewarp-identity-tests && dotnet test -c Release -- --filter-class CredentialUsageRecorder
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-method Stamps_
```

Then `dev run`, sign in with a passkey, open **Settings**.

**Expect**

- Build 0/0; the runfile reports 4/4; the recorder class reports 9/9; the two `Stamps_*`
  integration facts pass.
- The passkey row you just signed in with shows "Last used just now" (a never-used row shows
  "Never used"); clicking **Revoke** shows a confirmation "… created …, last used just now,
  fingerprint …". `GET /api/identity/credentials` JSON carries `lastUsedAt` (ISO instant or null)
  and still never `handle` / `publicMaterial`.
- Issue an agent token and call any bearer-protected route repeatedly within five minutes: the key
  row's `Version` advances once (at issuance) and not per request.

### Notes for reviewers

- Coalescing is per process (in-memory map), the same single-instance posture as
  `InMemoryAgentTokenStore`; N replicas write at most N times per interval per key.
- A dropped write keeps its claim slot, so a lost race costs one interval of staleness rather than a
  retry storm.
