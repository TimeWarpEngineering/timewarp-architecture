#region Purpose
// Minimal GET endpoint for the Hello query; the template's smoke-test and reference endpoint.
#endregion

#region Design
// Serves as the simplest complete example of the endpoint-centric pattern: a one-line shim over
// the mediator pipeline, with `using static Hello` binding the contract's nested Query/Response
// and Query.RouteTemplate keeping the URL owned by the contract.
// Integration tests and manual health probing rely on this endpoint staying trivial.
#endregion

namespace TimeWarp.Architecture.Features.Hellos;

using static Hello;

public class HelloEndpoint : BaseEndpoint<Query, Response>
{
  /// <summary>
  /// Simple endpoint for testing
  /// </summary>
  /// <param name="query"></param>
  /// <returns></returns>
  /// <returns><see cref="Response"/></returns>
  [HttpGet(Query.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  public Task<IActionResult> Process([FromQuery] Query query) => Send(query);
}
