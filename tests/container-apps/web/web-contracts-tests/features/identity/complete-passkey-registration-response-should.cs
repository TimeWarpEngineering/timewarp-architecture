#region Purpose
// Round-trip tests for the identity feature contract CompletePasskeyRegistration_Response (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;
using TimeWarp.Identity;

public class CompletePasskeyRegistration_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CompletePasskeyRegistration_Response_Should>();

  public static Task SerializeAndDeserialize_Via_Constructor()
  {
    CompletePasskeyRegistration.Response response = new(PrincipalId.New(), CredentialId.New(), "Proton Pass");

    CompletePasskeyRegistration.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.PrincipalId.ShouldBe(response.PrincipalId);
    parsed.CredentialId.ShouldBe(response.CredentialId);
    parsed.ProviderLabel.ShouldBe("Proton Pass");
    return Task.CompletedTask;
  }

  public static Task Reject_EmptyPrincipalId_During_Deserialization()
  {
    // PrincipalId's own [TypedId] JsonConverter fail-closes on an empty guid before the Response
    // ctor's Guard even runs — either seam rejecting it is the contract that matters here.
    string json = $$"""{"principalId":"00000000-0000-0000-0000-000000000000","credentialId":"{{Guid.NewGuid()}}"}""";

    Should.Throw<Exception>(() =>
      JsonSerializer.Deserialize<CompletePasskeyRegistration.Response>(json, ContractSerialization.Options));
    return Task.CompletedTask;
  }
}
