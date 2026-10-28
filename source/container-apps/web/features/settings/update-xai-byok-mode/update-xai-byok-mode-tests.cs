#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008

// Co-located Jaribu: UpdateXaiByokMode validator, serialization, and handler.
// Run standalone:  dotnet run source/container-apps/web/features/settings/update-xai-byok-mode/update-xai-byok-mode-tests.cs

#region Purpose
// Jaribu runfile: UpdateXaiByokMode validator (defined mode, version), JSON round-trip of the
// string enum, and handler (mode write, 409, policy fields left untouched).
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Settings
{

  using System.Text.Json;
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
  using static TimeWarp.Architecture.Features.Settings.UpdateXaiByokMode;
  using UpdateHandler = TimeWarp.Architecture.Features.Settings.Application.UpdateXaiByokMode.Handler;

  [TestTag("Contracts")]
  public class UpdateXaiByokModeCommand_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<UpdateXaiByokModeCommand_Given_>();

    public static Task Undefined_Mode_Should_Fail_Validation()
    {
      ValidationResult result = new Validator().Validate(new Command { XaiByokMode = (XaiByokMode)9, Version = 0 });
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task Negative_Version_Should_Fail_Validation()
    {
      ValidationResult result = new Validator().Validate(new Command { XaiByokMode = XaiByokMode.Allow, Version = -1 });
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    [Input(XaiByokMode.Allow)]
    [Input(XaiByokMode.Required)]
    [Input(XaiByokMode.Off)]
    public static Task Defined_Mode_Should_Pass_Validation(XaiByokMode mode)
    {
      ValidationResult result = new Validator().Validate(new Command { XaiByokMode = mode, Version = 0 });
      result.IsValid.ShouldBeTrue(mode.ToString());
      return Task.CompletedTask;
    }

    public static Task Command_And_Response_Should_RoundTrip_The_Mode_As_A_String()
    {
      Command command = new() { XaiByokMode = XaiByokMode.Required, Version = 4 };
      string commandJson = JsonSerializer.Serialize(command, ContractSerializationDefaults.Options);
      commandJson.ShouldContain("\"xaiByokMode\":\"Required\"");
      commandJson.ShouldNotContain("\"xaiByokMode\":1");
      Command? parsedCommand = JsonSerializer.Deserialize<Command>(commandJson, ContractSerializationDefaults.Options);
      parsedCommand.ShouldNotBeNull();
      parsedCommand.XaiByokMode.ShouldBe(XaiByokMode.Required);
      parsedCommand.Version.ShouldBe(4);

      Response response = new(XaiByokMode.Off, 2);
      string responseJson = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      responseJson.ShouldContain("\"xaiByokMode\":\"Off\"");
      Response? parsedResponse = JsonSerializer.Deserialize<Response>(responseJson, ContractSerializationDefaults.Options);
      parsedResponse.ShouldNotBeNull();
      parsedResponse.XaiByokMode.ShouldBe(XaiByokMode.Off);
      parsedResponse.Version.ShouldBe(2);

      Response mock = GetMockResponseFactory()(new Command());
      mock.XaiByokMode.ShouldBe(XaiByokMode.Allow);
      mock.Version.ShouldBe(1);
      return Task.CompletedTask;
    }
  }

  [TestTag("Application")]
  public class UpdateXaiByokModeHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<UpdateXaiByokModeHandler_Given_>();

    public static async Task Empty_Store_Should_Seed_Then_Set_The_Mode_At_Version_0()
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
        new Command { XaiByokMode = XaiByokMode.Required, Version = 0 },
        default);

      result.IsT0.ShouldBeTrue();
      result.AsT0.XaiByokMode.ShouldBe(XaiByokMode.Required);
      result.AsT0.Version.ShouldBe(1);
      SiteSettings stored = (await store.GetAsync()).ShouldNotBeNull();
      stored.XaiByokMode.ShouldBe(XaiByokMode.Required);
      stored.EntraSignInEnabled.ShouldBeFalse();
    }

    public static async Task Unmigrated_Store_Should_503()
    {
      UpdateHandler handler = new(new InMemorySiteSettingsStore());
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command { XaiByokMode = XaiByokMode.Off, Version = 0 },
        default);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(503);
    }

    public static async Task Stale_Version_Should_409_And_Leave_The_Mode()
    {
      InMemorySiteSettingsStore store = new();
      SiteSettings created = SiteSettings.Create();
      created.SetXaiByokMode(XaiByokMode.Required);
      await store.AddAsync(created);
      SiteSettings? current = await store.GetAsync();
      await store.UpdateAsync(current!);

      UpdateHandler handler = new(store);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command { XaiByokMode = XaiByokMode.Off, Version = 0 },
        default);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(409);
      result.AsT1.Title.ShouldBe("Concurrency conflict");
      (await store.GetAsync())!.XaiByokMode.ShouldBe(XaiByokMode.Required);
    }

    public static async Task Update_Should_Store_The_Mode_And_Leave_Policy_And_The_Name()
    {
      InMemorySiteSettingsStore store = new();
      SiteSettings created = SiteSettings.Create();
      created.ReplacePolicy(true, true, PasskeyPromptMode.Required);
      created.RenameAssistant("Zawadi");
      await store.AddAsync(created);
      UpdateHandler handler = new(store);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(
        new Command { XaiByokMode = XaiByokMode.Off, Version = 0 },
        default);

      result.IsT0.ShouldBeTrue();
      result.AsT0.XaiByokMode.ShouldBe(XaiByokMode.Off);
      result.AsT0.Version.ShouldBe(1);
      SiteSettings stored = (await store.GetAsync()).ShouldNotBeNull();
      stored.XaiByokMode.ShouldBe(XaiByokMode.Off);
      stored.AssistantName.ShouldBe("Zawadi");
      stored.EntraSignInEnabled.ShouldBeTrue();
      stored.EntraAllowBootstrap.ShouldBeTrue();
      stored.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Required);
    }
  }
}
