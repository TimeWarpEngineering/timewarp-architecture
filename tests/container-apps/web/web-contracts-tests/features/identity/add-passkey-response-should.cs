#region Purpose
// Round-trip tests for the identity feature contract AddPasskey_Response (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;
using TimeWarp.Identity;

public class AddPasskey_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AddPasskey_Response_Should>();

  public static Task SerializeAndDeserialize_Via_Constructor()
  {
    AddPasskey.Response response = new(CredentialId.New());

    AddPasskey.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.CredentialId.ShouldBe(response.CredentialId);
    return Task.CompletedTask;
  }

  public static Task Reject_EmptyCredentialId_During_Deserialization()
  {
    const string json = """{"credentialId":"00000000-0000-0000-0000-000000000000"}""";

    Should.Throw<Exception>(() =>
      JsonSerializer.Deserialize<AddPasskey.Response>(json, ContractSerialization.Options));
    return Task.CompletedTask;
  }
}
