#region Purpose
// Round-trip tests for the identity feature contract AddAgentKey_Command (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;

public class AddAgentKey_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AddAgentKey_Command_Should>();

  public static Task SerializeAndDeserialize_Including_Optional_Nickname()
  {
    AddAgentKey.Command command = new()
    {
      UserId = Guid.NewGuid(),
      PublicKey = "AQIDBA",
      Challenge = "BQYHCA",
      Signature = "CQoLDA",
      Nickname = "prod-worker-4"
    };

    AddAgentKey.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.PublicKey.ShouldBe(command.PublicKey);
    parsed.Challenge.ShouldBe(command.Challenge);
    parsed.Signature.ShouldBe(command.Signature);
    parsed.Nickname.ShouldBe("prod-worker-4");
    return Task.CompletedTask;
  }

  public static Task SerializeAndDeserialize_Without_Nickname()
  {
    AddAgentKey.Command command = new()
    {
      UserId = Guid.NewGuid(),
      PublicKey = "AQIDBA",
      Challenge = "BQYHCA",
      Signature = "CQoLDA"
    };

    AddAgentKey.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.Nickname.ShouldBeNull();
    return Task.CompletedTask;
  }
}
