#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:package Microsoft.Extensions.Options
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Co-located Jaribu: GetEntraSignInOffered exposes only the boolean (task 219-006).
// Run standalone:  dotnet run source/container-apps/web/features/identity/get-entra-sign-in-offered/get-entra-sign-in-offered-tests.cs

#region Purpose
// Jaribu runfile: offered boolean is scheme-enabled AND settings-enabled; JSON has no tenant list.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Identity
{

  using System.Text.Json;
  using System.Threading.Tasks;
  using Microsoft.Extensions.Options;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Features.Identity.Application;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.Identity.GetEntraSignInOffered;
  using OfferedHandler = TimeWarp.Architecture.Features.Identity.Application.GetEntraSignInOffered.Handler;

  [TestTag("Contracts")]
  public class GetEntraSignInOfferedResponse_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetEntraSignInOfferedResponse_Given_>();

    public static Task Json_Should_Expose_Only_Offered()
    {
      Response response = new(true);
      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      json.ShouldBe("""{"offered":true}""");
      json.ShouldNotContain("tenant");
      json.ShouldNotContain("bootstrap");
      json.ShouldNotContain("passkey");
      return Task.CompletedTask;
    }
  }

  [TestTag("Application")]
  public class GetEntraSignInOfferedHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetEntraSignInOfferedHandler_Given_>();

    public static async Task Settings_Disabled_Should_Not_Offer()
    {
      OfferedHandler handler = await HandlerAsync(schemeEnabled: true, settingsEnabled: false);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(new Query(), default);
      result.AsT0.Offered.ShouldBeFalse();
    }

    public static async Task Scheme_Disabled_Should_Not_Offer()
    {
      OfferedHandler handler = await HandlerAsync(schemeEnabled: false, settingsEnabled: true);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(new Query(), default);
      result.AsT0.Offered.ShouldBeFalse();
    }

    public static async Task Both_Enabled_Should_Offer()
    {
      OfferedHandler handler = await HandlerAsync(schemeEnabled: true, settingsEnabled: true);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(new Query(), default);
      result.AsT0.Offered.ShouldBeTrue();
    }

    private static async Task<OfferedHandler> HandlerAsync(bool schemeEnabled, bool settingsEnabled)
    {
      InMemorySiteSettingsStore store = new();
      await store.AddAsync(SiteSettings.Create(settingsEnabled, false, PasskeyPromptMode.Soft));
      IOptions<EntraAuthenticationOptions> options = Options.Create(
        new EntraAuthenticationOptions { Enabled = schemeEnabled });
      return new OfferedHandler(options, store);
    }
  }
}
