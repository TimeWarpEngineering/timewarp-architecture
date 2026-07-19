// ReSharper disable InconsistentNaming
namespace InMemoryWebAuthnChallengeStore_;

internal sealed class ManualTimeProvider : TimeProvider
{
  private DateTimeOffset Now;

  public ManualTimeProvider(DateTimeOffset start)
  {
    Now = start;
  }

  public override DateTimeOffset GetUtcNow() => Now;

  public void Advance(TimeSpan delta) => Now += delta;
}

public class Issue_And_TryConsume
{
  public void Consume_is_one_time()
  {
    var store = new InMemoryWebAuthnChallengeStore();
    byte[] challenge = store.Issue(WebAuthnCeremonyType.Registration);

    store.TryConsume(WebAuthnCeremonyType.Registration, challenge).ShouldBeTrue();
    store.TryConsume(WebAuthnCeremonyType.Registration, challenge).ShouldBeFalse();
  }

  public void Unknown_challenge_returns_false()
  {
    var store = new InMemoryWebAuthnChallengeStore();
    byte[] neverIssued = RandomNumberGenerator.GetBytes(32);

    store.TryConsume(WebAuthnCeremonyType.Registration, neverIssued).ShouldBeFalse();
  }

  public void Wrong_ceremony_type_returns_false()
  {
    var store = new InMemoryWebAuthnChallengeStore();
    byte[] challenge = store.Issue(WebAuthnCeremonyType.Registration);

    store.TryConsume(WebAuthnCeremonyType.Authentication, challenge).ShouldBeFalse();
  }

  public void Distinct_issues_return_distinct_challenges()
  {
    var store = new InMemoryWebAuthnChallengeStore();
    byte[] a = store.Issue(WebAuthnCeremonyType.Registration);
    byte[] b = store.Issue(WebAuthnCeremonyType.Registration);

    a.ShouldNotBe(b);
  }
}

public class Expiry
{
  public void Expired_challenge_is_not_consumable()
  {
    var timeProvider = new ManualTimeProvider(DateTimeOffset.UtcNow);
    var store = new InMemoryWebAuthnChallengeStore(timeProvider, TimeSpan.FromMinutes(5));

    byte[] challenge = store.Issue(WebAuthnCeremonyType.Authentication);
    timeProvider.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));

    store.TryConsume(WebAuthnCeremonyType.Authentication, challenge).ShouldBeFalse();
  }

  public void Not_yet_expired_challenge_is_consumable()
  {
    var timeProvider = new ManualTimeProvider(DateTimeOffset.UtcNow);
    var store = new InMemoryWebAuthnChallengeStore(timeProvider, TimeSpan.FromMinutes(5));

    byte[] challenge = store.Issue(WebAuthnCeremonyType.Authentication);
    timeProvider.Advance(TimeSpan.FromMinutes(4));

    store.TryConsume(WebAuthnCeremonyType.Authentication, challenge).ShouldBeTrue();
  }
}

public class CapEviction
{
  public void Oldest_entry_is_evicted_when_at_capacity()
  {
    var timeProvider = new ManualTimeProvider(DateTimeOffset.UtcNow);
    var store = new InMemoryWebAuthnChallengeStore(timeProvider, TimeSpan.FromMinutes(5), maxEntries: 2);

    byte[] first = store.Issue(WebAuthnCeremonyType.Registration);
    timeProvider.Advance(TimeSpan.FromSeconds(1));
    byte[] second = store.Issue(WebAuthnCeremonyType.Registration);
    timeProvider.Advance(TimeSpan.FromSeconds(1));
    // Store is at capacity (2); this Issue evicts the oldest (first) before adding.
    byte[] third = store.Issue(WebAuthnCeremonyType.Registration);

    store.TryConsume(WebAuthnCeremonyType.Registration, first).ShouldBeFalse();
    store.TryConsume(WebAuthnCeremonyType.Registration, second).ShouldBeTrue();
    store.TryConsume(WebAuthnCeremonyType.Registration, third).ShouldBeTrue();
  }
}
