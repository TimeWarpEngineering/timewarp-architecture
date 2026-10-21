#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007

#region Purpose
// The admin xAI key status round-trips, and neither the status nor a clear command carries the key.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.AgentChats
{

  using System.Text.Json;
  using Shouldly;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Contracts")]
  public class XaiApiKeyStatus_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<XaiApiKeyStatus_Given_>();

    public static Task Status_Should_RoundTrip_Without_An_ApiKey_Property()
    {
      GetXaiApiKeyStatus.Response response = new(
        managedByHost: false,
        configured: true,
        storageAvailable: true,
        canChange: true,
        needsReplacement: false,
        suffix: "aaaa",
        setAt: new DateTimeOffset(2026, 10, 22, 4, 22, 0, TimeSpan.Zero),
        setByPrincipalId: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        setupHint: XaiChatDefaults.DeployedSetupHint);

      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      json.ShouldNotContain("xai-");
      using JsonDocument document = JsonDocument.Parse(json);
      string[] names = [.. document.RootElement.EnumerateObject().Select(static property => property.Name).Order(StringComparer.Ordinal)];
      names.ShouldNotContain("apiKey");
      names.ShouldBe(
      [
        "canChange",
        "configured",
        "managedByHost",
        "needsReplacement",
        "setAt",
        "setByPrincipalId",
        "setupHint",
        "storageAvailable",
        "suffix"
      ]);

      GetXaiApiKeyStatus.Response? parsed =
        JsonSerializer.Deserialize<GetXaiApiKeyStatus.Response>(json, ContractSerializationDefaults.Options);
      parsed.ShouldNotBeNull();
      parsed.Suffix.ShouldBe("aaaa");
      parsed.Configured.ShouldBeTrue();
      parsed.SetByPrincipalId.ShouldBe(response.SetByPrincipalId);
      parsed.SetupHint.ShouldBe(response.SetupHint);
      return Task.CompletedTask;
    }

    public static Task Set_Command_Should_Carry_The_Key_And_The_Response_Should_Not()
    {
      const string secret = "xai-test-key-398-aaaa";
      SetXaiApiKey.Command command = new() { ApiKey = secret };
      string commandJson = JsonSerializer.Serialize(command, ContractSerializationDefaults.Options);
      commandJson.ShouldContain("apiKey");
      SetXaiApiKey.Command? parsed =
        JsonSerializer.Deserialize<SetXaiApiKey.Command>(commandJson, ContractSerializationDefaults.Options);
      parsed.ShouldNotBeNull();
      parsed.ApiKey.ShouldBe(secret);

      string responseJson = JsonSerializer.Serialize(
        new SetXaiApiKey.Response(
          managedByHost: false,
          configured: true,
          storageAvailable: true,
          canChange: true,
          needsReplacement: false,
          suffix: "aaaa",
          setAt: null,
          setByPrincipalId: null,
          setupHint: XaiChatDefaults.DeployedSetupHint),
        ContractSerializationDefaults.Options);
      responseJson.ShouldNotContain(secret);
      using JsonDocument responseDocument = JsonDocument.Parse(responseJson);
      responseDocument.RootElement.EnumerateObject().Select(static property => property.Name)
        .ShouldNotContain("apiKey");
      return Task.CompletedTask;
    }
  }
}
