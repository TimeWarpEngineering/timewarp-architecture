#region Purpose
// HTTP surface for the UpdateRole command; delegates all behavior to the mediator pipeline.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static UpdateRole;

public class UpdateRoleEndpoint : BaseEndpoint<Command, Response>
{
  /// <summary>
  /// Update an existing role
  /// </summary>
  /// <param name="command"></param>
  /// <returns><see cref="Response"/></returns>
  [HttpPut(Command.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
  public Task<IActionResult> Process([FromBody] Command command) => Send(command);
}
