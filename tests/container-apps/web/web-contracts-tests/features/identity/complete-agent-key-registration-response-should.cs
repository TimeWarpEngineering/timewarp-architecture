#region Purpose
// Round-trip tests for the identity feature contract CompleteAgentKeyRegistration_Response (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;
using TimeWarp.Identity;

public class CompleteAgentKeyRegistration_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CompleteAgentKeyRegistration_Response_Should>();

  public static Task SerializeAndDeserialize_Via_Constructor()
  {
    CompleteAgentKeyRegistration.Response response = new(PrincipalId.New(), "a1b2c3");

    CompleteAgentKeyRegistration.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.PrincipalId.ShouldBe(response.PrincipalId);
    parsed.KeyId.ShouldBe(response.KeyId);
    return Task.CompletedTask;
  }

  public static Task Reject_EmptyPrincipalId_During_Deserialization()
  {
    const string json = """{"principalId":"00000000-0000-0000-0000-000000000000","keyId":"a1b2c3"}""";

    Should.Throw<Exception>(() =>
      JsonSerializer.Deserialize<CompleteAgentKeyRegistration.Response>(json, ContractSerialization.Options));
    return Task.CompletedTask;
  }
}
