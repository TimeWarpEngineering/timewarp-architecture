#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008

// Co-located Jaribu: GetAssistantIdentity contract + handler (task 361).
// Run standalone:  dotnet run source/container-apps/web/features/settings/get-assistant-identity/get-assistant-identity-tests.cs

#region Purpose
// Jaribu runfile: GetAssistantIdentity round-trip, mock avatar agrees with the generator, default and
// renamed names, deterministic avatar that changes on rename, unmigrated store falls back to Akili.
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
  using OneOf;
  using Shouldly;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.Settings.GetAssistantIdentity;
  using AssistantIdentity = TimeWarp.Architecture.Features.Settings.Application.GetAssistantIdentity;

  [TestTag("Contracts")]
  public class GetAssistantIdentityResponse_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetAssistantIdentityResponse_Given_>();

    public static Task ValidResponse_Should_RoundTripThroughJson()
    {
      Response response = new("Zawadi", "data:image/svg+xml;base64,PHN2Zy8+");

      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      Response? parsed = JsonSerializer.Deserialize<Response>(json, ContractSerializationDefaults.Options);

      parsed.ShouldNotBeNull();
      parsed.Name.ShouldBe("Zawadi");
      parsed.Avatar.ShouldBe("data:image/svg+xml;base64,PHN2Zy8+");
      json.ShouldNotContain("passkeyPromptMode");
      json.ShouldNotContain("entraSignInEnabled");
      return Task.CompletedTask;
    }

    public static Task MockFactory_Should_Return_Akili()
    {
      Response response = GetMockResponseFactory()(new Query());
      response.Name.ShouldBe("Akili");
      response.Avatar.ShouldBe(MockAvatar);
      return Task.CompletedTask;
    }

    // web-contracts does not reference TimeWarp.Multiavatar, so the mock is a precomputed copy.
    public static Task MockAvatar_Should_Match_The_Generator()
    {
      MockAvatar.ShouldBe(AssistantIdentity.AvatarFor(AssistantNameRules.Default));
      return Task.CompletedTask;
    }
  }

  [TestTag("Application")]
  public class GetAssistantIdentityHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetAssistantIdentityHandler_Given_>();

    public static async Task Default_Settings_Should_Return_Akili_With_Its_Avatar()
    {
      InMemorySiteSettingsStore store = new();
      await store.AddAsync(SiteSettings.Create());

      Response response = await IdentityFrom(store);

      response.Name.ShouldBe("Akili");
      response.Avatar.ShouldStartWith("data:image/svg+xml;base64,");
      response.Avatar.ShouldBe(AssistantIdentity.AvatarFor("Akili"));
    }

    public static async Task Rename_Should_Change_The_Name_And_The_Avatar()
    {
      InMemorySiteSettingsStore store = new();
      await store.AddAsync(SiteSettings.Create());
      Response before = await IdentityFrom(store);

      SiteSettings current = (await store.GetAsync())!;
      current.RenameAssistant("Zawadi");
      await store.UpdateAsync(current);
      Response after = await IdentityFrom(store);

      after.Name.ShouldBe("Zawadi");
      after.Avatar.ShouldNotBe(before.Avatar);
      after.Avatar.ShouldBe(AssistantIdentity.AvatarFor("Zawadi"));
    }

    public static Task Avatar_Should_Be_Deterministic_Per_Name()
    {
      AssistantIdentity.AvatarFor("Zawadi").ShouldBe(AssistantIdentity.AvatarFor("Zawadi"));
      AssistantIdentity.AvatarFor("Zawadi").ShouldNotBe(AssistantIdentity.AvatarFor("Akili"));
      return Task.CompletedTask;
    }

    public static async Task Empty_Store_Should_Fall_Back_To_The_Default_Name()
    {
      Response response = await IdentityFrom(new InMemorySiteSettingsStore());

      response.Name.ShouldBe(AssistantNameRules.Default);
      response.Avatar.ShouldBe(MockAvatar);
    }

    private static async Task<Response> IdentityFrom(ISiteSettingsStore store)
    {
      OneOf<Response, SharedProblemDetails> result =
        await new AssistantIdentity.Handler(store).Handle(new Query(), default);
      result.IsT0.ShouldBeTrue();
      return result.AsT0;
    }
  }
}
