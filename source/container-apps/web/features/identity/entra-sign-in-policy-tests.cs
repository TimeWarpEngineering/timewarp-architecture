#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:package Microsoft.Extensions.Options
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Host-free IEntraSignInPolicy coverage (task 219-006 / 227).
// Run standalone:  dotnet run source/container-apps/web/features/identity/entra-sign-in-policy-tests.cs

#region Purpose
// Jaribu runfile: disabled / bootstrap disabled / untrusted tenant / allowed / organizations.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Identity.EntraSignInPolicyTests
{

  using System;
  using System.Threading.Tasks;
  using Microsoft.Extensions.Options;
  using Shouldly;
  using TimeWarp.Architecture.Features.Identity.Application;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Application")]
  public class SiteSettingsEntraSignInPolicy_Given_
  {
    private static readonly Guid Trusted = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");
    private static readonly Guid Untrusted = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<SiteSettingsEntraSignInPolicy_Given_>();

    public static async Task Challenge_Disabled_Should_Refuse_Sign_In_Disabled()
    {
      SiteSettingsEntraSignInPolicy policy = await PolicyAsync(enabled: false, allowBootstrap: true, Trusted);
      EntraSignInDecision decision = await policy.EvaluateAsync(EntraSignInMode.Challenge, null);
      decision.Allowed.ShouldBeFalse();
      decision.Problem.ShouldNotBeNull();
      decision.Problem!.Title.ShouldBe("Sign-in disabled");
      decision.Problem.Status.ShouldBe(403);
    }

    public static async Task Bootstrap_Disabled_Should_Refuse()
    {
      SiteSettingsEntraSignInPolicy policy = await PolicyAsync(enabled: true, allowBootstrap: false, Trusted);
      EntraSignInDecision decision = await policy.EvaluateAsync(EntraSignInMode.BootstrapCreate, Trusted);
      decision.Allowed.ShouldBeFalse();
      decision.Problem.ShouldNotBeNull();
      decision.Problem!.Title.ShouldBe("Bootstrap not allowed");
    }

    public static async Task Untrusted_Tenant_Should_Refuse()
    {
      SiteSettingsEntraSignInPolicy policy = await PolicyAsync(enabled: true, allowBootstrap: true, Trusted);
      EntraSignInDecision decision = await policy.EvaluateAsync(EntraSignInMode.BootstrapCreate, Untrusted);
      decision.Allowed.ShouldBeFalse();
      decision.Problem.ShouldNotBeNull();
      decision.Problem!.Title.ShouldBe("Untrusted tenant");
    }

    public static async Task Organizations_TenantId_Should_Refuse_Bootstrap()
    {
      SiteSettingsEntraSignInPolicy policy = await PolicyAsync(
        enabled: true,
        allowBootstrap: true,
        configuredTenantId: "organizations");
      EntraSignInDecision decision = await policy.EvaluateAsync(EntraSignInMode.BootstrapCreate, Trusted);
      decision.Allowed.ShouldBeFalse();
      decision.Problem!.Title.ShouldBe("Untrusted tenant");
    }

    public static async Task Allowed_Bootstrap_Should_Allow()
    {
      SiteSettingsEntraSignInPolicy policy = await PolicyAsync(enabled: true, allowBootstrap: true, Trusted);
      EntraSignInDecision decision = await policy.EvaluateAsync(EntraSignInMode.BootstrapCreate, Trusted);
      decision.Allowed.ShouldBeTrue();
      decision.Problem.ShouldBeNull();
    }

    public static async Task Sync_Hit_Should_Allow_When_Trusted_Even_If_Bootstrap_Disabled()
    {
      SiteSettingsEntraSignInPolicy policy = await PolicyAsync(enabled: true, allowBootstrap: false, Trusted);
      EntraSignInDecision decision = await policy.EvaluateAsync(EntraSignInMode.SyncHit, Trusted);
      decision.Allowed.ShouldBeTrue();
    }

    public static async Task Link_Foreign_Tid_Should_Refuse()
    {
      SiteSettingsEntraSignInPolicy policy = await PolicyAsync(enabled: true, allowBootstrap: true, Trusted);
      EntraSignInDecision decision = await policy.EvaluateAsync(EntraSignInMode.Link, Untrusted);
      decision.Allowed.ShouldBeFalse();
      decision.Problem!.Title.ShouldBe("Untrusted tenant");
    }

    public static async Task Challenge_Enabled_Should_Allow_Without_Tenant()
    {
      SiteSettingsEntraSignInPolicy policy = await PolicyAsync(enabled: true, allowBootstrap: false, Trusted);
      EntraSignInDecision decision = await policy.EvaluateAsync(EntraSignInMode.Challenge, null);
      decision.Allowed.ShouldBeTrue();
    }

    private static async Task<SiteSettingsEntraSignInPolicy> PolicyAsync(
      bool enabled,
      bool allowBootstrap,
      Guid configuredTenant)
    {
      return await PolicyAsync(enabled, allowBootstrap, configuredTenant.ToString("D"));
    }

    private static async Task<SiteSettingsEntraSignInPolicy> PolicyAsync(
      bool enabled,
      bool allowBootstrap,
      string configuredTenantId)
    {
      InMemorySiteSettingsStore store = new();
      await store.AddAsync(
        SiteSettings.Create(enabled, allowBootstrap, PasskeyPromptMode.Soft));
      IOptions<EntraAuthenticationOptions> options = Options.Create(
        new EntraAuthenticationOptions { TenantId = configuredTenantId });
      return new SiteSettingsEntraSignInPolicy(store, options);
    }
  }
}
