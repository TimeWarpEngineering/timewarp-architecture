#region Purpose
// HTTP surface for the GetRoles list query; delegates all behavior to the mediator pipeline.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static GetRoles;

public class GetRolesEndpoint : BaseEndpoint<Query, Response>
{
  /// <summary>
  /// Get the list of roles
  /// </summary>
  /// <param name="query"></param>
  /// <returns><see cref="Response"/></returns>
  [HttpGet(Query.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  public Task<IActionResult> Process([FromQuery] Query query) => Send(query);
}
