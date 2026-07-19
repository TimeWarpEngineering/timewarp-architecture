// ReSharper disable InconsistentNaming
namespace InMemoryAgentKeyChallengeStore_;

using InMemoryWebAuthnChallengeStore_;

public class Issue_And_TryConsume
{
  public void Consume_is_one_time()
  {
    var store = new InMemoryAgentKeyChallengeStore();
    byte[] challenge = store.Issue(AgentKeyCeremonyType.Registration);

    store.TryConsume(AgentKeyCeremonyType.Registration, challenge).ShouldBeTrue();
    store.TryConsume(AgentKeyCeremonyType.Registration, challenge).ShouldBeFalse();
  }

  public void Unknown_challenge_returns_false()
  {
    var store = new InMemoryAgentKeyChallengeStore();
    byte[] neverIssued = RandomNumberGenerator.GetBytes(32);

    store.TryConsume(AgentKeyCeremonyType.Registration, neverIssued).ShouldBeFalse();
  }

  public void Wrong_ceremony_type_returns_false()
  {
    var store = new InMemoryAgentKeyChallengeStore();
    byte[] challenge = store.Issue(AgentKeyCeremonyType.Registration);

    store.TryConsume(AgentKeyCeremonyType.TokenIssuance, challenge).ShouldBeFalse();
  }

  public void Distinct_issues_return_distinct_challenges()
  {
    var store = new InMemoryAgentKeyChallengeStore();
    byte[] a = store.Issue(AgentKeyCeremonyType.Registration);
    byte[] b = store.Issue(AgentKeyCeremonyType.Registration);

    a.ShouldNotBe(b);
  }
}

public class Expiry
{
  public void Expired_challenge_is_not_consumable()
  {
    var timeProvider = new ManualTimeProvider(DateTimeOffset.UtcNow);
    var store = new InMemoryAgentKeyChallengeStore(timeProvider, TimeSpan.FromMinutes(5));

    byte[] challenge = store.Issue(AgentKeyCeremonyType.TokenIssuance);
    timeProvider.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));

    store.TryConsume(AgentKeyCeremonyType.TokenIssuance, challenge).ShouldBeFalse();
  }

  public void Not_yet_expired_challenge_is_consumable()
  {
    var timeProvider = new ManualTimeProvider(DateTimeOffset.UtcNow);
    var store = new InMemoryAgentKeyChallengeStore(timeProvider, TimeSpan.FromMinutes(5));

    byte[] challenge = store.Issue(AgentKeyCeremonyType.TokenIssuance);
    timeProvider.Advance(TimeSpan.FromMinutes(4));

    store.TryConsume(AgentKeyCeremonyType.TokenIssuance, challenge).ShouldBeTrue();
  }
}

public class CapEviction
{
  public void Oldest_entry_is_evicted_when_at_capacity()
  {
    var timeProvider = new ManualTimeProvider(DateTimeOffset.UtcNow);
    var store = new InMemoryAgentKeyChallengeStore(timeProvider, TimeSpan.FromMinutes(5), maxEntries: 2);

    byte[] first = store.Issue(AgentKeyCeremonyType.Registration);
    timeProvider.Advance(TimeSpan.FromSeconds(1));
    byte[] second = store.Issue(AgentKeyCeremonyType.Registration);
    timeProvider.Advance(TimeSpan.FromSeconds(1));
    // Store is at capacity (2); this Issue evicts the oldest (first) before adding.
    byte[] third = store.Issue(AgentKeyCeremonyType.Registration);

    store.TryConsume(AgentKeyCeremonyType.Registration, first).ShouldBeFalse();
    store.TryConsume(AgentKeyCeremonyType.Registration, second).ShouldBeTrue();
    store.TryConsume(AgentKeyCeremonyType.Registration, third).ShouldBeTrue();
  }
}
