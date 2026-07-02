#region Purpose
// Handler-level test: a valid CreateRole command produces a Response with a real role id.
#endregion

namespace CreateRoleHandler_;

using static TimeWarp.Architecture.Features.Admin.Roles.CreateRole;

public class Handle_Returns
{
  private readonly Command Command;
  private readonly WebTestServerApplication WebTestServerApplication;

  public Handle_Returns
  (
    WebTestServerApplication webTestServerApplication
  )
  {
    Command = new Command
    {
      UserId = Guid.NewGuid(),
      Name = "Dispatcher",
      Description = "Schedules and routes work."
    };
    WebTestServerApplication = webTestServerApplication;
  }

  public async Task Response_With_NonEmpty_RoleId()
  {
    OneOf<Response, SharedProblemDetails> result = await WebTestServerApplication.Send(Command);

    result.Switch
    (
      response => response.RoleId.ShouldNotBe(Guid.Empty),
      problemDetails => problemDetails.ShouldBeNull("CreateRole handler returned SharedProblemDetails for a valid command.")
    );
  }
}
