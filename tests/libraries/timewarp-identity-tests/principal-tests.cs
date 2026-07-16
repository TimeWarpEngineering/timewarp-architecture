namespace Principal_;

public class Create
{
  public void Sets_keyed_trust_tier()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.TrustTier.ShouldBe(TrustTier.Keyed);
  }

  public void Assigns_non_empty_id_and_created_at()
  {
    DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-1);
    Principal principal = Principal.Create(PrincipalKind.Service);
    DateTimeOffset after = DateTimeOffset.UtcNow.AddSeconds(1);

    principal.Id.Value.ShouldNotBe(Guid.Empty);
    principal.Kind.ShouldBe(PrincipalKind.Service);
    principal.CreatedAt.ShouldBeInRange(before, after);
    principal.DisplayName.ShouldBeNull();
  }

  public void Allows_agent_without_human()
  {
    Principal agent = Principal.Create(PrincipalKind.Agent);
    agent.Kind.ShouldBe(PrincipalKind.Agent);
    agent.DisplayName.ShouldBeNull();
  }

  public void Rejects_undefined_kind() =>
    Should.Throw<ArgumentOutOfRangeException>(() => Principal.Create((PrincipalKind)99));
}

public class SetDisplayName
{
  public void Trims_whitespace()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.SetDisplayName("  Ada  ");
    principal.DisplayName.ShouldBe("Ada");
  }

  public void Whitespace_only_becomes_null()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.SetDisplayName("   ");
    principal.DisplayName.ShouldBeNull();
  }

  public void Null_clears_name()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.SetDisplayName("Ada");
    principal.SetDisplayName(null);
    principal.DisplayName.ShouldBeNull();
  }
}

public class SetTrustTier
{
  public void Updates_tier()
  {
    Principal principal = Principal.Create(PrincipalKind.Agent);
    principal.SetTrustTier(TrustTier.Funded);
    principal.TrustTier.ShouldBe(TrustTier.Funded);
  }

  public void Rejects_undefined_tier()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    Should.Throw<ArgumentOutOfRangeException>(() => principal.SetTrustTier((TrustTier)99));
  }
}
