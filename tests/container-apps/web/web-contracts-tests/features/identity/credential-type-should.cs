#region Purpose
// Round-trip tests for the identity feature contract CredentialType (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Web.Contracts.Tests;
using TimeWarp.Identity;

public class CredentialType_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CredentialType_Should>();

  public static Task RoundTrip_Passkey_And_AgentKey_As_Strings()
  {
    string passkeyJson = JsonSerializer.Serialize(CredentialType.Passkey, ContractSerialization.Options);
    string agentKeyJson = JsonSerializer.Serialize(CredentialType.AgentKey, ContractSerialization.Options);

    passkeyJson.ShouldBe("\"Passkey\"");
    agentKeyJson.ShouldBe("\"AgentKey\"");

    JsonSerializer.Deserialize<CredentialType>(passkeyJson, ContractSerialization.Options)
      .ShouldBe(CredentialType.Passkey);
    JsonSerializer.Deserialize<CredentialType>(agentKeyJson, ContractSerialization.Options)
      .ShouldBe(CredentialType.AgentKey);
    return Task.CompletedTask;
  }
}
