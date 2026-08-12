#region Purpose
// Persistence port for principals and credentials — hosts supply EF or other backends; tests use the in-memory impl.
#endregion

#region Design
// Folder placement rule (why this store lives in persistence/ while the challenge and token stores
// live beside their features): persistence/ holds the DURABLE domain-data seam — the port a host
// swaps for EF/Postgres, holding what the database will hold. The ceremony challenge stores
// (ceremonies/*/) and the token store (tokens/) hold EPHEMERAL protocol state (TTL'd nonces,
// short-lived grants) that will never be database entities — if distributed, they become
// cache/Redis-backed — so they are co-located with the feature they serve, per the repo's
// feature-cohesion convention. See overview.md at the library root.
//
// Durability inventory (task 104-032):
//   - IPrincipalStore (Principal + Credential, including agent keys) → durable; host EF behind
//     the postgres flag (EfPrincipalStore in web-infrastructure). In-memory remains the no-flag /
//     skip-mode default.
//   - IAgentTokenStore (~15 min bearer grants) → deliberately ephemeral (in-memory); Redis later
//     if multi-replica requires shared token state.
//   - IWebAuthnChallengeStore / IAgentKeyChallengeStore → ephemeral by design (in-memory).
// Credential lookup by CredentialId (RFC D3), not raw Guid. Type and Handle are immutable after Create — UpdateCredential
// persists revoke/label (and similar) changes for the same Id only; no handle reindex contract.
// FindCredentialByHandle may return revoked credentials (callers check IsRevoked).
// Clocks (D5, closed 104-006): TimeProvider is not part of this durable port — entity CreatedAt/
// RevokedAt stay wall-clock; ceremony challenge/token stores (not this port) already accept optional
// TimeProvider. Full domain-entity TimeProvider is not required for the Wave 1 gate.
//
// Concurrency (task 104-028, supersedes D6 last-write-wins and D4's "shared-reference vs
// snapshot-on-get is an implementation choice"). Principal and Credential inherit
// Entity<TId>.Version, a store-owned optimistic-concurrency token. Version == 0 means
// created-but-never-updated; every implementation MUST advance it by exactly 1 per successful
// update (EntityVersion.Next) and MUST return snapshots — this port contract requires
// snapshot-on-get, closing off D4's "implementation choice" framing, because a version check is
// meaningless if Get* can hand back the same shared instance a concurrent writer is mutating.
//   - Add*: persists the given instance's state as-is, including Version (0 for every publicly
//     creatable instance — Create never mints a nonzero Version).
//   - Get*/Find*/List*: return snapshots — fresh, caller-owned instances. Every call returns a new
//     instance; mutating a returned instance changes nothing in the store until Update* is called
//     with it.
//   - Update*: compares the incoming instance's Version against the stored row's Version. Mismatch
//     throws ConcurrencyConflictException (entity type, id, expected = the incoming instance's
//     Version, actual = the stored Version); stored state is left completely untouched. Match
//     persists a new snapshot with Version = EntityVersion.Next(stored Version). The caller's
//     in-hand instance is NOT modified by a successful Update* — immediately after, it is one
//     version stale; a second Update* with the same in-hand instance throws, and a caller that
//     wants to keep updating must re-Get. Unknown id remains InvalidOperationException — absence
//     and staleness are distinct failure classes and must not be conflated.
//   - AddCredentialAsync side effect: the first-credential rule (Provisional → Keyed) mutates the
//     STORED principal — via a new snapshot, never in place — and advances the stored principal's
//     Version when, and only when, the tier actually changes; the caller's in-hand principal
//     instance is untouched either way. A concurrent principal writer holding the pre-attach
//     snapshot conflicts on their next Update* instead of silently overwriting the tier change.
//   - Conflict policy (retry vs reload vs fail the request) stays with callers — that half of the
//     original D6 lean (defer callsite policy) was correct and is unchanged by this task.
//   - Exception delivery is NOT specified to be synchronous: Add*/Update* may throw before
//     returning a Task (the in-memory implementation does — its bodies run to completion
//     synchronously) or may surface the same condition as a faulted Task (an EF-backed
//     implementation will, since the check naturally happens inside an awaited SaveChanges).
//     Callers must not assume faulted-task delivery — do not separate task acquisition from
//     awaiting if you need to catch these exceptions (i.e. always `await store.UpdateXAsync(...)`
//     directly in a try/catch, not `Task t = store.UpdateXAsync(...); ...; await t;`).
#endregion

namespace TimeWarp.Identity;

public interface IPrincipalStore
{
  Task AddPrincipalAsync(Principal principal, CancellationToken cancellationToken = default);
  Task<Principal?> GetPrincipalAsync(PrincipalId id, CancellationToken cancellationToken = default);
  Task UpdatePrincipalAsync(Principal principal, CancellationToken cancellationToken = default);

  /// <summary>
  /// All principals as snapshots, ordered by <see cref="Principal.CreatedAt"/> ascending.
  /// </summary>
  Task<IReadOnlyList<Principal>> ListPrincipalsAsync(CancellationToken cancellationToken = default);

  Task AddCredentialAsync(Credential credential, CancellationToken cancellationToken = default);
  Task<Credential?> GetCredentialAsync(CredentialId credentialId, CancellationToken cancellationToken = default);
  Task<Credential?> FindCredentialByHandleAsync(CredentialType type, byte[] handle, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<Credential>> ListCredentialsAsync(PrincipalId principalId, bool includeRevoked = false, CancellationToken cancellationToken = default);
  Task UpdateCredentialAsync(Credential credential, CancellationToken cancellationToken = default);
}
