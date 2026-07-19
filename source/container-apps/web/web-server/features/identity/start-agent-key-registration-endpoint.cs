#region Purpose
// HTTP surface for the StartAgentKeyRegistration command; delegates all behavior to the mediator pipeline.
#endregion

#region Design
// Anonymous by design: this endpoint establishes the ceremony that will (if completed) create the
// agent's Principal — nothing to authorize yet.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using static StartAgentKeyRegistration;

public class StartAgentKeyRegistrationEndpoint : BaseEndpoint<Command, Response>
{
  /// <summary>
  /// Start an agent public-key registration ceremony.
  /// </summary>
  /// <param name="command"></param>
  /// <returns><see cref="Response"/> carrying the registration challenge</returns>
  [HttpPost(Command.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  public Task<IActionResult> Process([FromBody] Command command) => Send(command);
}
