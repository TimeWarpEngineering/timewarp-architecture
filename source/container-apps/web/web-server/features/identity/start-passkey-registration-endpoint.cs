#region Purpose
// HTTP surface for the StartPasskeyRegistration command; delegates all behavior to the mediator pipeline.
#endregion

#region Design
// Anonymous by design: this endpoint establishes the ceremony that will (if completed) create the
// session — nothing to authorize yet.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using static StartPasskeyRegistration;

public class StartPasskeyRegistrationEndpoint : BaseEndpoint<Command, Response>
{
  /// <summary>
  /// Start a WebAuthn passkey registration ceremony.
  /// </summary>
  /// <param name="command"></param>
  /// <returns><see cref="Response"/> carrying the creation options JSON</returns>
  [HttpPost(Command.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  public Task<IActionResult> Process([FromBody] Command command) => Send(command);
}
