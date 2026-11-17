#region Purpose
// Round-trip tests for the identity feature contract CompletePasskeyRegistration_Command (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;

public class CompletePasskeyRegistration_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CompletePasskeyRegistration_Command_Should>();

  public static Task SerializeAndDeserialize()
  {
    CompletePasskeyRegistration.Command command = new()
    {
      CredentialId = "AQIDBA",
      ClientDataJson = "eyJ0eXBlIjoid2ViYXV0aG4uY3JlYXRlIn0",
      AttestationObject = "o2NmbXRkbm9uZQ"
    };

    CompletePasskeyRegistration.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.CredentialId.ShouldBe(command.CredentialId);
    parsed.ClientDataJson.ShouldBe(command.ClientDataJson);
    parsed.AttestationObject.ShouldBe(command.AttestationObject);
    return Task.CompletedTask;
  }
}
