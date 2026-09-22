#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Host-free SiteSettingsConfigurationDrift coverage (task 225 / 227).
// Run standalone:  dotnet run source/container-apps/web/features/settings/site-settings-configuration-drift-tests.cs

#region Purpose
// Jaribu runfile: app-registration tenant label formatting.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Settings
{

  using System;
  using System.Threading.Tasks;
  using Shouldly;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Contracts")]
  public class SiteSettingsConfigurationDrift_Given_
  {
    private static readonly Guid TimeWarp = Guid.Parse("a16bcaef-ea01-44ad-be12-249a17658692");

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<SiteSettingsConfigurationDrift_Given_>();

    public static Task Format_Should_Include_Guid_With_Name_And_Domain()
    {
      SiteSettingsConfigurationDrift.FormatTenantLabel(
        "TimeWarp Enterprises LLC",
        "timewarp.engineering",
        TimeWarp.ToString("D"))
        .ShouldBe($"TimeWarp Enterprises LLC (timewarp.engineering) — {TimeWarp:D}");
      SiteSettingsConfigurationDrift.FormatTenantLabel(
        "TimeWarp Enterprises LLC",
        null,
        TimeWarp.ToString("D"))
        .ShouldBe($"TimeWarp Enterprises LLC — {TimeWarp:D}");
      SiteSettingsConfigurationDrift.FormatTenantLabel(
        null,
        "timewarp.engineering",
        TimeWarp.ToString("D"))
        .ShouldBe($"timewarp.engineering — {TimeWarp:D}");
      SiteSettingsConfigurationDrift.FormatTenantLabel(null, null, TimeWarp.ToString("D"))
        .ShouldBe(TimeWarp.ToString("D"));
      SiteSettingsConfigurationDrift.FormatTenantLabel(null, null, "organizations")
        .ShouldBe("organizations");
      return Task.CompletedTask;
    }

    public static Task Remediation_Should_Name_Admin_Page_And_Cli()
    {
      SiteSettingsConfigurationDrift.AdminPageRoute.ShouldBe("/Admin/Authentication");
      SiteSettingsConfigurationDrift.Remediation.ShouldContain("/Admin/Authentication");
      SiteSettingsConfigurationDrift.Remediation.ShouldContain("dev entra reseed");
      return Task.CompletedTask;
    }
  }
}
