namespace Principal_;

public class Create
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Create>();

  public static Task Sets_provisional_trust_tier_and_not_quarantined()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.TrustTier.ShouldBe(TrustTier.Provisional);
    principal.IsQuarantined.ShouldBeFalse();
    principal.IsActive.ShouldBeTrue();
    principal.IsFundedAndActive.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Assigns_non_empty_id_and_created_at()
  {
    DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-1);
    Principal principal = Principal.Create(PrincipalKind.Service);
    DateTimeOffset after = DateTimeOffset.UtcNow.AddSeconds(1);

    principal.Id.Value.ShouldNotBe(Guid.Empty);
    principal.Kind.ShouldBe(PrincipalKind.Service);
    principal.CreatedAt.ShouldBeInRange(before, after);
    principal.DisplayName.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Allows_agent_without_human()
  {
    Principal agent = Principal.Create(PrincipalKind.Agent);
    agent.Kind.ShouldBe(PrincipalKind.Agent);
    agent.DisplayName.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Rejects_none_kind()
  {
    Should.Throw<ArgumentOutOfRangeException>(() => Principal.Create(PrincipalKind.None));
    return Task.CompletedTask;
  }

  public static Task Rejects_undefined_kind()
  {
    Should.Throw<ArgumentOutOfRangeException>(() => Principal.Create((PrincipalKind)99));
    return Task.CompletedTask;
  }

  public static Task Version_defaults_to_zero()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.Version.ShouldBe(0);
    return Task.CompletedTask;
  }

  public static Task Different_ids_are_not_equal()
  {
    Principal a = Principal.Create(PrincipalKind.Human);
    Principal b = Principal.Create(PrincipalKind.Human);
    a.ShouldNotBe(b);
    return Task.CompletedTask;
  }
}

public class SetDisplayName
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SetDisplayName>();

  public static Task Trims_whitespace()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.SetDisplayName("  Ada  ");
    principal.DisplayName.ShouldBe("Ada");
    return Task.CompletedTask;
  }

  public static Task Whitespace_only_becomes_null()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.SetDisplayName("   ");
    principal.DisplayName.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Null_clears_name()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.SetDisplayName("Ada");
    principal.SetDisplayName(null);
    principal.DisplayName.ShouldBeNull();
    return Task.CompletedTask;
  }
}

public class Promote
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Promote>();

  public static Task Allows_any_strictly_higher_progression_tier()
  {
    Principal principal = Principal.Create(PrincipalKind.Agent);
    principal.Promote(TrustTier.Funded);
    principal.TrustTier.ShouldBe(TrustTier.Funded);
    return Task.CompletedTask;
  }

  public static Task Allows_step_to_keyed_then_funded_then_established()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.Promote(TrustTier.Keyed);
    principal.TrustTier.ShouldBe(TrustTier.Keyed);

    principal.Promote(TrustTier.Funded);
    principal.TrustTier.ShouldBe(TrustTier.Funded);

    principal.Promote(TrustTier.Established);
    principal.TrustTier.ShouldBe(TrustTier.Established);
    principal.IsFundedAndActive.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Rejects_none_target()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    Should.Throw<ArgumentOutOfRangeException>(() => principal.Promote(TrustTier.None));
    return Task.CompletedTask;
  }

  public static Task Rejects_undefined_tier()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    Should.Throw<ArgumentOutOfRangeException>(() => principal.Promote((TrustTier)99));
    return Task.CompletedTask;
  }

  public static Task Rejects_same_or_lower_tier()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.Promote(TrustTier.Funded);

    Should.Throw<InvalidOperationException>(() => principal.Promote(TrustTier.Funded));
    Should.Throw<InvalidOperationException>(() => principal.Promote(TrustTier.Keyed));
    Should.Throw<InvalidOperationException>(() => principal.Promote(TrustTier.Provisional));
    return Task.CompletedTask;
  }

  public static Task Rejects_when_quarantined()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.Quarantine();
    Should.Throw<InvalidOperationException>(() => principal.Promote(TrustTier.Keyed));
    return Task.CompletedTask;
  }
}

public class RecordCredentialAttached
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RecordCredentialAttached>();

  public static Task Promotes_provisional_to_keyed_when_not_quarantined()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.RecordCredentialAttached();
    principal.TrustTier.ShouldBe(TrustTier.Keyed);
    return Task.CompletedTask;
  }

  public static Task Promotes_provisional_to_keyed_even_when_quarantined()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.Quarantine();
    principal.RecordCredentialAttached();
    principal.TrustTier.ShouldBe(TrustTier.Keyed);
    principal.IsQuarantined.ShouldBeTrue();
    principal.IsActive.ShouldBeFalse();
    principal.IsFundedAndActive.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task No_op_when_already_keyed_or_higher()
  {
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.Promote(TrustTier.Funded);
    principal.RecordCredentialAttached();
    principal.TrustTier.ShouldBe(TrustTier.Funded);
    return Task.CompletedTask;
  }
}

public class QuarantineLifecycle
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<QuarantineLifecycle>();

  public static Task Quarantine_and_clear()
  {
    Principal principal = Principal.Create(PrincipalKind.Service);
    principal.IsQuarantined.ShouldBeFalse();
    principal.IsActive.ShouldBeTrue();

    principal.Quarantine();
    principal.IsQuarantined.ShouldBeTrue();
    principal.IsActive.ShouldBeFalse();

    principal.ClearQuarantine();
    principal.IsQuarantined.ShouldBeFalse();
    principal.IsActive.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task IsFundedAndActive_requires_funded_and_not_quarantined()
  {
    Principal principal = Principal.Create(PrincipalKind.Agent);
    principal.IsFundedAndActive.ShouldBeFalse();

    principal.Promote(TrustTier.Funded);
    principal.IsFundedAndActive.ShouldBeTrue();

    principal.Quarantine();
    principal.IsFundedAndActive.ShouldBeFalse();

    principal.ClearQuarantine();
    principal.IsFundedAndActive.ShouldBeTrue();

    principal.Promote(TrustTier.Established);
    principal.IsFundedAndActive.ShouldBeTrue();
    return Task.CompletedTask;
  }
}
