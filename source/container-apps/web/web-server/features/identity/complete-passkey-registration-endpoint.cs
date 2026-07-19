#region Purpose
// HTTP surface for the CompletePasskeyRegistration command; delegates all behavior to the mediator pipeline.
#endregion

#region Design
// Anonymous by design — same as StartPasskeyRegistrationEndpoint: this IS the request that
// establishes the session; there is no prior session to authorize against.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using static CompletePasskeyRegistration;

public class CompletePasskeyRegistrationEndpoint : BaseEndpoint<Command, Response>
{
  /// <summary>
  /// Complete a WebAuthn passkey registration ceremony.
  /// </summary>
  /// <param name="command"></param>
  /// <returns><see cref="Response"/> carrying the newly minted PrincipalId</returns>
  [HttpPost(Command.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
  public Task<IActionResult> Process([FromBody] Command command) => Send(command);
}
