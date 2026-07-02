#region Purpose
// HTTP surface for the TrackEvent analytics command; delegates all behavior to the mediator pipeline.
#endregion

#region Design
// Endpoint-centric pattern: this class is a one-line shim so validation (FluentValidationBehavior)
// and handling live in the mediator pipeline; the shim itself carries nothing worth unit testing.
// `using static TrackEvent` binds to the contract's nested Command/Response, and Command.Route
// keeps the URL owned by the contract so client and server cannot drift.
// ProducesResponseType attributes exist solely to make the OpenAPI document accurate.
#endregion

namespace TimeWarp.Architecture.Features.Analytics;

using static TrackEvent;
public class TrackEventEndpoint : BaseEndpoint<Command, Response>
{
  /// <summary>
  /// Track events in analytics
  /// </summary>
  /// <param name="command"></param>
  /// <returns></returns>
  [HttpPost(Command.Route)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  public Task<IActionResult> Process([FromBody] Command command) => Send(command);
}
