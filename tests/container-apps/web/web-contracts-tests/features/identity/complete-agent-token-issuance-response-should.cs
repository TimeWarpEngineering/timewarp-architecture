#region Purpose
// Round-trip tests for the identity feature contract CompleteAgentTokenIssuance_Response (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;
using TimeWarp.Identity;

public class CompleteAgentTokenIssuance_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CompleteAgentTokenIssuance_Response_Should>();

  public static Task SerializeAndDeserialize_Via_Constructor()
  {
    CompleteAgentTokenIssuance.Response response = new
    (
      accessToken: "opaque-token-value",
      expiresInSeconds: 900,
      scopes: [AgentScopes.IdentityRead],
      principalId: PrincipalId.New()
    );

    CompleteAgentTokenIssuance.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.AccessToken.ShouldBe(response.AccessToken);
    parsed.TokenType.ShouldBe("Bearer");
    parsed.ExpiresInSeconds.ShouldBe(900);
    parsed.Scopes.ShouldBe(response.Scopes);
    parsed.PrincipalId.ShouldBe(response.PrincipalId);
    return Task.CompletedTask;
  }
}
