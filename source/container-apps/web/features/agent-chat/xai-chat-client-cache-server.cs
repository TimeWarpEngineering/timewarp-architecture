#region Purpose
// Keeps one live xAI chat client per API key and disposes a client only once no completion uses it.
#endregion

#region Design
// Clients are cached per key, up to MaxCachedClients. Rent counts a use and stamps a logical
// clock; past the bound the least recently rented key is evicted. An evicted client with no use
// is disposed at once. One still inside GetResponseAsync moves to IdleEvicted and is disposed by
// the Release that brings its use count to zero. Disposing it earlier would drop that call.
// Release of a client the cache does not hold is a no-op, so the caller may release any client
// it rented. Dispose disposes every live and evicted client outside the lock; a completion still
// in flight may already have finished. Clients come from IXaiChatClientFactory so tests can see
// which key each client was built with. Keys are dictionary keys only and are never logged.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using Microsoft.Extensions.AI;

/// <summary>A bounded per-key cache of xAI chat clients with use counting.</summary>
internal sealed class XaiChatClientCache : IDisposable
{
  /// <summary>Live chat clients kept per key. Older keys are evicted past this count.</summary>
  internal const int MaxCachedClients = 8;

  private readonly IXaiChatClientFactory Clients;
  private readonly Lock Gate = new();
  private readonly Dictionary<string, CachedChatClient> LiveClients = new(StringComparer.Ordinal);
  private readonly List<CachedChatClient> IdleEvicted = [];
  private long ClientClock;

  public XaiChatClientCache(IXaiChatClientFactory clients)
  {
    Clients = clients;
  }

  /// <summary>The client for <paramref name="apiKey"/>, counted as in use until <see cref="Release"/>.</summary>
  public IChatClient Rent(string apiKey, string endpoint, string model)
  {
    lock (Gate)
    {
      ClientClock++;
      if (LiveClients.TryGetValue(apiKey, out CachedChatClient? existing))
      {
        existing.Uses++;
        existing.LastUsed = ClientClock;
        return existing.Client;
      }

      while (LiveClients.Count >= MaxCachedClients)
      {
        CachedChatClient oldest = OldestLive();
        LiveClients.Remove(oldest.Key);
        oldest.Evicted = true;
        if (oldest.Uses == 0)
        {
          oldest.Client.Dispose();
        }
        else
        {
          IdleEvicted.Add(oldest);
        }
      }

      IChatClient created = Clients.Create(apiKey, endpoint, model);
      LiveClients.Add(
        apiKey,
        new CachedChatClient
        {
          Key = apiKey,
          Client = created,
          Uses = 1,
          LastUsed = ClientClock
        });
      return created;
    }
  }

  /// <summary>Ends one use of <paramref name="client"/>. An evicted client is disposed at its last use.</summary>
  public void Release(IChatClient client)
  {
    lock (Gate)
    {
      CachedChatClient? entry = FindCached(client);
      if (entry is null)
      {
        return;
      }

      if (entry.Uses > 0)
      {
        entry.Uses--;
      }

      if (entry.Evicted && entry.Uses == 0)
      {
        IdleEvicted.Remove(entry);
        entry.Client.Dispose();
      }
    }
  }

  /// <summary>Disposes every cached client. An in-flight completion may already have finished.</summary>
  public void Dispose()
  {
    IChatClient[] clients;
    lock (Gate)
    {
      List<IChatClient> pending = [];
      foreach (CachedChatClient entry in LiveClients.Values)
      {
        pending.Add(entry.Client);
      }

      foreach (CachedChatClient entry in IdleEvicted)
      {
        pending.Add(entry.Client);
      }

      clients = [.. pending];
      LiveClients.Clear();
      IdleEvicted.Clear();
    }

    foreach (IChatClient client in clients)
    {
      client.Dispose();
    }
  }

  /// <summary>Caller holds <see cref="Gate"/> and <see cref="LiveClients"/> is not empty.</summary>
  private CachedChatClient OldestLive()
  {
    CachedChatClient? oldest = null;
    foreach (CachedChatClient candidate in LiveClients.Values)
    {
      if (oldest is null || candidate.LastUsed < oldest.LastUsed)
      {
        oldest = candidate;
      }
    }

    return oldest ?? throw new InvalidOperationException("The chat client cache was empty.");
  }

  /// <summary>Caller holds <see cref="Gate"/>.</summary>
  private CachedChatClient? FindCached(IChatClient client)
  {
    foreach (CachedChatClient entry in LiveClients.Values)
    {
      if (ReferenceEquals(entry.Client, client))
      {
        return entry;
      }
    }

    foreach (CachedChatClient entry in IdleEvicted)
    {
      if (ReferenceEquals(entry.Client, client))
      {
        return entry;
      }
    }

    return null;
  }

  /// <summary>One cached client and how many completions are still using it.</summary>
  private sealed class CachedChatClient
  {
    public required string Key { get; init; }

    public required IChatClient Client { get; init; }

    public int Uses { get; set; }

    public bool Evicted { get; set; }

    public long LastUsed { get; set; }
  }
}
