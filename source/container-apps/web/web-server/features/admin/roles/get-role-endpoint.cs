#region Purpose
// HTTP surface for the GetRole by-id query; delegates all behavior to the mediator pipeline.
#endregion

#region Design
// No binding-source attribute on the parameter: RoleId binds from the route segment and UserId
// from the query string (SuppressInferBindingSourcesForParameters keeps [ApiController] from
// forcing a body source). GET carries no body by contract.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static GetRole;

public class GetRoleEndpoint : BaseEndpoint<Query, Response>
{
  /// <summary>
  /// Get a role by id
  /// </summary>
  /// <param name="query"></param>
  /// <returns><see cref="Response"/></returns>
  [HttpGet(Query.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
  public Task<IActionResult> Process(Query query) => Send(query);
}
