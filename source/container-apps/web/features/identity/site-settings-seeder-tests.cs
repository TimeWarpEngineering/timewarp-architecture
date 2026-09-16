#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:package Microsoft.Extensions.Logging.Abstractions
#:package Microsoft.Extensions.Options
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Host-free SiteSettingsSeeder coverage (task 219-006 / 225).
// Run standalone:  dotnet run source/container-apps/web/features/identity/site-settings-seeder-tests.cs

#region Purpose
// Jaribu runfile: seed-once from configuration; drift warnings; Development-only reseed.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Identity.SiteSettingsSeederTests
{

  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using Microsoft.Extensions.Logging;
  using Microsoft.Extensions.Logging.Abstractions;
  using Microsoft.Extensions.Options;
  using Shouldly;
  using TimeWarp.Architecture.Features.Identity.Application;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Application")]
  public class SiteSettingsSeeder_Given_
  {
    private static readonly Guid Tenant = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");
    private static readonly Guid OtherTenant = Guid.Parse("a16bcaef-ea01-44ad-be12-249a17658692");

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<SiteSettingsSeeder_Given_>();

    public static async Task Empty_Store_Should_Seed_From_Configuration()
    {
      InMemorySiteSettingsStore store = new();
      SiteSettingsSeeder seeder = Create(store, enabled: true, allowBootstrap: true, Tenant);
      SiteSettings seeded = await seeder.GetOrSeedAsync();
      seeded.EntraSignInEnabled.ShouldBeTrue();
      seeded.EntraAllowBootstrap.ShouldBeTrue();
      seeded.IsTrustedTenant(Tenant).ShouldBeTrue();
      seeded.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Soft);
    }

    public static async Task Empty_Get_Without_Insert_Should_Still_Allow_Config_Seed()
    {
      InMemorySiteSettingsStore store = new();
      SiteSettings? afterPretendGet = await store.GetAsync();
      afterPretendGet.ShouldBeNull();

      SiteSettingsSeeder seeder = Create(store, enabled: true, allowBootstrap: true, Tenant);
      SiteSettings seeded = await seeder.GetOrSeedAsync();
      seeded.EntraSignInEnabled.ShouldBeTrue();
      seeded.EntraAllowBootstrap.ShouldBeTrue();
      seeded.IsTrustedTenant(Tenant).ShouldBeTrue();
    }

    public static async Task Second_Call_Should_Not_Overwrite()
    {
      InMemorySiteSettingsStore store = new();
      SiteSettingsSeeder seeder = Create(store, enabled: true, allowBootstrap: true, Tenant);
      SiteSettings first = await seeder.GetOrSeedAsync();
      first.ReplacePolicy(false, false, [], PasskeyPromptMode.Required);
      await store.UpdateAsync(first);

      SiteSettings second = await seeder.GetOrSeedAsync();
      second.EntraSignInEnabled.ShouldBeFalse();
      second.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Required);
      second.Version.ShouldBe(1);
    }

    public static async Task Config_Tenant_Missing_From_Trusted_List_Should_Warn()
    {
      InMemorySiteSettingsStore store = new();
      SiteSettingsSeeder first = Create(store, enabled: true, allowBootstrap: true, Tenant);
      await first.GetOrSeedAsync();

      CapturingLogger logger = new();
      SiteSettingsSeeder drifted = Create(
        store,
        enabled: true,
        allowBootstrap: true,
        OtherTenant,
        logger: logger);
      await drifted.GetOrSeedAsync();

      logger.Messages.ShouldContain(message =>
        message.Contains(OtherTenant.ToString("D"), StringComparison.Ordinal)
        && message.Contains("/Admin/Authentication", StringComparison.Ordinal)
        && message.Contains("dev entra reseed", StringComparison.Ordinal));
      logger.Levels.ShouldContain(LogLevel.Warning);
    }

    public static async Task Reseed_Flag_In_Development_Should_Overwrite_Policy_Fields()
    {
      InMemorySiteSettingsStore store = new();
      SiteSettingsSeeder first = Create(store, enabled: true, allowBootstrap: true, Tenant);
      SiteSettings seeded = await first.GetOrSeedAsync();
      seeded.ReplacePolicy(false, false, [], PasskeyPromptMode.Required);
      await store.UpdateAsync(seeded);

      CapturingLogger logger = new();
      SiteSettingsSeeder reseed = Create(
        store,
        enabled: true,
        allowBootstrap: true,
        OtherTenant,
        reseedSiteSettings: true,
        logger: logger);
      SiteSettings after = await reseed.GetOrSeedAsync(isDevelopment: true);

      after.EntraSignInEnabled.ShouldBeTrue();
      after.EntraAllowBootstrap.ShouldBeTrue();
      after.IsTrustedTenant(OtherTenant).ShouldBeTrue();
      after.IsTrustedTenant(Tenant).ShouldBeFalse();
      after.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Required);
      logger.Messages.ShouldContain(message =>
        message.Contains("Overwrote EntraSignInEnabled", StringComparison.Ordinal));
    }

    public static async Task Reseed_Flag_Outside_Development_Should_Not_Overwrite()
    {
      InMemorySiteSettingsStore store = new();
      SiteSettingsSeeder first = Create(store, enabled: true, allowBootstrap: true, Tenant);
      SiteSettings seeded = await first.GetOrSeedAsync();
      seeded.ReplacePolicy(false, false, [], PasskeyPromptMode.Required);
      await store.UpdateAsync(seeded);

      CapturingLogger logger = new();
      SiteSettingsSeeder reseed = Create(
        store,
        enabled: true,
        allowBootstrap: true,
        OtherTenant,
        reseedSiteSettings: true,
        logger: logger);
      SiteSettings after = await reseed.GetOrSeedAsync(isDevelopment: false);

      after.EntraSignInEnabled.ShouldBeFalse();
      after.EntraAllowBootstrap.ShouldBeFalse();
      after.IsTrustedTenant(Tenant).ShouldBeFalse();
      after.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Required);
      logger.Messages.ShouldContain(message =>
        message.Contains("honoured only in Development", StringComparison.Ordinal));
    }

    private static SiteSettingsSeeder Create(
      ISiteSettingsStore store,
      bool enabled,
      bool allowBootstrap,
      Guid tenant,
      bool reseedSiteSettings = false,
      ILogger<SiteSettingsSeeder>? logger = null)
    {
      IOptions<EntraAuthenticationOptions> options = Options.Create(
        new EntraAuthenticationOptions
        {
          Enabled = enabled,
          AllowBootstrap = allowBootstrap,
          TenantId = tenant.ToString("D"),
          TrustedTenants = [tenant.ToString("D")],
          ReseedSiteSettings = reseedSiteSettings
        });
      return new SiteSettingsSeeder(
        store,
        options,
        logger ?? NullLogger<SiteSettingsSeeder>.Instance);
    }

    private sealed class CapturingLogger : ILogger<SiteSettingsSeeder>
    {
      public List<string> Messages { get; } = [];
      public List<LogLevel> Levels { get; } = [];

      public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

      public bool IsEnabled(LogLevel logLevel) => true;

      public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
      {
        Levels.Add(logLevel);
        Messages.Add(formatter(state, exception));
      }
    }
  }
}
