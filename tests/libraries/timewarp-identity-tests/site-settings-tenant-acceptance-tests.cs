#region Purpose
// SiteSettings tenant acceptance (task 306): restricted list, any tenant, and list normalization.
#endregion

namespace SiteSettingsTenantAcceptance_;

public class SiteSettings_Given_
{
  private static readonly Guid Home = Guid.Parse("00000000-0000-0000-0000-000000000001");
  private static readonly Guid Other = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SiteSettings_Given_>();

  public static Task Create_Should_Start_Restricted_Empty_And_Initialized()
  {
    SiteSettings settings = SiteSettings.Create();
    settings.EntraTenantAcceptanceMode.ShouldBe(EntraTenantAcceptanceMode.Restricted);
    settings.EntraAcceptedTenants.ShouldBeEmpty();
    settings.EntraTenantAcceptanceInitialized.ShouldBeTrue();
    settings.AcceptsEntraTenant(Home).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Restricted_Should_Accept_Only_Listed_Tenants()
  {
    SiteSettings settings = SiteSettings.Create(entraAcceptedTenants: [EntraAcceptedTenant.IdOnly(Home)]);
    settings.AcceptsEntraTenant(Home).ShouldBeTrue();
    settings.AcceptsEntraTenant(Other).ShouldBeFalse();
    settings.AcceptsEntraTenant(Guid.Empty).ShouldBeFalse();
    settings.AcceptsEntraTenant(EntraTenantIds.Consumers).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Any_Should_Accept_A_Different_Non_Empty_Tid()
  {
    SiteSettings settings = SiteSettings.Create(
      entraTenantAcceptanceMode: EntraTenantAcceptanceMode.Any,
      entraAcceptedTenants: [EntraAcceptedTenant.IdOnly(Home)]);
    settings.AcceptsEntraTenant(Other).ShouldBeTrue();
    settings.AcceptsEntraTenant(EntraTenantIds.Consumers).ShouldBeTrue();
    settings.AcceptsEntraTenant(Guid.Empty).ShouldBeFalse();
    settings.EntraAcceptedTenants.ShouldBe([EntraAcceptedTenant.IdOnly(Home)]);
    return Task.CompletedTask;
  }

  public static Task Replace_Should_Collapse_Duplicates_Sort_And_Mark_Initialized()
  {
    SiteSettings settings = SiteSettings.Create(entraTenantAcceptanceInitialized: false);
    settings.ReplaceEntraTenantAcceptance(
      EntraTenantAcceptanceMode.Restricted,
      [EntraAcceptedTenant.IdOnly(Other), EntraAcceptedTenant.IdOnly(Home), EntraAcceptedTenant.IdOnly(Home)]);
    settings.EntraTenantAcceptanceInitialized.ShouldBeTrue();
    EntraTenantIds.TryNormalize(
      [EntraAcceptedTenant.IdOnly(Other), EntraAcceptedTenant.IdOnly(Home), EntraAcceptedTenant.IdOnly(Home)],
      out IReadOnlyList<EntraAcceptedTenant> expected,
      out string? error)
      .ShouldBeTrue(error);
    settings.EntraAcceptedTenants.ShouldBe(expected);
    return Task.CompletedTask;
  }

  public static Task ReplacePolicy_Should_Keep_Tenant_Acceptance()
  {
    SiteSettings settings = SiteSettings.Create(
      entraTenantAcceptanceMode: EntraTenantAcceptanceMode.Any,
      entraAcceptedTenants: [EntraAcceptedTenant.IdOnly(Home)]);
    settings.ReplacePolicy(true, true, PasskeyPromptMode.Required);
    settings.EntraTenantAcceptanceMode.ShouldBe(EntraTenantAcceptanceMode.Any);
    settings.EntraAcceptedTenants.ShouldBe([EntraAcceptedTenant.IdOnly(Home)]);
    return Task.CompletedTask;
  }

  public static Task Empty_Guid_Should_Throw()
  {
    Should.Throw<ArgumentException>(() =>
      SiteSettings.Create(entraAcceptedTenants: [EntraAcceptedTenant.IdOnly(Guid.Empty)]));
    return Task.CompletedTask;
  }

  public static Task Parse_Should_Accept_Blank_And_Separators_And_Reject_Text()
  {
    EntraTenantIds.TryParse("  ", out IReadOnlyList<Guid> blank, out string? blankError).ShouldBeTrue(blankError);
    blank.ShouldBeEmpty();

    string text = $"{Other}, {Home}\n{Home}";
    EntraTenantIds.TryParse(text, out IReadOnlyList<Guid> parsed, out string? error).ShouldBeTrue(error);
    parsed.Count.ShouldBe(2);
    parsed.ShouldContain(Home);
    parsed.ShouldContain(Other);

    EntraTenantIds.TryParse("not-a-guid", out _, out string? parseError).ShouldBeFalse();
    parseError.ShouldNotBeNull();
    parseError.ShouldContain("not-a-guid");
    return Task.CompletedTask;
  }

  public static Task Json_Should_Round_Trip_And_Ignore_Garbage()
  {
    string json = EntraTenantIds.ToJson([EntraAcceptedTenant.IdOnly(Home)]);
    EntraTenantIds.FromJson(json).ShouldBe([EntraAcceptedTenant.IdOnly(Home)]);
    json.ShouldContain("\"tenantId\"");
    json.ShouldContain("\"domain\":null");
    EntraTenantIds.FromJson("not-json").ShouldBeEmpty();
    EntraTenantIds.FromJson(null).ShouldBeEmpty();
    EntraTenantIds.FromJson($"[\"{Home:D}\"]").ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task Normalize_Should_Keep_One_Domain_And_Refuse_A_Conflict()
  {
    EntraAcceptedTenant bare = EntraAcceptedTenant.IdOnly(Home);
    EntraAcceptedTenant named = new(Home, " Contoso.COM ");
    EntraTenantIds.TryNormalize([bare, named], out IReadOnlyList<EntraAcceptedTenant> kept, out string? error)
      .ShouldBeTrue(error);
    kept.ShouldBe([new EntraAcceptedTenant(Home, "contoso.com")]);

    EntraTenantIds.TryNormalize(
      [new EntraAcceptedTenant(Home, "contoso.com"), new EntraAcceptedTenant(Home, "fabrikam.com")],
      out _,
      out string? conflict)
      .ShouldBeFalse();
    conflict.ShouldNotBeNull();
    conflict.ShouldContain(Home.ToString("D"));
    return Task.CompletedTask;
  }

  public static Task Normalize_Should_Refuse_An_Invalid_Domain()
  {
    EntraTenantIds.TryNormalize([new EntraAcceptedTenant(Home, "not a domain")], out _, out string? error)
      .ShouldBeFalse();
    error.ShouldBe("'not a domain' is not a domain name.");
    return Task.CompletedTask;
  }
}
