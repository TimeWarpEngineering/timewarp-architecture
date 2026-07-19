#region Purpose
// Thread-safe in-memory IAgentTokenStore for unit tests and single-instance hosts.
#endregion

#region Design
// At-rest hashing: the dictionary is keyed by SHA-256(token) hex, never the raw bearer value — a
// memory dump or debugger inspection of this store never discloses a presentable token, mirroring
// how passwords/credential handles are never stored raw elsewhere in this library. Issue mints 32
// CSPRNG bytes (matches the challenge stores' entropy) and returns the base64url encoding of the RAW
// bytes to the caller — only the hash of that string is what gets stored.
// Unlike the challenge stores, Validate does NOT consume/remove the entry on success — a token is
// meant to authenticate every request for its whole lifetime, not be one-time. It DOES opportunistically
// remove an entry found to be expired (cheap incidental cleanup, not load-bearing: Issue's
// PruneExpired sweep is the actual bound on unbounded growth from abandoned/expired tokens).
// Single-instance semantics only (see IAgentTokenStore's Design region for the multi-instance
// revisit trigger) — same posture as the challenge stores, not attempted here.
// Prune-on-Issue + evict-oldest-by-expiry-at-cap mirrors InMemoryChallengeStoreCore exactly (not
// reusing that generic core: a token entry's shape — PrincipalId + Scopes, no ceremony type — differs
// enough, and Validate's non-consuming semantics differ enough from TryConsume, that sharing would
// need a second generic parameter/behavior flag for no real duplication savings at this size).
#endregion

namespace TimeWarp.Identity;

using System.Collections.Concurrent;

public sealed class InMemoryAgentTokenStore : IAgentTokenStore
{
  private readonly ConcurrentDictionary<string, Entry> Tokens = new(StringComparer.Ordinal);
  private readonly TimeProvider TimeProvider;
  private readonly int MaxEntries;

  public InMemoryAgentTokenStore(TimeProvider? timeProvider = null, int maxEntries = 100_000)
  {
    ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxEntries, 0);

    TimeProvider = timeProvider ?? TimeProvider.System;
    MaxEntries = maxEntries;
  }

  public string Issue(PrincipalId principalId, IReadOnlyCollection<string> scopes, TimeSpan lifetime)
  {
    ArgumentNullException.ThrowIfNull(scopes);

    PruneExpired();

    byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);
    string token = Base64Url.EncodeToString(tokenBytes);
    string key = HashToken(token);
    DateTimeOffset expiresAt = TimeProvider.GetUtcNow() + lifetime;

    if (Tokens.Count >= MaxEntries)
    {
      EvictOldest();
    }

    Tokens[key] = new Entry(principalId, [.. scopes], expiresAt);
    return token;
  }

  public AgentTokenGrant? Validate(string token)
  {
    if (string.IsNullOrEmpty(token))
    {
      return null;
    }

    string key = HashToken(token);
    if (!Tokens.TryGetValue(key, out Entry entry))
    {
      return null;
    }

    if (entry.ExpiresAt <= TimeProvider.GetUtcNow())
    {
      Tokens.TryRemove(key, out _);
      return null;
    }

    return new AgentTokenGrant(entry.PrincipalId, entry.Scopes, entry.ExpiresAt);
  }

  private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

  private void PruneExpired()
  {
    DateTimeOffset now = TimeProvider.GetUtcNow();
    foreach (KeyValuePair<string, Entry> pair in Tokens)
    {
      if (pair.Value.ExpiresAt <= now)
      {
        Tokens.TryRemove(pair.Key, out _);
      }
    }
  }

  private void EvictOldest()
  {
    string? oldestKey = null;
    DateTimeOffset oldestExpiry = DateTimeOffset.MaxValue;

    foreach (KeyValuePair<string, Entry> pair in Tokens)
    {
      if (pair.Value.ExpiresAt < oldestExpiry)
      {
        oldestExpiry = pair.Value.ExpiresAt;
        oldestKey = pair.Key;
      }
    }

    if (oldestKey is not null)
    {
      Tokens.TryRemove(oldestKey, out _);
    }
  }

  private readonly record struct Entry(PrincipalId PrincipalId, IReadOnlyList<string> Scopes, DateTimeOffset ExpiresAt);
}
