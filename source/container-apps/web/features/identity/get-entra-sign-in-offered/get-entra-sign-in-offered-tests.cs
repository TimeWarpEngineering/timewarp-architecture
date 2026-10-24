#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008

// Co-located Jaribu: GetEntraSignInOffered returns the offered SignInMethod list.
// Run standalone:  dotnet run source/container-apps/web/features/identity/get-entra-sign-in-offered/get-entra-sign-in-offered-tests.cs

#region Purpose
// Jaribu runfile: offered methods are the listed ones, Microsoft365 also needs settings; emptied store re-seeds; JSON has no tenant list.
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
  using Microsoft.Extensions.Logging.Abstractions;
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

    public static Task Json_Should_Expose_Only_The_Method_List()
    {
      Response response = new([SignInMethod.Passkey, SignInMethod.Microsoft365]);
      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      json.ShouldBe("""{"methods":["Passkey","Microsoft365"]}""");
      json.ShouldNotContain("tenant");
      json.ShouldNotContain("bootstrap");
      json.ShouldNotContain("prompt");
      return Task.CompletedTask;
    }
  }

  [TestTag("Application")]
  public class GetEntraSignInOfferedHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetEntraSignInOfferedHandler_Given_>();

    public static async Task Settings_Disabled_Should_Omit_Microsoft365()
    {
      OfferedHandler handler = await HandlerAsync(microsoft365Listed: true, settingsEnabled: false);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(new Query(), default);
      result.AsT0.Methods.ShouldBe([SignInMethod.Passkey]);
    }

    public static async Task Microsoft365_Not_Listed_Should_Omit_It()
    {
      OfferedHandler handler = await HandlerAsync(microsoft365Listed: false, settingsEnabled: true);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(new Query(), default);
      result.AsT0.Methods.ShouldBe([SignInMethod.Passkey]);
    }

    public static async Task Both_Listed_And_Enabled_Should_Offer_Both()
    {
      OfferedHandler handler = await HandlerAsync(microsoft365Listed: true, settingsEnabled: true);
      OneOf<Response, SharedProblemDetails> result = await handler.Handle(new Query(), default);
      result.AsT0.Methods.ShouldBe([SignInMethod.Passkey, SignInMethod.Microsoft365]);
    }

    public static async Task Deleted_Row_Should_Offer_From_Seeded_Policy()
    {
      InMemorySiteSettingsStore original = new();
      await original.AddAsync(SiteSettings.Create(false, false, PasskeyPromptMode.Soft));
      InMemorySiteSettingsStore emptied = new();
      IOptions<SignInOptions> signIn = Options.Create(new SignInOptions
      {
        Methods = [SignInMethod.Passkey, SignInMethod.Microsoft365]
      });
      IOptions<EntraAuthenticationOptions> entra = Options.Create(
        new EntraAuthenticationOptions { AllowBootstrap = false });
      OfferedHandler before = new(signIn, original);
      OfferedHandler after = new(
        signIn,
        new SeedOnReadSiteSettingsStore(
          emptied,
          new SiteSettingsSeeder(emptied, entra, signIn, NullLogger<SiteSettingsSeeder>.Instance),
          isDevelopment: false,
          NullLogger<SeedOnReadSiteSettingsStore>.Instance));

      (await before.Handle(new Query(), default)).AsT0.Methods.ShouldBe([SignInMethod.Passkey]);
      (await after.Handle(new Query(), default)).AsT0.Methods.ShouldBe([SignInMethod.Passkey, SignInMethod.Microsoft365]);
      SiteSettings? seeded = await emptied.GetAsync();
      seeded.ShouldNotBeNull();
      seeded.EntraSignInEnabled.ShouldBeTrue();
    }

    private static async Task<OfferedHandler> HandlerAsync(bool microsoft365Listed, bool settingsEnabled)
    {
      InMemorySiteSettingsStore store = new();
      await store.AddAsync(SiteSettings.Create(settingsEnabled, false, PasskeyPromptMode.Soft));
      IOptions<SignInOptions> signIn = Options.Create(new SignInOptions
      {
        Methods = microsoft365Listed
          ? [SignInMethod.Passkey, SignInMethod.Microsoft365]
          : [SignInMethod.Passkey]
      });
      return new OfferedHandler(signIn, store);
    }
  }
}
