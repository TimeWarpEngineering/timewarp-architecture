#region Purpose
// HTTP surface for the DeleteRole command; delegates all behavior to the mediator pipeline.
#endregion

#region Design
// No binding-source attribute on the parameter: RoleId binds from the route segment and UserId
// from the query string — DELETE carries no body by contract (see BaseApiService.PrepareContent).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static DeleteRole;

public class DeleteRoleEndpoint : BaseEndpoint<Command, Response>
{
  /// <summary>
  /// Delete a role
  /// </summary>
  /// <param name="command"></param>
  /// <returns><see cref="Response"/></returns>
  [HttpDelete(Command.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
  public Task<IActionResult> Process(Command command) => Send(command);
}
