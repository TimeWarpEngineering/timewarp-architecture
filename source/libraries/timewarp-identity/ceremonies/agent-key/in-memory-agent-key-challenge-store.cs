#region Purpose
// Thread-safe in-memory IAgentKeyChallengeStore for unit tests and single-instance hosts.
#endregion

#region Design
// Thin wrapper over InMemoryChallengeStoreCore<AgentKeyCeremonyType> — see
// ceremonies/in-memory-challenge-store-core.cs for the shared prune/evict/consume behavior, and
// InMemoryWebAuthnChallengeStore (its sibling) for the original single-instance-semantics rationale.
#endregion

namespace TimeWarp.Identity;

public sealed class InMemoryAgentKeyChallengeStore : IAgentKeyChallengeStore
{
  private readonly InMemoryChallengeStoreCore<AgentKeyCeremonyType> Core;

  public InMemoryAgentKeyChallengeStore(TimeProvider? timeProvider = null, TimeSpan? timeToLive = null, int maxEntries = 10_000)
  {
    Core = new InMemoryChallengeStoreCore<AgentKeyCeremonyType>(timeProvider, timeToLive, maxEntries);
  }

  public byte[] Issue(AgentKeyCeremonyType ceremonyType) => Core.Issue(ceremonyType);

  public bool TryConsume(AgentKeyCeremonyType ceremonyType, byte[] challenge) => Core.TryConsume(ceremonyType, challenge);
}
