#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Co-located Jaribu: UpdateSiteSettings contract + handler concurrency (task 219-006).
// Run standalone:  dotnet run source/container-apps/web/features/settings/update-site-settings/update-site-settings-tests.cs

#region Purpose
// Jaribu runfile: UpdateSiteSettings validator GUIDs, empty-store 503, 409, successful update.
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
  using FluentValidation.Results;
  using OneOf;
  using Shouldly;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.Settings.UpdateSiteSettings;
  using UpdateHandler = TimeWarp.Architecture.Features.Settings.Application.UpdateSiteSettings.Handler;

  [TestTag("Contracts")]
  public class UpdateSiteSettingsCommand_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<UpdateSiteSettingsCommand_Given_>();

    public static Task Invalid_Tenant_Should_Fail_Validation()
    {
      Command command = new()
      {
        EntraTrustedTenants = ["not-a-guid"],
        PasskeyPromptMode = PasskeyPromptMode.Soft,
        Version = 0
      };

      ValidationResult result = new Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task Guid_Tenants_Should_Pass_Validation()
    {
      Command command = new()
      {
        EntraSignInEnabled = true,
        EntraAllowBootstrap = true,
        EntraTrustedTenants = ["30f3971f-4719-4f20-9b6f-88916e0b95bd"],
        PasskeyPromptMode = PasskeyPromptMode.Required,
        Version = 0
      };

      ValidationResult result = new Validator().Validate(command);
      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }
  }

  [TestTag("Application")]
  public class UpdateSiteSettingsHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<UpdateSiteSettingsHandler_Given_>();

    public static async Task Empty_Store_Should_Return_Not_Initialized_Without_Insert()
    {
      InMemorySiteSettingsStore store = new();
      UpdateHandler handler = new(store);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command
        {
          EntraSignInEnabled = true,
          EntraAllowBootstrap = true,
          EntraTrustedTenants = [],
          PasskeyPromptMode = PasskeyPromptMode.Required,
          Version = 0
        },
        default);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(503);
      result.AsT1.Title.ShouldBe("Site settings not initialized");
      SiteSettings? stored = await store.GetAsync();
      stored.ShouldBeNull();
    }

    public static async Task Stale_Version_Should_409()
    {
      InMemorySiteSettingsStore store = new();
      await store.AddAsync(SiteSettings.Create());
      SiteSettings? current = await store.GetAsync();
      current!.ReplacePolicy(true, false, [], PasskeyPromptMode.Soft);
      await store.UpdateAsync(current);

      UpdateHandler handler = new(store);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command
        {
          EntraSignInEnabled = false,
          EntraAllowBootstrap = true,
          EntraTrustedTenants = [],
          PasskeyPromptMode = PasskeyPromptMode.Required,
          Version = 0
        },
        default);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(409);
      result.AsT1.Title.ShouldBe("Concurrency conflict");
      SiteSettings? stored = await store.GetAsync();
      stored!.EntraSignInEnabled.ShouldBeTrue();
    }

    public static async Task Matching_Version_Should_Persist_And_Advance()
    {
      InMemorySiteSettingsStore store = new();
      await store.AddAsync(SiteSettings.Create());
      Guid tenant = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");
      UpdateHandler handler = new(store);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command
        {
          EntraSignInEnabled = true,
          EntraAllowBootstrap = true,
          EntraTrustedTenants = [tenant.ToString("D")],
          PasskeyPromptMode = PasskeyPromptMode.Required,
          Version = 0
        },
        default);

      result.IsT0.ShouldBeTrue();
      result.AsT0.EntraSignInEnabled.ShouldBeTrue();
      result.AsT0.Version.ShouldBe(1);
      result.AsT0.EntraTrustedTenants.ShouldBe([tenant.ToString("D")]);
    }
  }
}
