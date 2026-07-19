#region Purpose
// HTTP surface for the GetCurrentSession query; delegates all behavior to the mediator pipeline.
#endregion

#region Design
// No binding-source attribute and no route/query parameters: identity comes from the ambient
// session cookie (IBrowserSessionService), never from a client-supplied value. GET carries no body
// by contract.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using static GetCurrentSession;

public class GetCurrentSessionEndpoint : BaseEndpoint<Query, Response>
{
  /// <summary>
  /// Get the current browser session.
  /// </summary>
  /// <param name="query"></param>
  /// <returns><see cref="Response"/></returns>
  [HttpGet(Query.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  public Task<IActionResult> Process(Query query) => Send(query);
}
