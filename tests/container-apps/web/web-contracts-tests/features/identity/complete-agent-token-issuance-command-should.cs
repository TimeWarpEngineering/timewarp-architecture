#region Purpose
// Round-trip tests for the identity feature contract CompleteAgentTokenIssuance_Command (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;
using TimeWarp.Identity;

public class CompleteAgentTokenIssuance_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CompleteAgentTokenIssuance_Command_Should>();

  public static Task SerializeAndDeserialize_Scopes_List()
  {
    CompleteAgentTokenIssuance.Command command = new()
    {
      KeyId = "a1b2c3",
      Challenge = "BQYHCA",
      Signature = "CQoLDA",
      Scopes = [AgentScopes.IdentityRead, AgentScopes.DemoInvoke]
    };

    CompleteAgentTokenIssuance.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.KeyId.ShouldBe(command.KeyId);
    parsed.Challenge.ShouldBe(command.Challenge);
    parsed.Signature.ShouldBe(command.Signature);
    parsed.Scopes.ShouldBe(command.Scopes);
    return Task.CompletedTask;
  }
}
