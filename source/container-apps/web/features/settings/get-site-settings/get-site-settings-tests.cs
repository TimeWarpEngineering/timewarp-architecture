#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Co-located Jaribu: GetSiteSettings contract + handler (task 219-006).
// Run standalone:  dotnet run source/container-apps/web/features/settings/get-site-settings/get-site-settings-tests.cs

#region Purpose
// Jaribu runfile: GetSiteSettings round-trip, mock factory, empty-store not-initialized.
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
  using System.Threading.Tasks;
  using OneOf;
  using Shouldly;
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
      Guid tenant = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");
      Response response = new(
        entraSignInEnabled: true,
        entraAllowBootstrap: true,
        entraTrustedTenants: [tenant.ToString("D")],
        passkeyPromptMode: PasskeyPromptMode.Required,
        version: 3);

      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      Response? parsed = JsonSerializer.Deserialize<Response>(json, ContractSerializationDefaults.Options);

      parsed.ShouldNotBeNull();
      parsed.EntraSignInEnabled.ShouldBeTrue();
      parsed.EntraAllowBootstrap.ShouldBeTrue();
      parsed.EntraTrustedTenants.ShouldBe([tenant.ToString("D")]);
      parsed.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Required);
      parsed.Version.ShouldBe(3);
      json.ShouldContain("passkeyPromptMode");
      return Task.CompletedTask;
    }

    public static Task MockFactory_Should_ReturnDisabledDefaults()
    {
      Response response = GetMockResponseFactory()(new Query());
      response.EntraSignInEnabled.ShouldBeFalse();
      response.EntraTrustedTenants.ShouldBeEmpty();
      response.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Soft);
      return Task.CompletedTask;
    }
  }

  [TestTag("Application")]
  public class GetSiteSettingsHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetSiteSettingsHandler_Given_>();

    public static async Task Empty_Store_Should_Return_Not_Initialized_Without_Insert()
    {
      InMemorySiteSettingsStore store = new();
      GetHandler handler = new(store);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(new Query(), default);
      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(503);
      result.AsT1.Title.ShouldBe("Site settings not initialized");
      SiteSettings? stored = await store.GetAsync();
      stored.ShouldBeNull();
    }
  }
}
