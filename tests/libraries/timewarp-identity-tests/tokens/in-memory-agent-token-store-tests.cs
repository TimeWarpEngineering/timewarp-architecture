// ReSharper disable InconsistentNaming
namespace InMemoryAgentTokenStore_;

using InMemoryWebAuthnChallengeStore_;

public class Issue_And_Validate
{
  public void Round_trips_principal_id_and_scopes()
  {
    var store = new InMemoryAgentTokenStore();
    PrincipalId principalId = PrincipalId.New();
    string[] scopes = [AgentScopes.IdentityRead];

    string token = store.Issue(principalId, scopes, TimeSpan.FromMinutes(15));
    AgentTokenGrant? grant = store.Validate(token);

    grant.ShouldNotBeNull();
    grant.PrincipalId.ShouldBe(principalId);
    grant.Scopes.ShouldBe(scopes);
  }

  public void Scopes_are_copied_not_aliased()
  {
    var store = new InMemoryAgentTokenStore();
    List<string> scopes = [AgentScopes.IdentityRead];

    string token = store.Issue(PrincipalId.New(), scopes, TimeSpan.FromMinutes(15));
    scopes.Add(AgentScopes.DemoInvoke); // mutate the caller's own list after Issue

    AgentTokenGrant? grant = store.Validate(token);
    grant.ShouldNotBeNull();
    grant.Scopes.Count.ShouldBe(1);
    grant.Scopes.ShouldBe([AgentScopes.IdentityRead]);
  }

  public void Unknown_token_returns_null()
  {
    var store = new InMemoryAgentTokenStore();

    store.Validate("never-issued-token").ShouldBeNull();
  }

  public void Garbage_token_returns_null_without_throwing()
  {
    var store = new InMemoryAgentTokenStore();

    store.Validate("!!!not-even-base64url!!!").ShouldBeNull();
  }

  public void Empty_token_returns_null_without_throwing()
  {
    var store = new InMemoryAgentTokenStore();

    store.Validate("").ShouldBeNull();
  }

  public void Distinct_issues_return_distinct_tokens()
  {
    var store = new InMemoryAgentTokenStore();
    string first = store.Issue(PrincipalId.New(), [AgentScopes.IdentityRead], TimeSpan.FromMinutes(15));
    string second = store.Issue(PrincipalId.New(), [AgentScopes.IdentityRead], TimeSpan.FromMinutes(15));

    first.ShouldNotBe(second);
  }
}

public class Expiry
{
  public void Expired_token_is_not_valid()
  {
    var timeProvider = new ManualTimeProvider(DateTimeOffset.UtcNow);
    var store = new InMemoryAgentTokenStore(timeProvider);

    string token = store.Issue(PrincipalId.New(), [AgentScopes.IdentityRead], TimeSpan.FromMinutes(15));
    timeProvider.Advance(TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(1));

    store.Validate(token).ShouldBeNull();
  }

  public void Not_yet_expired_token_is_valid()
  {
    var timeProvider = new ManualTimeProvider(DateTimeOffset.UtcNow);
    var store = new InMemoryAgentTokenStore(timeProvider);

    string token = store.Issue(PrincipalId.New(), [AgentScopes.IdentityRead], TimeSpan.FromMinutes(15));
    timeProvider.Advance(TimeSpan.FromMinutes(14));

    store.Validate(token).ShouldNotBeNull();
  }

  public void Validate_does_not_consume_the_token()
  {
    // Unlike the one-time challenge stores, a token authenticates every request for its whole
    // lifetime — Validate must be repeatable.
    var store = new InMemoryAgentTokenStore();
    string token = store.Issue(PrincipalId.New(), [AgentScopes.IdentityRead], TimeSpan.FromMinutes(15));

    store.Validate(token).ShouldNotBeNull();
    store.Validate(token).ShouldNotBeNull();
  }
}

public class CapEviction
{
  public void Oldest_entry_is_evicted_when_at_capacity()
  {
    var timeProvider = new ManualTimeProvider(DateTimeOffset.UtcNow);
    var store = new InMemoryAgentTokenStore(timeProvider, maxEntries: 2);

    string first = store.Issue(PrincipalId.New(), [AgentScopes.IdentityRead], TimeSpan.FromMinutes(15));
    timeProvider.Advance(TimeSpan.FromSeconds(1));
    string second = store.Issue(PrincipalId.New(), [AgentScopes.IdentityRead], TimeSpan.FromMinutes(15));
    timeProvider.Advance(TimeSpan.FromSeconds(1));
    // Store is at capacity (2); this Issue evicts the oldest (first) before adding.
    string third = store.Issue(PrincipalId.New(), [AgentScopes.IdentityRead], TimeSpan.FromMinutes(15));

    store.Validate(first).ShouldBeNull();
    store.Validate(second).ShouldNotBeNull();
    store.Validate(third).ShouldNotBeNull();
  }
}
