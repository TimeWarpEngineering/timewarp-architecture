#region Purpose
// Writes Credential.LastUsedAt after a successful authentication: every time for a ceremony
// (passkey sign-in, agent-token issuance) and coalesced per credential for per-request bearer validation.
#endregion

#region Design
// Two write shapes (task 248-002). RecordAsync(store, credential) — the caller already holds the
// verified snapshot (the ceremony handlers) — stamps and persists on EVERY call: a sign-in is
// per-ceremony, so one write per ceremony is the correct cost. RecordCoalescedAsync(store,
// credentialId) — the per-request bearer path — first claims a per-credential slot in a
// process-local map and only Gets + writes when the previous write is older than CoalesceInterval
// (DefaultCoalesceInterval = 5 minutes, the ONE place the interval is stated). The hot path stays
// read-mostly: a bearer that hits a hundred endpoints in a minute costs one credential UPDATE, not
// a hundred. Both shapes share the map, so a token that is issued (ceremony write) and used
// immediately writes once, not twice.
//
// Race rule vs RevokeCredential (load-bearing): last-used is ADVISORY. When UpdateCredentialAsync
// throws ConcurrencyConflictException the write is DROPPED (returns false) and NEVER retried — the
// concurrent writer that advanced Version is most likely RevokeCredential.Handler's own
// snapshot + Version retry loop, and a retried last-used write could only ever fight it. Revoke wins
// in both orderings: revoke first → this Update conflicts and is dropped, the row stays revoked with
// its old stamp; mark-used first → revoke's Update conflicts, ITS loop re-Gets (now seeing the fresh
// stamp) and revokes on the next attempt, keeping the stamp. No exception from a lost race ever
// surfaces to the authentication caller. Any OTHER exception (store unavailable, row vanished)
// propagates — hiding infrastructure failure behind "advisory" would mask an outage, and the store
// read that preceded the write would already have failed the request anyway.
//
// Claim semantics: the slot is claimed BEFORE the write and is not released when the write is
// dropped — the dropped instant still counts as "attempted", so the next attempt is one interval
// later. Staleness is bounded by one interval and a lost race cannot produce a retry storm. A
// credential Get that returns null/revoked on the coalesced path also keeps the claim (nothing to
// stamp; the bearer's principal-liveness check is the caller's concern, not this recorder's).
// Clock: TimeProvider (System by default) so tests pin instants; the interval is a ctor parameter
// for tests, hosts take the default. Map growth is one entry per credential used by this process;
// entries older than the interval are swept when the map crosses PruneThreshold. Multi-instance
// hosts coalesce per instance (at most one write per interval per instance) — acceptable, the
// same single-instance posture as InMemoryAgentTokenStore.
// Singleton-safe beside a scoped store: IPrincipalStore is a METHOD parameter, not a ctor
// dependency, so the recorder is a process singleton (it owns the map) while EfPrincipalStore
// stays scoped per request.
#endregion

namespace TimeWarp.Identity;

using System.Collections.Concurrent;

/// <summary>
/// Persists <see cref="Credential.LastUsedAt"/> after successful authentication — always for a
/// ceremony, at most once per <see cref="CoalesceInterval"/> per credential for per-request validation.
/// A write that loses the version race is dropped, never retried.
/// </summary>
public sealed class CredentialUsageRecorder
{
  /// <summary>Per-request (bearer validation) writes are coalesced to at most one per credential per this interval.</summary>
  public static readonly TimeSpan DefaultCoalesceInterval = TimeSpan.FromMinutes(5);

  private const int PruneThreshold = 10_000;

  private readonly ConcurrentDictionary<CredentialId, DateTimeOffset> LastWrites = new();
  private readonly TimeProvider TimeProvider;

  /// <summary>Creates a recorder; tests pass a fake clock and a short interval.</summary>
  public CredentialUsageRecorder(TimeProvider? timeProvider = null, TimeSpan? coalesceInterval = null)
  {
    TimeSpan interval = coalesceInterval ?? DefaultCoalesceInterval;
    ArgumentOutOfRangeException.ThrowIfLessThan(interval, TimeSpan.Zero);

    TimeProvider = timeProvider ?? TimeProvider.System;
    CoalesceInterval = interval;
  }

  /// <summary>Minimum spacing between two coalesced writes for the same credential.</summary>
  public TimeSpan CoalesceInterval { get; }

  /// <summary>
  /// Ceremony write: stamps the caller's verified snapshot and persists it now. Returns false when the
  /// write was dropped (lost the version race, or the stamp did not advance).
  /// </summary>
  public async Task<bool> RecordAsync(IPrincipalStore store, Credential credential, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(store);
    ArgumentNullException.ThrowIfNull(credential);

    DateTimeOffset now = TimeProvider.GetUtcNow();
    LastWrites[credential.Id] = now;
    PruneIfLarge(now);

    return await WriteAsync(store, credential, now, cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Per-request write: no-op (false) when this credential was written inside the interval;
  /// otherwise Gets the row and persists a fresh stamp. A lost version race is dropped (false).
  /// </summary>
  public async Task<bool> RecordCoalescedAsync(IPrincipalStore store, CredentialId credentialId, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(store);

    DateTimeOffset now = TimeProvider.GetUtcNow();
    if (!TryClaim(credentialId, now))
    {
      return false;
    }

    PruneIfLarge(now);

    Credential? credential = await store.GetCredentialAsync(credentialId, cancellationToken).ConfigureAwait(false);
    if (credential?.IsRevoked != false)
    {
      return false;
    }

    return await WriteAsync(store, credential, now, cancellationToken).ConfigureAwait(false);
  }

  private static async Task<bool> WriteAsync(IPrincipalStore store, Credential credential, DateTimeOffset now, CancellationToken cancellationToken)
  {
    if (!credential.MarkUsed(now))
    {
      return false;
    }

    try
    {
      await store.UpdateCredentialAsync(credential, cancellationToken).ConfigureAwait(false);
      return true;
    }
    catch (ConcurrencyConflictException)
    {
      // Lost the version race — most likely to a concurrent revoke. Dropped, never retried (Design region).
      return false;
    }
  }

  private bool TryClaim(CredentialId credentialId, DateTimeOffset now)
  {
    while (true)
    {
      if (!LastWrites.TryGetValue(credentialId, out DateTimeOffset last))
      {
        if (LastWrites.TryAdd(credentialId, now))
        {
          return true;
        }

        continue;
      }

      if (now - last < CoalesceInterval)
      {
        return false;
      }

      if (LastWrites.TryUpdate(credentialId, now, last))
      {
        return true;
      }
    }
  }

  private void PruneIfLarge(DateTimeOffset now)
  {
    if (LastWrites.Count < PruneThreshold)
    {
      return;
    }

    foreach (KeyValuePair<CredentialId, DateTimeOffset> pair in LastWrites)
    {
      if (now - pair.Value >= CoalesceInterval)
      {
        LastWrites.TryRemove(pair);
      }
    }
  }
}
