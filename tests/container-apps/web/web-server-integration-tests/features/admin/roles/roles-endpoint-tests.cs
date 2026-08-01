#region Purpose
// End-to-end tests for the roles read/update/delete endpoints: real host, real pipeline,
// query-string auth for GET/DELETE, 404 problem details for missing ids.
#endregion

namespace RolesEndpoints_;

using TimeWarp.Architecture.Features.Admin.Roles;
using TimeWarp.Architecture.Features;

public class GetRoles_Returns
{

  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetRoles_Returns>();

  public static async Task SetupOnce()
  {
    Graph = await HostGraphFactory.CreateWebWithApiAsync();
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  public static async Task Seeded_Roles()
  {
    var query = new GetRoles.Query { UserId = Guid.NewGuid() };

    OneOf<GetRoles.Response, SharedProblemDetails> result = await Web.Send(query);

    result.Switch
    (
      response =>
      {
        response.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
        response.Items.ShouldContain(dto => dto.RoleId == RoleIds.Administrator);
      },
      problemDetails => problemDetails.ShouldBeNull("GetRoles returned SharedProblemDetails.")
    );
  }

}

public class GetRole_Returns
{

  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetRole_Returns>();

  public static async Task SetupOnce()
  {
    Graph = await HostGraphFactory.CreateWebWithApiAsync();
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  public static async Task Role_Given_Known_Id()
  {
    var query = new GetRole.Query { RoleId = RoleIds.Administrator, UserId = Guid.NewGuid() };

    OneOf<GetRole.Response, SharedProblemDetails> result = await Web.Send(query);

    result.Switch
    (
      response =>
      {
        response.RoleId.ShouldBe(RoleIds.Administrator);
        response.Name.ShouldBe(nameof(RoleIds.Administrator));
      },
      problemDetails => problemDetails.ShouldBeNull("GetRole returned SharedProblemDetails for a known id.")
    );
  }

  public static async Task NotFound_Given_Unknown_Id()
  {
    var query = new GetRole.Query { RoleId = Guid.NewGuid(), UserId = Guid.NewGuid() };

    OneOf<GetRole.Response, SharedProblemDetails> result = await Web.Send(query);

    result.Switch
    (
      response => response.ShouldBeNull("GetRole returned a Response for an unknown id."),
      problemDetails => problemDetails.Status.ShouldBe(404)
    );
  }

}

public class UpdateThenDelete_Roundtrip
{

  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<UpdateThenDelete_Roundtrip>();

  public static async Task SetupOnce()
  {
    Graph = await HostGraphFactory.CreateWebWithApiAsync();
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  public static async Task Create_Update_Get_Delete()
  {
    Guid userId = Guid.NewGuid();

    // Create a role this test owns (the seeded roles stay untouched for other tests).
    var create = new CreateRole.Command { UserId = userId, Name = "Courier", Description = "Delivers things." };
    OneOf<CreateRole.Response, SharedProblemDetails> created = await Web.Send(create);
    Guid roleId = created.AsT0.RoleId;

    // Update it (PUT body carries UserId + RoleId).
    var update = new UpdateRole.Command { RoleId = roleId, UserId = userId, Name = "Courier", Description = "Delivers things faster." };
    OneOf<UpdateRole.Response, SharedProblemDetails> updated = await Web.Send(update);
    updated.IsT0.ShouldBeTrue("UpdateRole returned SharedProblemDetails.");

    // Read back the update.
    OneOf<GetRole.Response, SharedProblemDetails> fetched =
      await Web.Send(new GetRole.Query { RoleId = roleId, UserId = userId });
    fetched.AsT0.Description.ShouldBe("Delivers things faster.");

    // Delete it (DELETE: RoleId in route, UserId in query string).
    OneOf<DeleteRole.Response, SharedProblemDetails> deleted =
      await Web.Send(new DeleteRole.Command { RoleId = roleId, UserId = userId });
    deleted.IsT0.ShouldBeTrue("DeleteRole returned SharedProblemDetails.");

    // Gone.
    OneOf<GetRole.Response, SharedProblemDetails> afterDelete =
      await Web.Send(new GetRole.Query { RoleId = roleId, UserId = userId });
    afterDelete.IsT1.ShouldBeTrue("Deleted role still resolves.");
    afterDelete.AsT1.Status.ShouldBe(404);
  }

}
