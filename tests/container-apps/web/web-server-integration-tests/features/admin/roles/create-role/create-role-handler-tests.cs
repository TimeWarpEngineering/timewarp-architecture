#region Purpose
// Handler-level test: a valid CreateRole command produces a Response with a real role id.
#endregion

namespace CreateRoleHandler_;

using static TimeWarp.Architecture.Features.Admin.Roles.CreateRole;

public class Handle_Returns
{

  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Handle_Returns>();

  public static async Task SetupOnce()
  {
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync();
#else
    Graph = await HostGraphFactory.CreateWebAsync();
#endif
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  private static Command CreateValidCommand() => new()
  {
    UserId = Guid.NewGuid(),
    Name = "Dispatcher",
    Description = "Schedules and routes work."
  };

  public static async Task Response_With_NonEmpty_RoleId()
  {

    Command command = CreateValidCommand();

    OneOf<Response, SharedProblemDetails> result = await Web.Send(command);

    result.Switch
    (
      response => response.RoleId.ShouldNotBe(Guid.Empty),
      problemDetails => problemDetails.ShouldBeNull("CreateRole handler returned SharedProblemDetails for a valid command.")
    );
  }

}
