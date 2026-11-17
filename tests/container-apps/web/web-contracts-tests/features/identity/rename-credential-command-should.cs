#region Purpose
// Round-trip tests for the identity feature contract RenameCredential_Command (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;

public class RenameCredential_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RenameCredential_Command_Should>();

  public static Task SerializeAndDeserialize_Including_Generated_CredentialId_And_Nickname()
  {
    RenameCredential.Command command = new() { UserId = Guid.NewGuid(), CredentialId = Guid.NewGuid(), Nickname = "Work laptop" };

    RenameCredential.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.UserId.ShouldBe(command.UserId);
    parsed.CredentialId.ShouldBe(command.CredentialId);
    parsed.Nickname.ShouldBe("Work laptop");
    return Task.CompletedTask;
  }

  public static Task Route_Is_Rename_Under_The_Credential()
  {
    Guid id = Guid.NewGuid();
    RenameCredential.Command command = new() { UserId = Guid.NewGuid(), CredentialId = id, Nickname = "x" };
    command.GetRoute().ShouldBe($"api/identity/credentials/{id}/rename");
    return Task.CompletedTask;
  }
}
