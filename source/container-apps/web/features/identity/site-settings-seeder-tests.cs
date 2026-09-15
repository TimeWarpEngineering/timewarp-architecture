#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:package Microsoft.Extensions.Logging.Abstractions
#:package Microsoft.Extensions.Options
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Host-free SiteSettingsSeeder coverage (task 219-006).
// Run standalone:  dotnet run source/container-apps/web/features/identity/site-settings-seeder-tests.cs

#region Purpose
// Jaribu runfile: seed-once from configuration; empty Get does not block config seed.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Identity.SiteSettingsSeederTests
{

  using System;
  using System.Threading.Tasks;
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
      // Pretend GetSiteSettings ran against an empty store (no insert).
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

    private static SiteSettingsSeeder Create(
      ISiteSettingsStore store,
      bool enabled,
      bool allowBootstrap,
      Guid tenant)
    {
      IOptions<EntraAuthenticationOptions> options = Options.Create(
        new EntraAuthenticationOptions
        {
          Enabled = enabled,
          AllowBootstrap = allowBootstrap,
          TrustedTenants = [tenant.ToString("D")]
        });
      return new SiteSettingsSeeder(store, options, NullLogger<SiteSettingsSeeder>.Instance);
    }
  }
}
