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
using TimeWarp.Architecture.Features.Authorization;
using TimeWarp.Architecture.Web.Contracts.Tests;

public class CreateRole_Command_Should
{
  public static void SerializeAndDeserialize()
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
  }
}

public class CreateRole_Response_Should
{
  public static void SerializeAndDeserialize_Via_Constructor()
  {
    CreateRole.Response response = new(RoleIds.Administrator);

    CreateRole.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.RoleId.ShouldBe(RoleIds.Administrator);
  }

  public static void Reject_EmptyGuid_During_Deserialization()
  {
    // The ctor Guard is the Response's invariant gate; deserialization must not bypass it.
    string json = """{"roleId":"00000000-0000-0000-0000-000000000000"}""";

    Should.Throw<Exception>(() =>
      JsonSerializer.Deserialize<CreateRole.Response>(json, ContractSerialization.Options));
  }
}

public class GetRole_Query_Should
{
  public static void SerializeAndDeserialize_Including_Generated_RouteProperty()
  {
    // RoleId is not declared in get-role.cs — [ApiRoute("api/Roles/{RoleId:min(1)}")] generates it.
    GetRole.Query query = new() { RoleId = 42, UserId = Guid.NewGuid() };

    GetRole.Query parsed = ContractSerialization.RoundTrip(query);

    parsed.RoleId.ShouldBe(42);
    parsed.UserId.ShouldBe(query.UserId);
  }
}

public class GetRole_Response_Should
{
  public static void SerializeAndDeserialize_Via_Constructor()
  {
    GetRole.Response response = new(RoleIds.Accountant, "Accountant", "Accounting module access.");

    GetRole.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.RoleId.ShouldBe(RoleIds.Accountant);
    parsed.Name.ShouldBe("Accountant");
    parsed.Description.ShouldBe("Accounting module access.");
  }
}

public class GetRoles_Response_Should
{
  public static void SerializeAndDeserialize_ListResponse_Envelope()
  {
    GetRoles.RoleDto[] items =
    [
      new(RoleIds.Administrator, "Administrator", "All modules."),
      new(RoleIds.Accountant, "Accountant", "Accounting module.")
    ];
    GetRoles.Response response = new(totalCount: 7, items);

    GetRoles.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.TotalCount.ShouldBe(7);
    parsed.Items.Length.ShouldBe(2);
    parsed.Items[0].RoleId.ShouldBe(RoleIds.Administrator);
    parsed.Items[0].Name.ShouldBe("Administrator");
    parsed.Items[1].RoleId.ShouldBe(RoleIds.Accountant);
    parsed.Items[1].Description.ShouldBe("Accounting module.");
  }
}
