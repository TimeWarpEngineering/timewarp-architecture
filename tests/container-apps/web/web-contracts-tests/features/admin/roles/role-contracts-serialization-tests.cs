#region Purpose
// Round-trip tests for the admin/roles contracts — the shapes where serialization can diverge.
#endregion

#region Design
// Each test targets a specific deserialization mechanism, not a POCO copy:
// - ctor+Guard Responses prove System.Text.Json binds camelCase JSON to constructor parameters
//   AND that Guard invariants run during deserialization (a Guid.Empty payload must throw, not
//   materialize an invalid Response).
// - GetRoles.Response proves the ListResponse<T> envelope (base get-only props via derived ctor).
// - GetRole.Query proves source-generated route properties ([ApiRoute] emits RoleId) serialize.
#endregion

// ReSharper disable InconsistentNaming
namespace RoleContracts_;

using TimeWarp.Architecture.Features.Admin.Roles;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Web.Contracts.Tests;

public class CreateRole_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CreateRole_Command_Should>();

  public static Task SerializeAndDeserialize()
  {
    CreateRole.Command command = new()
    {
      UserId = Guid.NewGuid(),
      Name = "Auditor",
      Description = "Read-only access to financial modules."
    };

    CreateRole.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.UserId.ShouldBe(command.UserId);
    parsed.Name.ShouldBe(command.Name);
    parsed.Description.ShouldBe(command.Description);
    return Task.CompletedTask;
  }
}

public class CreateRole_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CreateRole_Response_Should>();

  public static Task SerializeAndDeserialize_Via_Constructor()
  {
    CreateRole.Response response = new(RoleIds.Administrator);

    CreateRole.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.RoleId.ShouldBe(RoleIds.Administrator);
    return Task.CompletedTask;
  }

  public static Task Reject_EmptyGuid_During_Deserialization()
  {
    // The ctor Guard is the Response's invariant gate; deserialization must not bypass it.
    const string json = """{"roleId":"00000000-0000-0000-0000-000000000000"}""";

    Should.Throw<Exception>(() =>
      JsonSerializer.Deserialize<CreateRole.Response>(json, ContractSerialization.Options));
    return Task.CompletedTask;
  }
}

public class GetRole_Query_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetRole_Query_Should>();

  public static Task SerializeAndDeserialize_Including_Generated_RouteProperty()
  {
    // RoleId is not declared in get-role.cs — [ApiRoute("api/Roles/{RoleId:guid}")] generates it.
    Guid roleId = Guid.NewGuid();
    GetRole.Query query = new() { RoleId = roleId, UserId = Guid.NewGuid() };

    GetRole.Query parsed = ContractSerialization.RoundTrip(query);

    parsed.RoleId.ShouldBe(roleId);
    parsed.UserId.ShouldBe(query.UserId);
    return Task.CompletedTask;
  }
}

public class GetRole_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetRole_Response_Should>();

  public static Task SerializeAndDeserialize_Via_Constructor()
  {
    GetRole.Response response = new(
      RoleIds.Member,
      "Member",
      "Default passkey principal.",
      [PermissionIds.ProfileRead, PermissionIds.SettingsRead]);

    GetRole.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.RoleId.ShouldBe(RoleIds.Member);
    parsed.Name.ShouldBe("Member");
    parsed.Description.ShouldBe("Default passkey principal.");
    parsed.PermissionIds.ShouldBe([PermissionIds.ProfileRead, PermissionIds.SettingsRead]);
    return Task.CompletedTask;
  }
}

public class GetRoles_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetRoles_Response_Should>();

  public static Task SerializeAndDeserialize_ListResponse_Envelope()
  {
    GetRoles.RoleDto[] items =
    [
      new(
        RoleIds.Administrator,
        "Administrator",
        "Tenant admin.",
        [PermissionIds.AdminAccess, PermissionIds.AdminRolesManage]),
      new(RoleIds.Member, "Member", "Default passkey principal.", [PermissionIds.ProfileRead])
    ];
    GetRoles.Response response = new(totalCount: 4, items);

    GetRoles.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.TotalCount.ShouldBe(4);
    parsed.Items.Length.ShouldBe(2);
    parsed.Items[0].RoleId.ShouldBe(RoleIds.Administrator);
    parsed.Items[0].Name.ShouldBe("Administrator");
    parsed.Items[0].PermissionIds.ShouldBe([PermissionIds.AdminAccess, PermissionIds.AdminRolesManage]);
    parsed.Items[1].RoleId.ShouldBe(RoleIds.Member);
    parsed.Items[1].Description.ShouldBe("Default passkey principal.");
    parsed.Items[1].PermissionIds.ShouldBe([PermissionIds.ProfileRead]);
    return Task.CompletedTask;
  }
}

public class SetRolePermissions_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SetRolePermissions_Command_Should>();

  public static Task SerializeAndDeserialize_Including_Generated_RouteProperty()
  {
    Guid roleId = RoleIds.Developer;
    SetRolePermissions.Command command = new()
    {
      RoleId = roleId,
      UserId = Guid.NewGuid(),
      PermissionIds = [PermissionIds.DeveloperAccess, PermissionIds.ProfileRead]
    };

    SetRolePermissions.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.RoleId.ShouldBe(roleId);
    parsed.UserId.ShouldBe(command.UserId);
    parsed.PermissionIds.ShouldBe(command.PermissionIds);
    return Task.CompletedTask;
  }
}

public class SetRolePermissions_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SetRolePermissions_Response_Should>();

  public static Task SerializeAndDeserialize()
  {
    SetRolePermissions.Response response = new([PermissionIds.AdminAccess, PermissionIds.AdminRolesRead]);

    SetRolePermissions.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.PermissionIds.ShouldBe([PermissionIds.AdminAccess, PermissionIds.AdminRolesRead]);
    return Task.CompletedTask;
  }
}
