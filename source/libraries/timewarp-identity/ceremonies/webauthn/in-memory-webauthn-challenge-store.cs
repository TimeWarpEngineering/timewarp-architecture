#region Purpose
// Thread-safe in-memory IWebAuthnChallengeStore for unit tests and single-instance hosts.
#endregion

#region Design
// Single-instance semantics only: challenges live in process memory, so a multi-instance/scaled-out
// host would let a request land on an instance that never issued the challenge it is being asked to
// consume. A distributed store (e.g. Redis-backed) is explicit future work, not attempted here.
// Keyed by the challenge's own base64url encoding — the 32 random bytes are already
// collision-resistant, so no separate id/GUID is minted. TryConsume removes the entry via
// ConcurrentDictionary.TryRemove BEFORE checking ceremony type/expiry: removal is what makes
// consumption one-time and atomic under concurrent callers; the type/expiry checks after removal
// only decide whether the (now-gone-either-way) challenge counts as a successful consume.
// Issue prunes expired entries first (bounds unbounded growth from abandoned ceremonies under
// normal operation) and then evicts the single oldest-by-expiry entry if still at MaxEntries — a
// cheap DoS bound, not a substitute for real rate limiting (task 104-015).
#endregion

namespace TimeWarp.Identity;

using System.Collections.Concurrent;

public sealed class InMemoryWebAuthnChallengeStore : IWebAuthnChallengeStore
{
  private readonly ConcurrentDictionary<string, Entry> Challenges = new(StringComparer.Ordinal);
  private readonly TimeProvider TimeProvider;
  private readonly TimeSpan TimeToLive;
  private readonly int MaxEntries;

  public InMemoryWebAuthnChallengeStore(TimeProvider? timeProvider = null, TimeSpan? timeToLive = null, int maxEntries = 10_000)
  {
    ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxEntries, 0);

    TimeProvider = timeProvider ?? TimeProvider.System;
    TimeToLive = timeToLive ?? TimeSpan.FromMinutes(5);
    MaxEntries = maxEntries;
  }

  public byte[] Issue(WebAuthnCeremonyType ceremonyType)
  {
    PruneExpired();

    byte[] challenge = RandomNumberGenerator.GetBytes(32);
    string key = Base64Url.EncodeToString(challenge);
    DateTimeOffset expiresAt = TimeProvider.GetUtcNow() + TimeToLive;

    if (Challenges.Count >= MaxEntries)
    {
      EvictOldest();
    }

    Challenges[key] = new Entry(ceremonyType, expiresAt);
    return challenge;
  }

  public bool TryConsume(WebAuthnCeremonyType ceremonyType, byte[] challenge)
  {
    ArgumentNullException.ThrowIfNull(challenge);

    string key = Base64Url.EncodeToString(challenge);
    if (!Challenges.TryRemove(key, out Entry entry)) return false;
    if (entry.CeremonyType != ceremonyType) return false;
    return entry.ExpiresAt > TimeProvider.GetUtcNow();
  }

  private void PruneExpired()
  {
    DateTimeOffset now = TimeProvider.GetUtcNow();
    foreach (KeyValuePair<string, Entry> pair in Challenges)
    {
      if (pair.Value.ExpiresAt <= now)
      {
        Challenges.TryRemove(pair.Key, out _);
      }
    }
  }

  private void EvictOldest()
  {
    string? oldestKey = null;
    DateTimeOffset oldestExpiry = DateTimeOffset.MaxValue;

    foreach (KeyValuePair<string, Entry> pair in Challenges)
    {
      if (pair.Value.ExpiresAt < oldestExpiry)
      {
        oldestExpiry = pair.Value.ExpiresAt;
        oldestKey = pair.Key;
      }
    }

    if (oldestKey is not null)
    {
      Challenges.TryRemove(oldestKey, out _);
    }
  }

  private readonly record struct Entry(WebAuthnCeremonyType CeremonyType, DateTimeOffset ExpiresAt);
}
