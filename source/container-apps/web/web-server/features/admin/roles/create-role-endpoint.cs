#region Purpose
// HTTP surface for the CreateRole command; delegates all behavior to the mediator pipeline.
#endregion

#region Design
// Endpoint-centric pattern: a one-line shim so validation (FluentValidationBehavior runs the
// contract's Validator — RoleDetailsValidator + AuthApiRequestValidator — server-side) and
// handling live in the mediator pipeline. `using static CreateRole` binds the contract's nested
// Command/Response, and Command.RouteTemplate keeps the URL owned by the contract so client and
// server cannot drift.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static CreateRole;

public class CreateRoleEndpoint : BaseEndpoint<Command, Response>
{
  /// <summary>
  /// Create a new role
  /// </summary>
  /// <param name="command"></param>
  /// <returns><see cref="Response"/> carrying the new role's id</returns>
  [HttpPost(Command.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  public Task<IActionResult> Process([FromBody] Command command) => Send(command);
}
