#region Purpose
// Round-trip tests for the identity feature contract GetCredentials_Query (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;

public class GetCredentials_Query_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetCredentials_Query_Should>();

  public static Task SerializeAndDeserialize_Including_Generated_RouteProperty()
  {
    GetCredentials.Query query = new() { UserId = Guid.NewGuid(), IncludeRevoked = true };

    GetCredentials.Query parsed = ContractSerialization.RoundTrip(query);

    parsed.UserId.ShouldBe(query.UserId);
    parsed.IncludeRevoked.ShouldBeTrue();
    return Task.CompletedTask;
  }
}
