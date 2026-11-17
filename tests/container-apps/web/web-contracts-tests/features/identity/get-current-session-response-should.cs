#region Purpose
// Round-trip tests for the identity feature contract GetCurrentSession_Response (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;
using TimeWarp.Identity;

public class GetCurrentSession_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetCurrentSession_Response_Should>();

  public static Task SerializeAndDeserialize_Authenticated()
  {
    GetCurrentSession.Response response = new(
      isAuthenticated: true,
      PrincipalId.New(),
      roleIds: [RoleIds.Administrator],
      permissions: [PermissionIds.AdminAccess, PermissionIds.ProfileRead]);

    GetCurrentSession.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.IsAuthenticated.ShouldBeTrue();
    parsed.PrincipalId.ShouldBe(response.PrincipalId);
    parsed.RoleIds.ShouldBe(response.RoleIds);
    parsed.Permissions.ShouldBe(response.Permissions);
    // Task 253: the account fingerprint travels and matches the principal it was derived from.
    parsed.AccountFingerprint.ShouldBe(PrincipalFingerprint.Compute(response.PrincipalId!.Value));
    parsed.AccountFingerprint.ShouldBe(response.AccountFingerprint);
    return Task.CompletedTask;
  }

  public static Task SerializeAndDeserialize_Unauthenticated()
  {
    GetCurrentSession.Response response = new(isAuthenticated: false, principalId: null);

    GetCurrentSession.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.IsAuthenticated.ShouldBeFalse();
    parsed.PrincipalId.ShouldBeNull();
    parsed.AccountFingerprint.ShouldBeNull();
    parsed.RoleIds.ShouldBeEmpty();
    parsed.Permissions.ShouldBeEmpty();
    return Task.CompletedTask;
  }
}
