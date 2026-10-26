#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008

// Co-located Jaribu: UpdateAssistantName validator and handler.
// Run standalone:  dotnet run source/container-apps/web/features/settings/update-assistant-name/update-assistant-name-tests.cs

#region Purpose
// Jaribu runfile: UpdateAssistantName validator (plain-text name, version) and handler
// (trimmed rename, 409, policy fields left untouched).
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
  using TimeWarp.Architecture.Features.Identity;
  using TimeWarp.Architecture.Features.Identity.Application;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.Settings.UpdateAssistantName;
  using UpdateHandler = TimeWarp.Architecture.Features.Settings.Application.UpdateAssistantName.Handler;

  [TestTag("Contracts")]
  public class UpdateAssistantNameCommand_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<UpdateAssistantNameCommand_Given_>();

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
    [Input("Ak\U000E0041ili")]
    public static Task Invalid_Assistant_Name_Should_Fail_Validation(string name)
    {
      ValidationResult result = new Validator().Validate(new Command { AssistantName = name, Version = 0 });
      result.IsValid.ShouldBeFalse(name);
      result.Errors.ShouldContain(error => error.ErrorMessage.StartsWith("Assistant name", System.StringComparison.Ordinal));
      return Task.CompletedTask;
    }

    public static Task Lone_Surrogate_Assistant_Name_Should_Fail_Validation()
    {
      ValidationResult result = new Validator().Validate(new Command { AssistantName = LoneSurrogateName(), Version = 0 });
      result.IsValid.ShouldBeFalse();
      result.Errors.ShouldContain(error => error.ErrorMessage.StartsWith("Assistant name", System.StringComparison.Ordinal));
      return Task.CompletedTask;
    }

    public static Task Null_Assistant_Name_Should_Fail_Validation()
    {
      ValidationResult result = new Validator().Validate(new Command { AssistantName = null!, Version = 0 });
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task Negative_Version_Should_Fail_Validation()
    {
      ValidationResult result = new Validator().Validate(new Command { AssistantName = AssistantNameRules.Default, Version = -1 });
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    [Input("Akili")]
    [Input("Zawadi")]
    [Input("  Zawadi  ")]
    [Input("Q&A Helper")]
    [Input("12345678901234567890123456789012")]
    [Input("Akili \U0001F916")]
    public static Task Valid_Assistant_Name_Should_Pass_Validation(string name)
    {
      ValidationResult result = new Validator().Validate(new Command { AssistantName = name, Version = 0 });
      result.IsValid.ShouldBeTrue(name);
      return Task.CompletedTask;
    }

    private static string LoneSurrogateName() => "Ak" + (char)0xD800 + "ili";
  }

  [TestTag("Application")]
  public class UpdateAssistantNameHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<UpdateAssistantNameHandler_Given_>();

    public static async Task Empty_Store_Should_Seed_Then_Rename_At_Version_0()
    {
      InMemorySiteSettingsStore store = new();
      IOptions<EntraAuthenticationOptions> configured = Options.Create(new EntraAuthenticationOptions());
      UpdateHandler handler = new(
        new SeedOnReadSiteSettingsStore(
          store,
          new SiteSettingsSeeder(
            store,
            configured,
            Options.Create(new SignInOptions { Methods = [SignInMethod.Passkey] }),
            NullLogger<SiteSettingsSeeder>.Instance),
          isDevelopment: false,
          NullLogger<SeedOnReadSiteSettingsStore>.Instance));
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command { AssistantName = "  Zawadi  ", Version = 0 },
        default);

      result.IsT0.ShouldBeTrue();
      result.AsT0.AssistantName.ShouldBe("Zawadi");
      result.AsT0.Version.ShouldBe(1);
      SiteSettings stored = (await store.GetAsync()).ShouldNotBeNull();
      stored.AssistantName.ShouldBe("Zawadi");
      stored.EntraSignInEnabled.ShouldBeFalse();
    }

    public static async Task Stale_Version_Should_409_And_Leave_The_Name()
    {
      InMemorySiteSettingsStore store = new();
      await store.AddAsync(SiteSettings.Create());
      SiteSettings? current = await store.GetAsync();
      current!.RenameAssistant("Akili");
      await store.UpdateAsync(current);

      UpdateHandler handler = new(store);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command { AssistantName = "Zawadi", Version = 0 },
        default);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(409);
      result.AsT1.Title.ShouldBe("Concurrency conflict");
      (await store.GetAsync())!.AssistantName.ShouldBe("Akili");
    }

    public static async Task Rename_Should_Store_The_Trimmed_Name_And_Leave_Policy()
    {
      InMemorySiteSettingsStore store = new();
      SiteSettings created = SiteSettings.Create();
      created.ReplacePolicy(true, true, PasskeyPromptMode.Required);
      await store.AddAsync(created);
      UpdateHandler handler = new(store);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command { AssistantName = "  Zawadi  ", Version = 0 },
        default);

      result.IsT0.ShouldBeTrue();
      result.AsT0.AssistantName.ShouldBe("Zawadi");
      result.AsT0.Version.ShouldBe(1);
      SiteSettings stored = (await store.GetAsync()).ShouldNotBeNull();
      stored.AssistantName.ShouldBe("Zawadi");
      stored.EntraSignInEnabled.ShouldBeTrue();
      stored.EntraAllowBootstrap.ShouldBeTrue();
      stored.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Required);
    }
  }
}
