#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Logging.Abstractions
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Co-located Jaribu: GetSiteSettings contract + handler (task 219-006 / 227).
// Run standalone:  dotnet run source/container-apps/web/features/settings/get-site-settings/get-site-settings-tests.cs

#region Purpose
// Jaribu runfile: GetSiteSettings round-trip, mock factory, emptied store re-seeds, unmigrated table 503.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Settings
{

  using System;
  using System.Text.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using Microsoft.Extensions.Logging.Abstractions;
  using Microsoft.Extensions.Options;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Features.Identity.Application;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.Settings.GetSiteSettings;
  using GetHandler = TimeWarp.Architecture.Features.Settings.Application.GetSiteSettings.Handler;

  [TestTag("Contracts")]
  public class GetSiteSettingsResponse_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetSiteSettingsResponse_Given_>();

    public static Task ValidResponse_Should_RoundTripThroughJson()
    {
      Guid configured = Guid.Parse("a16bcaef-ea01-44ad-be12-249a17658692");
      Response response = new(
        entraSignInEnabled: true,
        entraAllowBootstrap: true,
        passkeyPromptMode: PasskeyPromptMode.Required,
        version: 3,
        configurationTenantId: configured.ToString("D"),
        configurationTenantDisplayName: "TimeWarp Enterprises LLC",
        configurationTenantDomain: "timewarp.engineering",
        configurationEnabled: true,
        configurationAllowBootstrap: false);

      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      Response? parsed = JsonSerializer.Deserialize<Response>(json, ContractSerializationDefaults.Options);

      parsed.ShouldNotBeNull();
      parsed.EntraSignInEnabled.ShouldBeTrue();
      parsed.EntraAllowBootstrap.ShouldBeTrue();
      parsed.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Required);
      parsed.Version.ShouldBe(3);
      parsed.ConfigurationTenantId.ShouldBe(configured.ToString("D"));
      parsed.ConfigurationTenantDisplayName.ShouldBe("TimeWarp Enterprises LLC");
      parsed.ConfigurationTenantDomain.ShouldBe("timewarp.engineering");
      parsed.ConfigurationEnabled.ShouldBeTrue();
      parsed.ConfigurationAllowBootstrap.ShouldBeFalse();
      json.ShouldContain("passkeyPromptMode");
      json.ShouldContain("configurationTenantId");
      json.ShouldNotContain("entraTrustedTenants");
      return Task.CompletedTask;
    }

    public static Task MockFactory_Should_ReturnDisabledDefaults()
    {
      Response response = GetMockResponseFactory()(new Query());
      response.EntraSignInEnabled.ShouldBeFalse();
      response.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Soft);
      return Task.CompletedTask;
    }
  }

  [TestTag("Application")]
  public class GetSiteSettingsHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetSiteSettingsHandler_Given_>();

    public static async Task Emptied_Store_Should_Return_Seeded_Values()
    {
      EmptiableSiteSettingsStore inner = new();
      await inner.AddAsync(SiteSettings.Create(false, false, PasskeyPromptMode.Required));
      EntraAuthenticationOptions configured = new() { Enabled = true, AllowBootstrap = true };
      GetHandler handler = CreateHandler(SeedOnRead(inner, configured), configured);

      inner.Clear();
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(new Query(), default);

      result.IsT0.ShouldBeTrue();
      result.AsT0.EntraSignInEnabled.ShouldBeTrue();
      result.AsT0.EntraAllowBootstrap.ShouldBeTrue();
      result.AsT0.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Soft);
      result.AsT0.Version.ShouldBe(0);
      (await inner.GetAsync()).ShouldNotBeNull();
    }

    public static async Task Unmigrated_Table_Should_Return_Unavailable()
    {
      EmptiableSiteSettingsStore inner = new() { ThrowUndefinedTable = true };
      EntraAuthenticationOptions configured = new();
      GetHandler handler = CreateHandler(SeedOnRead(inner, configured), configured);

      OneOf<Response, SharedProblemDetails> result = await handler.Handle(new Query(), default);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(503);
      result.AsT1.Title.ShouldBe("Site settings unavailable");
    }

    public static async Task Snapshot_Should_Include_Configuration_Tenant()
    {
      Guid configured = Guid.Parse("a16bcaef-ea01-44ad-be12-249a17658692");
      InMemorySiteSettingsStore store = new();
      await store.AddAsync(SiteSettings.Create(true, true, PasskeyPromptMode.Soft));
      GetHandler handler = CreateHandler(
        store,
        new EntraAuthenticationOptions
        {
          Enabled = true,
          AllowBootstrap = false,
          TenantId = configured.ToString("D"),
          TenantDisplayName = "TimeWarp Enterprises LLC",
          TenantDomain = "timewarp.engineering"
        });
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(new Query(), default);
      result.IsT0.ShouldBeTrue();
      result.AsT0.EntraSignInEnabled.ShouldBeTrue();
      result.AsT0.ConfigurationTenantId.ShouldBe(configured.ToString("D"));
      result.AsT0.ConfigurationTenantDisplayName.ShouldBe("TimeWarp Enterprises LLC");
      result.AsT0.ConfigurationTenantDomain.ShouldBe("timewarp.engineering");
      result.AsT0.ConfigurationEnabled.ShouldBeTrue();
      result.AsT0.ConfigurationAllowBootstrap.ShouldBeFalse();
    }

    private static GetHandler CreateHandler(
      ISiteSettingsStore store,
      EntraAuthenticationOptions? options = null) =>
      new(store, Options.Create(options ?? new EntraAuthenticationOptions()));

    private static SeedOnReadSiteSettingsStore SeedOnRead(
      ISiteSettingsStore inner,
      EntraAuthenticationOptions configured) =>
      new(
        inner,
        new SiteSettingsSeeder(inner, Options.Create(configured), NullLogger<SiteSettingsSeeder>.Instance),
        isDevelopment: false,
        NullLogger<SeedOnReadSiteSettingsStore>.Instance);
  }

  // In-memory store a test can empty (the `dev db reset` shape) or fail like an unmigrated table.
  internal sealed class EmptiableSiteSettingsStore : ISiteSettingsStore
  {
    private InMemorySiteSettingsStore Current = new();

    public bool ThrowUndefinedTable { get; set; }

    public void Clear() => Current = new InMemorySiteSettingsStore();

    public Task<SiteSettings?> GetAsync(CancellationToken cancellationToken = default) =>
      ThrowUndefinedTable
        ? throw new InvalidOperationException("42P01: relation \"identity.site_settings\" does not exist")
        : Current.GetAsync(cancellationToken);

    public Task AddAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default) =>
      Current.AddAsync(siteSettings, cancellationToken);

    public Task UpdateAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default) =>
      Current.UpdateAsync(siteSettings, cancellationToken);
  }
}
