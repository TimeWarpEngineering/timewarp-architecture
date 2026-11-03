#region Purpose
// Round-trip tests for the identity feature contract CompletePasskeyAuthentication_Command (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;

public class CompletePasskeyAuthentication_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CompletePasskeyAuthentication_Command_Should>();

  public static Task SerializeAndDeserialize_Including_Optional_UserHandle()
  {
    CompletePasskeyAuthentication.Command command = new()
    {
      CredentialId = "AQIDBA",
      ClientDataJson = "eyJ0eXBlIjoid2ViYXV0aG4uZ2V0In0",
      AuthenticatorData = "AQIDBA",
      Signature = "AQIDBA",
      UserHandle = "dXNlckhhbmRsZQ"
    };

    CompletePasskeyAuthentication.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.CredentialId.ShouldBe(command.CredentialId);
    parsed.ClientDataJson.ShouldBe(command.ClientDataJson);
    parsed.AuthenticatorData.ShouldBe(command.AuthenticatorData);
    parsed.Signature.ShouldBe(command.Signature);
    parsed.UserHandle.ShouldBe(command.UserHandle);
    return Task.CompletedTask;
  }

  public static Task SerializeAndDeserialize_Without_UserHandle()
  {
    CompletePasskeyAuthentication.Command command = new()
    {
      CredentialId = "AQIDBA",
      ClientDataJson = "eyJ0eXBlIjoid2ViYXV0aG4uZ2V0In0",
      AuthenticatorData = "AQIDBA",
      Signature = "AQIDBA"
    };

    CompletePasskeyAuthentication.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.UserHandle.ShouldBeNull();
    return Task.CompletedTask;
  }
}
