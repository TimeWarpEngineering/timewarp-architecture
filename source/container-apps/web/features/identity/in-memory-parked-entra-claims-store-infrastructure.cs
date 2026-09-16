#region Purpose
// In-memory IParkedEntraClaimsStore: 10-minute TTL, single-use consume, opaque id.
#endregion

#region Design
// ConcurrentDictionary keyed by a 32-byte random id (base64url). TryConsume removes first
// then checks expiry so a racy second consume never sees the payload. TryGet peeks without
// removing. Cap + prune-on-Park matches the WebAuthn challenge store.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Infrastructure;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using TimeWarp.Architecture.Features.Identity.Application;

public sealed class InMemoryParkedEntraClaimsStore : IParkedEntraClaimsStore
{
  private const int MaxEntries = 10_000;
  private static readonly TimeSpan TimeToLive = TimeSpan.FromMinutes(10);

  private readonly ConcurrentDictionary<string, Entry> Entries = new(StringComparer.Ordinal);
  private readonly TimeProvider TimeProvider;

  public InMemoryParkedEntraClaimsStore(TimeProvider? timeProvider = null)
  {
    TimeProvider = timeProvider ?? TimeProvider.System;
  }

  public string Park(ParkedEntraClaims claims)
  {
    ArgumentNullException.ThrowIfNull(claims);
    PruneExpired();
    if (Entries.Count >= MaxEntries)
    {
      EvictOldest();
    }

    string id = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    Entries[id] = new Entry(claims, TimeProvider.GetUtcNow() + TimeToLive);
    return id;
  }

  public bool TryGet(string id, out ParkedEntraClaims? claims)
  {
    claims = null;
    if (string.IsNullOrEmpty(id) || !Entries.TryGetValue(id, out Entry entry))
    {
      return false;
    }

    if (entry.ExpiresAt <= TimeProvider.GetUtcNow())
    {
      Entries.TryRemove(id, out _);
      return false;
    }

    claims = entry.Claims;
    return true;
  }

  public bool TryConsume(string id, out ParkedEntraClaims? claims)
  {
    claims = null;
    if (string.IsNullOrEmpty(id) || !Entries.TryRemove(id, out Entry entry))
    {
      return false;
    }

    if (entry.ExpiresAt <= TimeProvider.GetUtcNow())
    {
      return false;
    }

    claims = entry.Claims;
    return true;
  }

  private void PruneExpired()
  {
    DateTimeOffset now = TimeProvider.GetUtcNow();
    foreach (KeyValuePair<string, Entry> pair in Entries)
    {
      if (pair.Value.ExpiresAt <= now)
      {
        Entries.TryRemove(pair.Key, out _);
      }
    }
  }

  private void EvictOldest()
  {
    string? oldestKey = null;
    DateTimeOffset oldestExpiry = DateTimeOffset.MaxValue;
    foreach (KeyValuePair<string, Entry> pair in Entries)
    {
      if (pair.Value.ExpiresAt < oldestExpiry)
      {
        oldestExpiry = pair.Value.ExpiresAt;
        oldestKey = pair.Key;
      }
    }

    if (oldestKey is not null)
    {
      Entries.TryRemove(oldestKey, out _);
    }
  }

  private readonly record struct Entry(ParkedEntraClaims Claims, DateTimeOffset ExpiresAt);
}
