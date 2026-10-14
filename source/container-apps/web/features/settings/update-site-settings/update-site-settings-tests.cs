#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Logging.Abstractions
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008

// Co-located Jaribu: UpdateSiteSettings contract + handler concurrency (task 219-006 / 227).
// Run standalone:  dotnet run source/container-apps/web/features/settings/update-site-settings/update-site-settings-tests.cs

#region Purpose
// Jaribu runfile: UpdateSiteSettings validator (passkey mode, assistant name), emptied store re-seeds at
// Version 0, 409, successful update, assistant rename stored trimmed.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Settings
{

  using System.Threading.Tasks;
  using FluentValidation.Results;
  using Microsoft.Extensions.Logging.Abstractions;
  using Microsoft.Extensions.Options;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Features.Identity.Application;
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

    public static Task Undefined_Passkey_Mode_Should_Fail_Validation()
    {
      Command command = new()
      {
        PasskeyPromptMode = (PasskeyPromptMode)99,
        AssistantName = AssistantNameRules.Default,
        Version = 0
      };

      ValidationResult result = new Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task Defined_Passkey_Mode_Should_Pass_Validation()
    {
      Command command = new()
      {
        EntraSignInEnabled = true,
        EntraAllowBootstrap = true,
        PasskeyPromptMode = PasskeyPromptMode.Required,
        AssistantName = AssistantNameRules.Default,
        Version = 0
      };

      ValidationResult result = new Validator().Validate(command);
      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    // Task 361: AssistantName is required, and trimmed it is 1-32 plain-text characters.
    [Input("")]
    [Input("   ")]
    [Input("123456789012345678901234567890123")]
    [Input("<b>Akili</b>")]
    [Input("Ak<ili")]
    [Input("Ak\u0007ili")]
    [Input("Akili\nZawadi")]
    [Input("Ak\u2028ili")]
    [Input("Ak\u202Eili")]
    [Input("Ak\u200Bili")]
    public static Task Invalid_Assistant_Name_Should_Fail_Validation(string name)
    {
      ValidationResult result = new Validator().Validate(ValidCommand(name));
      result.IsValid.ShouldBeFalse(name);
      result.Errors.ShouldContain(error => error.ErrorMessage.StartsWith("Assistant name", System.StringComparison.Ordinal));
      return Task.CompletedTask;
    }

    public static Task Null_Assistant_Name_Should_Fail_Validation()
    {
      ValidationResult result = new Validator().Validate(ValidCommand(null!));
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    [Input("Akili")]
    [Input("Zawadi")]
    [Input("  Zawadi  ")]
    [Input("Q&A Helper")]
    [Input("12345678901234567890123456789012")]
    public static Task Valid_Assistant_Name_Should_Pass_Validation(string name)
    {
      ValidationResult result = new Validator().Validate(ValidCommand(name));
      result.IsValid.ShouldBeTrue(name);
      return Task.CompletedTask;
    }

    private static Command ValidCommand(string assistantName) =>
      new()
      {
        PasskeyPromptMode = PasskeyPromptMode.Soft,
        AssistantName = assistantName,
        Version = 0
      };
  }

  [TestTag("Application")]
  public class UpdateSiteSettingsHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<UpdateSiteSettingsHandler_Given_>();

    public static async Task Empty_Store_Should_Seed_Then_Update_At_Version_0()
    {
      InMemorySiteSettingsStore store = new();
      IOptions<EntraAuthenticationOptions> configured = Options.Create(new EntraAuthenticationOptions());
      UpdateHandler handler = new(
        new SeedOnReadSiteSettingsStore(
          store,
          new SiteSettingsSeeder(store, configured, NullLogger<SiteSettingsSeeder>.Instance),
          isDevelopment: false,
          NullLogger<SeedOnReadSiteSettingsStore>.Instance));
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command
        {
          EntraSignInEnabled = true,
          EntraAllowBootstrap = true,
          PasskeyPromptMode = PasskeyPromptMode.Required,
          AssistantName = AssistantNameRules.Default,
          Version = 0
        },
        default);

      result.IsT0.ShouldBeTrue();
      result.AsT0.EntraSignInEnabled.ShouldBeTrue();
      result.AsT0.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Required);
      result.AsT0.Version.ShouldBe(1);
      SiteSettings? stored = await store.GetAsync();
      stored.ShouldNotBeNull();
      stored.Version.ShouldBe(1);
    }

    public static async Task Stale_Version_Should_409()
    {
      InMemorySiteSettingsStore store = new();
      await store.AddAsync(SiteSettings.Create());
      SiteSettings? current = await store.GetAsync();
      current!.ReplacePolicy(true, false, PasskeyPromptMode.Soft);
      await store.UpdateAsync(current);

      UpdateHandler handler = new(store);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command
        {
          EntraSignInEnabled = false,
          EntraAllowBootstrap = true,
          PasskeyPromptMode = PasskeyPromptMode.Required,
          AssistantName = AssistantNameRules.Default,
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
      UpdateHandler handler = new(store);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command
        {
          EntraSignInEnabled = true,
          EntraAllowBootstrap = true,
          PasskeyPromptMode = PasskeyPromptMode.Required,
          AssistantName = AssistantNameRules.Default,
          Version = 0
        },
        default);

      result.IsT0.ShouldBeTrue();
      result.AsT0.EntraSignInEnabled.ShouldBeTrue();
      result.AsT0.EntraAllowBootstrap.ShouldBeTrue();
      result.AsT0.Version.ShouldBe(1);
    }

    public static async Task Rename_Should_Store_The_Trimmed_Name_And_Echo_It()
    {
      InMemorySiteSettingsStore store = new();
      await store.AddAsync(SiteSettings.Create());
      UpdateHandler handler = new(store);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command
        {
          PasskeyPromptMode = PasskeyPromptMode.Soft,
          AssistantName = "  Zawadi  ",
          Version = 0
        },
        default);

      result.IsT0.ShouldBeTrue();
      result.AsT0.AssistantName.ShouldBe("Zawadi");
      (await store.GetAsync())!.AssistantName.ShouldBe("Zawadi");
    }
  }
}
