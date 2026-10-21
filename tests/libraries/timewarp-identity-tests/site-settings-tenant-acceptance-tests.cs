#region Purpose
// SiteSettings tenant acceptance (task 306): restricted list, any tenant, and list normalization.
#endregion

namespace SiteSettingsTenantAcceptance_;

public class SiteSettings_Given_
{
  private static readonly Guid Home = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");
  private static readonly Guid Other = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SiteSettings_Given_>();

  public static Task Create_Should_Start_Restricted_Empty_And_Initialized()
  {
    SiteSettings settings = SiteSettings.Create();
    settings.EntraTenantAcceptanceMode.ShouldBe(EntraTenantAcceptanceMode.Restricted);
    settings.EntraAcceptedTenantIds.ShouldBeEmpty();
    settings.EntraTenantAcceptanceInitialized.ShouldBeTrue();
    settings.AcceptsEntraTenant(Home).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Restricted_Should_Accept_Only_Listed_Tenants()
  {
    SiteSettings settings = SiteSettings.Create(entraAcceptedTenantIds: [Home]);
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
      entraAcceptedTenantIds: [Home]);
    settings.AcceptsEntraTenant(Other).ShouldBeTrue();
    settings.AcceptsEntraTenant(EntraTenantIds.Consumers).ShouldBeTrue();
    settings.AcceptsEntraTenant(Guid.Empty).ShouldBeFalse();
    settings.EntraAcceptedTenantIds.ShouldBe([Home]);
    return Task.CompletedTask;
  }

  public static Task Replace_Should_Collapse_Duplicates_Sort_And_Mark_Initialized()
  {
    SiteSettings settings = SiteSettings.Create(entraTenantAcceptanceInitialized: false);
    settings.ReplaceEntraTenantAcceptance(EntraTenantAcceptanceMode.Restricted, [Other, Home, Home]);
    settings.EntraTenantAcceptanceInitialized.ShouldBeTrue();
    EntraTenantIds.TryNormalize([Other, Home, Home], out IReadOnlyList<Guid> expected, out string? error)
      .ShouldBeTrue(error);
    settings.EntraAcceptedTenantIds.ShouldBe(expected);
    return Task.CompletedTask;
  }

  public static Task ReplacePolicy_Should_Keep_Tenant_Acceptance()
  {
    SiteSettings settings = SiteSettings.Create(
      entraTenantAcceptanceMode: EntraTenantAcceptanceMode.Any,
      entraAcceptedTenantIds: [Home]);
    settings.ReplacePolicy(true, true, PasskeyPromptMode.Required);
    settings.EntraTenantAcceptanceMode.ShouldBe(EntraTenantAcceptanceMode.Any);
    settings.EntraAcceptedTenantIds.ShouldBe([Home]);
    return Task.CompletedTask;
  }

  public static Task Empty_Guid_Should_Throw()
  {
    Should.Throw<ArgumentException>(() =>
      SiteSettings.Create(entraAcceptedTenantIds: [Guid.Empty]));
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
    string json = EntraTenantIds.ToJson([Home]);
    EntraTenantIds.FromJson(json).ShouldBe([Home]);
    EntraTenantIds.FromJson("not-json").ShouldBeEmpty();
    EntraTenantIds.FromJson(null).ShouldBeEmpty();
    return Task.CompletedTask;
  }
}
