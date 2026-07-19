#region Purpose
// HTTP surface for the StartPasskeyAuthentication command; delegates all behavior to the mediator pipeline.
#endregion

#region Design
// Anonymous by design: this endpoint issues a fresh challenge for a not-yet-authenticated browser —
// nothing to authorize yet.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using static StartPasskeyAuthentication;

public class StartPasskeyAuthenticationEndpoint : BaseEndpoint<Command, Response>
{
  /// <summary>
  /// Start a WebAuthn passkey authentication ceremony.
  /// </summary>
  /// <param name="command"></param>
  /// <returns><see cref="Response"/> carrying the request options JSON</returns>
  [HttpPost(Command.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  public Task<IActionResult> Process([FromBody] Command command) => Send(command);
}
