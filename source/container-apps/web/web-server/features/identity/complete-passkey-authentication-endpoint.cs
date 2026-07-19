#region Purpose
// HTTP surface for the CompletePasskeyAuthentication command; delegates all behavior to the mediator pipeline.
#endregion

#region Design
// Anonymous by design: this IS the request that establishes the session — nothing to authorize
// against yet. A rejected assertion (unknown credential, bad signature, replayed challenge) yields a
// SharedProblemDetails, never a 401/403 auth challenge, since there is no prior session here to
// challenge against.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using static CompletePasskeyAuthentication;

public class CompletePasskeyAuthenticationEndpoint : BaseEndpoint<Command, Response>
{
  /// <summary>
  /// Complete a WebAuthn passkey authentication ceremony.
  /// </summary>
  /// <param name="command"></param>
  /// <returns><see cref="Response"/> carrying the authenticated PrincipalId</returns>
  [HttpPost(Command.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Forbidden)]
  public Task<IActionResult> Process([FromBody] Command command) => Send(command);
}
