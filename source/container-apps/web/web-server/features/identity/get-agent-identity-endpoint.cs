#region Purpose
// HTTP surface for the GetAgentIdentity query; delegates all behavior to the mediator pipeline.
#endregion

#region Design
// The ONE protected endpoint this task ships: [Authorize(Policy = AgentTokenDefaults.IdentityReadPolicy)]
// restricts authentication to ONLY the agent-token scheme (never the cookie scheme or the dormant
// Entra default) and requires the identity:read scope claim — this is the end-to-end proof that
// bearer validation and scope enforcement actually work, not just that the ceremony endpoints
// compile. No binding-source attribute and no route/query parameters: identity comes from the
// ambient bearer token (IAgentCallerContext), never from a client-supplied value. GET carries no
// body by contract.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Authorization;
using static GetAgentIdentity;

[Authorize(Policy = AgentTokenDefaults.IdentityReadPolicy)]
public class GetAgentIdentityEndpoint : BaseEndpoint<Query, Response>
{
  /// <summary>
  /// Get the current agent's own identity.
  /// </summary>
  /// <param name="query"></param>
  /// <returns><see cref="Response"/></returns>
  [HttpGet(Query.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Unauthorized)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Forbidden)]
  public Task<IActionResult> Process(Query query) => Send(query);
}
