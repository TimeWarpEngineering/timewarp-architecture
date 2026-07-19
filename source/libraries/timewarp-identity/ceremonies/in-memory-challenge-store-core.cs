#region Purpose
// Shared thread-safe in-memory one-time-challenge storage body, generic over the ceremony-type enum
// so WebAuthn and agent-key challenge stores don't hand-duplicate the same prune/evict/consume logic.
#endregion

#region Design
// Extracted from InMemoryWebAuthnChallengeStore (task 104-003) when 104-004 needed the identical
// behavior for agent-key ceremonies — behavior-preserving: InMemoryWebAuthnChallengeStore's public
// surface (ctor signature, Issue/TryConsume) is unchanged, it now delegates to an instance of this
// core rather than implementing the logic itself, and the existing WebAuthn challenge-store tests
// re-run unmodified as the regression pin for that claim.
// `where TCeremonyType : struct, Enum` (not a specific enum type) is what makes this shareable:
// WebAuthnCeremonyType and AgentKeyCeremonyType are unrelated enums, each already following the
// same reserved-zero-None convention, so the core only needs "some enum" to key its Entry record by
// — it never inspects or special-cases the enum's actual members.
// Internal, not public: this is an implementation-sharing seam between ceremony stores living in the
// SAME assembly, not a port itself — IWebAuthnChallengeStore/IAgentKeyChallengeStore remain the
// public contracts consumers depend on, and the delegation is invisible to them.
// Same single-instance semantics, same key-is-the-challenge's-own-base64url-encoding, same
// remove-before-check TryConsume ordering, same prune-on-Issue + evict-oldest-at-cap posture as the
// original WebAuthn-only implementation — see that type's original Design region (preserved on the
// WebAuthn wrapper) for the full rationale; not re-derived here to avoid two sources of truth.
#endregion

namespace TimeWarp.Identity;

using System.Collections.Concurrent;

internal sealed class InMemoryChallengeStoreCore<TCeremonyType>
  where TCeremonyType : struct, Enum
{
  private readonly ConcurrentDictionary<string, Entry> Challenges = new(StringComparer.Ordinal);
  private readonly TimeProvider TimeProvider;
  private readonly TimeSpan TimeToLive;
  private readonly int MaxEntries;

  public InMemoryChallengeStoreCore(TimeProvider? timeProvider, TimeSpan? timeToLive, int maxEntries)
  {
    ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxEntries, 0);

    TimeProvider = timeProvider ?? TimeProvider.System;
    TimeToLive = timeToLive ?? TimeSpan.FromMinutes(5);
    MaxEntries = maxEntries;
  }

  public byte[] Issue(TCeremonyType ceremonyType)
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

  public bool TryConsume(TCeremonyType ceremonyType, byte[] challenge)
  {
    ArgumentNullException.ThrowIfNull(challenge);

    string key = Base64Url.EncodeToString(challenge);
    if (!Challenges.TryRemove(key, out Entry entry)) return false;
    if (!entry.CeremonyType.Equals(ceremonyType)) return false;
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

  private readonly record struct Entry(TCeremonyType CeremonyType, DateTimeOffset ExpiresAt);
}
