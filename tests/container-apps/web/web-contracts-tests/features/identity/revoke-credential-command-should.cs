#region Purpose
// Round-trip tests for the identity feature contract RevokeCredential_Command (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;

public class RevokeCredential_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RevokeCredential_Command_Should>();

  public static Task SerializeAndDeserialize_Including_Generated_RouteProperty()
  {
    RevokeCredential.Command command = new() { UserId = Guid.NewGuid(), CredentialId = Guid.NewGuid() };

    RevokeCredential.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.UserId.ShouldBe(command.UserId);
    parsed.CredentialId.ShouldBe(command.CredentialId);
    return Task.CompletedTask;
  }
}
