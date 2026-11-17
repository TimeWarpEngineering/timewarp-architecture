#region Purpose
// Round-trip tests for the identity feature contract AddPasskey_Command (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;

public class AddPasskey_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AddPasskey_Command_Should>();

  public static Task SerializeAndDeserialize_Including_Optional_Nickname()
  {
    AddPasskey.Command command = new()
    {
      UserId = Guid.NewGuid(),
      CredentialId = "AQIDBA",
      ClientDataJson = "eyJ0eXBlIjoid2ViYXV0aG4uY3JlYXRlIn0",
      AttestationObject = "o2NmbXRkbm9uZQ",
      Nickname = "MacBook",
      AuthenticatorAttachment = "platform",
      Transports = ["internal", "hybrid"]
    };

    AddPasskey.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.CredentialId.ShouldBe(command.CredentialId);
    parsed.ClientDataJson.ShouldBe(command.ClientDataJson);
    parsed.AttestationObject.ShouldBe(command.AttestationObject);
    parsed.Nickname.ShouldBe("MacBook");
    parsed.AuthenticatorAttachment.ShouldBe("platform");
    parsed.Transports.ShouldBe(["internal", "hybrid"]);
    return Task.CompletedTask;
  }

  public static Task SerializeAndDeserialize_Without_Nickname()
  {
    AddPasskey.Command command = new()
    {
      UserId = Guid.NewGuid(),
      CredentialId = "AQIDBA",
      ClientDataJson = "eyJ0eXBlIjoid2ViYXV0aG4uY3JlYXRlIn0",
      AttestationObject = "o2NmbXRkbm9uZQ"
    };

    AddPasskey.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.Nickname.ShouldBeNull();
    return Task.CompletedTask;
  }
}
