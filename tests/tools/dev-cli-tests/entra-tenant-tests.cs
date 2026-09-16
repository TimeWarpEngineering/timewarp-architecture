// ReSharper disable InconsistentNaming
namespace EntraTenants_;

public class FormatTenantLine_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FormatTenantLine_Given_>();

  public static Task ResolvedWithDomain_Should_IncludeNameDomainAndId()
  {
    EntraTenant tenant = new(
      "30f3971f-4719-4f20-9b6f-88916e0b95bd",
      "CrunchIt, LLC",
      "crunchitfs.com",
      NameResolved: true);
    EntraTenants.FormatTenantLine(tenant)
      .ShouldBe("CrunchIt, LLC (crunchitfs.com) — 30f3971f-4719-4f20-9b6f-88916e0b95bd");
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
}

public class TryReadOrganization_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TryReadOrganization_Given_>();

  public static Task HappyGraphPayload_Should_ParseDefaultDomain()
  {
    const string json = """
      {"value":[{"id":"30f3971f-4719-4f20-9b6f-88916e0b95bd","displayName":"CrunchIt, LLC","verifiedDomains":[{"name":"crunchitfs.onmicrosoft.com","isDefault":false,"isInitial":true},{"name":"crunchitfs.com","isDefault":true,"isInitial":false}]}]}
      """;
    EntraTenants.TryReadOrganization(json, out string? id, out string? displayName, out string? defaultDomain)
      .ShouldBeTrue();
    id.ShouldBe("30f3971f-4719-4f20-9b6f-88916e0b95bd");
    displayName.ShouldBe("CrunchIt, LLC");
    defaultDomain.ShouldBe("crunchitfs.com");
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
      [{"tenantId":"30f3971f-4719-4f20-9b6f-88916e0b95bd"},{"id":"74ca6706-93ef-4a23-b05a-e9ab17b7f86f"}]
      """;
    EntraTenants.TryReadTenantIdsFromTenantList(json, out IReadOnlyList<string> ids).ShouldBeTrue();
    ids.ShouldBe(
    [
      "30f3971f-4719-4f20-9b6f-88916e0b95bd",
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
      ["30f3971f-4719-4f20-9b6f-88916e0b95bd", ""],
      ["30F3971F-4719-4F20-9B6F-88916E0B95BD", "74ca6706-93ef-4a23-b05a-e9ab17b7f86f"]);
    ids.ShouldBe(
    [
      "30f3971f-4719-4f20-9b6f-88916e0b95bd",
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
    EntraTenant existing = new("30f3971f-4719-4f20-9b6f-88916e0b95bd", null, null, NameResolved: false);
    EntraTenant incoming = new(
      "30f3971f-4719-4f20-9b6f-88916e0b95bd",
      "CrunchIt, LLC",
      "crunchitfs.com",
      NameResolved: true);
    EntraTenant merged = EntraTenants.MergeTenant(existing, incoming);
    merged.DisplayName.ShouldBe("CrunchIt, LLC");
    merged.DefaultDomain.ShouldBe("crunchitfs.com");
    merged.NameResolved.ShouldBeTrue();
    return Task.CompletedTask;
  }
}

public class SelectTenant_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SelectTenant_Given_>();

  private static readonly EntraTenant CrunchIt = new(
    "30f3971f-4719-4f20-9b6f-88916e0b95bd",
    "CrunchIt, LLC",
    "crunchitfs.com",
    NameResolved: true);

  private static readonly EntraTenant Other = new(
    "74ca6706-93ef-4a23-b05a-e9ab17b7f86f",
    "Other Org",
    "other.example",
    NameResolved: true);

  public static Task MatchById_DifferentGuidCasing_Should_Select()
  {
    TenantSelection selection = EntraTenants.SelectTenant(
      [CrunchIt, Other],
      "30F3971F-4719-4F20-9B6F-88916E0B95BD");
    selection.Status.ShouldBe(TenantSelectionStatus.Selected);
    selection.Selected.ShouldBe(CrunchIt);
    return Task.CompletedTask;
  }

  public static Task MatchByDomain_Should_Select()
  {
    TenantSelection selection = EntraTenants.SelectTenant([CrunchIt, Other], "crunchitfs.com");
    selection.Status.ShouldBe(TenantSelectionStatus.Selected);
    selection.Selected.ShouldBe(CrunchIt);
    return Task.CompletedTask;
  }

  public static Task MatchByDisplayName_CaseInsensitive_Should_Select()
  {
    TenantSelection selection = EntraTenants.SelectTenant([CrunchIt, Other], "crunchit, llc");
    selection.Status.ShouldBe(TenantSelectionStatus.Selected);
    selection.Selected.ShouldBe(CrunchIt);
    return Task.CompletedTask;
  }

  public static Task OmittedWithOneTenant_Should_Select()
  {
    TenantSelection selection = EntraTenants.SelectTenant([CrunchIt], null);
    selection.Status.ShouldBe(TenantSelectionStatus.Selected);
    selection.Selected.ShouldBe(CrunchIt);
    return Task.CompletedTask;
  }

  public static Task OmittedWithTwoTenants_Should_BeAmbiguous()
  {
    TenantSelection selection = EntraTenants.SelectTenant([CrunchIt, Other], "  ");
    selection.Status.ShouldBe(TenantSelectionStatus.Ambiguous);
    selection.Selected.ShouldBeNull();
    selection.Candidates.Count.ShouldBe(2);
    return Task.CompletedTask;
  }

  public static Task ProvidedValueMatchesNothing_Should_BeNotFound()
  {
    TenantSelection selection = EntraTenants.SelectTenant([CrunchIt, Other], "missing.example");
    selection.Status.ShouldBe(TenantSelectionStatus.NotFound);
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
    EntraTenants.DefaultAppDisplayName("crunchitfs.com")
      .ShouldBe("TimeWarp Architecture Dev (crunchitfs.com)");
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
    IReadOnlyList<string> names = EntraTenants.AppLookupNames(null, "crunchitfs.com");
    names.ShouldBe(
    [
      "TimeWarp Architecture Dev (crunchitfs.com)",
      EntraSetup.DefaultDisplayName
    ]);
    return Task.CompletedTask;
  }

  public static Task ExplicitName_Should_ReturnOnlyThat()
  {
    IReadOnlyList<string> names = EntraTenants.AppLookupNames(" My App ", "crunchitfs.com");
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
    EntraTenants.SignInAudienceSummary("crunchitfs.com")
      .ShouldBe(
        "Sign in with an @crunchitfs.com account (single-tenant app). Other tenants' accounts must be invited as guests first.");
    return Task.CompletedTask;
  }

  public static Task WithoutDomain_Should_UseGenericWording()
  {
    EntraTenants.SignInAudienceSummary(null)
      .ShouldBe(
        "Sign in with an account from this tenant (single-tenant app). Other tenants' accounts must be invited as guests first.");
    return Task.CompletedTask;
  }
}
