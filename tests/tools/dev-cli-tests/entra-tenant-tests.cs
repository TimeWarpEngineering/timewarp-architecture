// ReSharper disable InconsistentNaming
namespace EntraTenants_;

public class FormatTenantLine_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FormatTenantLine_Given_>();

  public static Task ResolvedWithDomain_Should_IncludeNameDomainAndId()
  {
    EntraTenant tenant = new(
      "00000000-0000-0000-0000-000000000001",
      "Contoso, Ltd",
      "contoso.com",
      NameResolved: true);
    EntraTenants.FormatTenantLine(tenant)
      .ShouldBe("Contoso, Ltd (contoso.com) — 00000000-0000-0000-0000-000000000001");
    return Task.CompletedTask;
  }

  public static Task Unresolved_Should_ShowGuidAndNameUnavailable()
  {
    EntraTenant tenant = new(
      "74ca6706-93ef-4a23-b05a-e9ab17b7f86f",
      null,
      null,
      NameResolved: false);
    EntraTenants.FormatTenantLine(tenant)
      .ShouldBe("74ca6706-93ef-4a23-b05a-e9ab17b7f86f (name unavailable)");
    return Task.CompletedTask;
  }

  public static Task DomainOnlyResolved_Should_IncludeDomainAndId()
  {
    EntraTenant tenant = new(
      "74ca6706-93ef-4a23-b05a-e9ab17b7f86f",
      null,
      "gkenastongmail932.onmicrosoft.com",
      NameResolved: true);
    EntraTenants.FormatTenantLine(tenant)
      .ShouldBe("gkenastongmail932.onmicrosoft.com — 74ca6706-93ef-4a23-b05a-e9ab17b7f86f");
    return Task.CompletedTask;
  }
}

public class TryReadOrganization_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TryReadOrganization_Given_>();

  public static Task HappyGraphPayload_Should_ParseDefaultDomain()
  {
    const string json = """
      {"value":[{"id":"00000000-0000-0000-0000-000000000001","displayName":"Contoso, Ltd","verifiedDomains":[{"name":"contoso.onmicrosoft.com","isDefault":false,"isInitial":true},{"name":"contoso.com","isDefault":true,"isInitial":false}]}]}
      """;
    EntraTenants.TryReadOrganization(json, out string? id, out string? displayName, out string? defaultDomain)
      .ShouldBeTrue();
    id.ShouldBe("00000000-0000-0000-0000-000000000001");
    displayName.ShouldBe("Contoso, Ltd");
    defaultDomain.ShouldBe("contoso.com");
    return Task.CompletedTask;
  }

  public static Task InvalidJson_Should_ReturnFalse()
  {
    EntraTenants.TryReadOrganization("{not-json", out _, out _, out _).ShouldBeFalse();
    return Task.CompletedTask;
  }
}

public class TryReadTenantIdsFromTenantList_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TryReadTenantIdsFromTenantList_Given_>();

  public static Task TenantIdOrIdProperty_Should_CollectIds()
  {
    const string json = """
      [{"tenantId":"00000000-0000-0000-0000-000000000001"},{"id":"74ca6706-93ef-4a23-b05a-e9ab17b7f86f"}]
      """;
    EntraTenants.TryReadTenantIdsFromTenantList(json, out IReadOnlyList<string> ids).ShouldBeTrue();
    ids.ShouldBe(
    [
      "00000000-0000-0000-0000-000000000001",
      "74ca6706-93ef-4a23-b05a-e9ab17b7f86f"
    ]);
    return Task.CompletedTask;
  }
}

public class CollectTenantIds_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CollectTenantIds_Given_>();

  public static Task Union_Should_DedupeCaseInsensitivePreserveFirstCasing()
  {
    IReadOnlyList<string> ids = EntraTenants.CollectTenantIds(
      ["aaaaaaaa-0000-0000-0000-000000000001", ""],
      ["AAAAAAAA-0000-0000-0000-000000000001", "74ca6706-93ef-4a23-b05a-e9ab17b7f86f"]);
    ids.ShouldBe(
    [
      "aaaaaaaa-0000-0000-0000-000000000001",
      "74ca6706-93ef-4a23-b05a-e9ab17b7f86f"
    ]);
    return Task.CompletedTask;
  }
}

public class MergeTenant_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<MergeTenant_Given_>();

  public static Task ResolvedIncoming_Should_FillUnresolvedExisting()
  {
    EntraTenant existing = new("00000000-0000-0000-0000-000000000001", null, null, NameResolved: false);
    EntraTenant incoming = new(
      "00000000-0000-0000-0000-000000000001",
      "Contoso, Ltd",
      "contoso.com",
      NameResolved: true);
    EntraTenant merged = EntraTenants.MergeTenant(existing, incoming);
    merged.DisplayName.ShouldBe("Contoso, Ltd");
    merged.DefaultDomain.ShouldBe("contoso.com");
    merged.NameResolved.ShouldBeTrue();
    return Task.CompletedTask;
  }
}

public class SelectTenant_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SelectTenant_Given_>();

  private static readonly EntraTenant Contoso = new(
    "00000000-0000-0000-0000-000000000001",
    "Contoso, Ltd",
    "contoso.com",
    NameResolved: true);

  private static readonly EntraTenant Other = new(
    "74ca6706-93ef-4a23-b05a-e9ab17b7f86f",
    "Other Org",
    "other.example",
    NameResolved: true);

  public static Task MatchById_DifferentGuidCasing_Should_Select()
  {
    EntraTenant tenant = new(
      "aaaaaaaa-0000-0000-0000-000000000001",
      "Contoso, Ltd",
      "contoso.com",
      NameResolved: true);
    TenantSelection selection = EntraTenants.SelectTenant(
      [tenant, Other],
      "AAAAAAAA-0000-0000-0000-000000000001");
    selection.Status.ShouldBe(TenantSelectionStatus.Selected);
    selection.Selected.ShouldBe(tenant);
    return Task.CompletedTask;
  }

  public static Task MatchByDomain_Should_Select()
  {
    TenantSelection selection = EntraTenants.SelectTenant([Contoso, Other], "contoso.com");
    selection.Status.ShouldBe(TenantSelectionStatus.Selected);
    selection.Selected.ShouldBe(Contoso);
    return Task.CompletedTask;
  }

  public static Task MatchByDisplayName_CaseInsensitive_Should_Select()
  {
    TenantSelection selection = EntraTenants.SelectTenant([Contoso, Other], "contoso, ltd");
    selection.Status.ShouldBe(TenantSelectionStatus.Selected);
    selection.Selected.ShouldBe(Contoso);
    return Task.CompletedTask;
  }

  public static Task OmittedWithOneTenant_Should_Select()
  {
    TenantSelection selection = EntraTenants.SelectTenant([Contoso], null);
    selection.Status.ShouldBe(TenantSelectionStatus.Selected);
    selection.Selected.ShouldBe(Contoso);
    return Task.CompletedTask;
  }

  public static Task OmittedWithTwoTenants_Should_BeAmbiguous()
  {
    TenantSelection selection = EntraTenants.SelectTenant([Contoso, Other], "  ");
    selection.Status.ShouldBe(TenantSelectionStatus.Ambiguous);
    selection.Selected.ShouldBeNull();
    selection.Candidates.Count.ShouldBe(2);
    return Task.CompletedTask;
  }

  public static Task ProvidedValueMatchesNothing_Should_BeNotFound()
  {
    TenantSelection selection = EntraTenants.SelectTenant([Contoso, Other], "missing.example");
    selection.Status.ShouldBe(TenantSelectionStatus.NotFound);
    selection.Candidates.Count.ShouldBe(2);
    return Task.CompletedTask;
  }

  public static Task ProvidedDisplayNameMatchesTwo_Should_BeAmbiguous()
  {
    EntraTenant firstDefault = new(
      "74ca6706-93ef-4a23-b05a-e9ab17b7f86f",
      "Default Directory",
      "one.onmicrosoft.com",
      NameResolved: true);
    EntraTenant secondDefault = new(
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "Default Directory",
      "two.onmicrosoft.com",
      NameResolved: true);
    TenantSelection selection = EntraTenants.SelectTenant(
      [Contoso, firstDefault, secondDefault],
      "Default Directory");
    selection.Status.ShouldBe(TenantSelectionStatus.Ambiguous);
    selection.Selected.ShouldBeNull();
    selection.Candidates.Count.ShouldBe(2);
    return Task.CompletedTask;
  }
}

public class DefaultAppDisplayName_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DefaultAppDisplayName_Given_>();

  public static Task WithDomain_Should_Suffix()
  {
    EntraTenants.DefaultAppDisplayName("contoso.com")
      .ShouldBe("TimeWarp Architecture Dev (contoso.com)");
    return Task.CompletedTask;
  }

  public static Task NullDomain_Should_ReturnBare()
  {
    EntraTenants.DefaultAppDisplayName(null).ShouldBe(EntraSetup.DefaultDisplayName);
    return Task.CompletedTask;
  }
}

public class AppLookupNames_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AppLookupNames_Given_>();

  public static Task Default_Should_ReturnSuffixedThenLegacy()
  {
    IReadOnlyList<string> names = EntraTenants.AppLookupNames(null, "contoso.com");
    names.ShouldBe(
    [
      "TimeWarp Architecture Dev (contoso.com)",
      EntraSetup.DefaultDisplayName
    ]);
    return Task.CompletedTask;
  }

  public static Task ExplicitName_Should_ReturnOnlyThat()
  {
    IReadOnlyList<string> names = EntraTenants.AppLookupNames(" My App ", "contoso.com");
    names.ShouldBe(["My App"]);
    return Task.CompletedTask;
  }
}

public class ResolveExistingApp_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ResolveExistingApp_Given_>();

  public static Task PreferSuffixed_Should_IgnoreLegacyIds()
  {
    EntraTenants.ResolveExistingApp(
      ["preferred-id"],
      ["legacy-a", "legacy-b"],
      out string? appId,
      out bool ambiguous);
    appId.ShouldBe("preferred-id");
    ambiguous.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task ReuseLegacy_WhenSuffixedMissing()
  {
    EntraTenants.ResolveExistingApp(
      [],
      ["legacy-id"],
      out string? appId,
      out bool ambiguous);
    appId.ShouldBe("legacy-id");
    ambiguous.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task TwoPreferred_Should_BeAmbiguous()
  {
    EntraTenants.ResolveExistingApp(
      ["a", "b"],
      ["legacy"],
      out string? appId,
      out bool ambiguous);
    appId.ShouldBeNull();
    ambiguous.ShouldBeTrue();
    return Task.CompletedTask;
  }
}

public class PublicOriginMissingFromRedirectUris_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PublicOriginMissingFromRedirectUris_Given_>();

  public static Task Missing_Should_BeTrue()
  {
    EntraTenants.PublicOriginMissingFromRedirectUris(
      "https://arch.timewarp.work",
      ["https://localhost:63611/signin-oidc"]).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task PresentAsPrefix_Should_BeFalse()
  {
    EntraTenants.PublicOriginMissingFromRedirectUris(
      "https://arch.timewarp.work/",
      ["https://arch.timewarp.work/signin-oidc"]).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task NullOrigin_Should_BeFalse()
  {
    EntraTenants.PublicOriginMissingFromRedirectUris(
      null,
      ["https://localhost:63611/signin-oidc"]).ShouldBeFalse();
    return Task.CompletedTask;
  }
}

public class SignInAudienceSummary_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SignInAudienceSummary_Given_>();

  public static Task WithDomain_Should_MentionAtDomain()
  {
    EntraTenants.SignInAudienceSummary("contoso.com")
      .ShouldBe(
        "Sign in with an @contoso.com account (single-tenant app). Other tenants' accounts must be invited as guests first.");
    return Task.CompletedTask;
  }

  public static Task WithoutDomain_Should_UseGenericWording()
  {
    EntraTenants.SignInAudienceSummary(null)
      .ShouldBe(
        "Sign in with an account from this tenant (single-tenant app). Other tenants' accounts must be invited as guests first.");
    return Task.CompletedTask;
  }

  public static Task MultiTenantNote_Should_Name_Admin_Policy_And_Account_Types()
  {
    string note = EntraTenants.MultiTenantAcceptanceNote();
    note.ShouldContain("/Admin/Authentication");
    note.ShouldContain("AzureADMultipleOrgs");
    note.ShouldContain("AzureADandPersonalMicrosoftAccount");
    note.ShouldContain("organizations");
    note.ShouldContain("9188040d-6c67-4c5b-b112-36a304b66dad");
    note.ShouldContain("no tenant blocklist");
    return Task.CompletedTask;
  }
}
