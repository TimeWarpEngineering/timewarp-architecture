#region Purpose
// HTTP surface for the CompleteAgentTokenIssuance command; delegates all behavior to the mediator pipeline.
#endregion

#region Design
// Anonymous by design: this IS the request that proves possession of a registered key and mints the
// bearer token — nothing to authorize against yet (the token this request produces is what later
// authorizes GetAgentIdentity and other agent-token-protected endpoints).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using static CompleteAgentTokenIssuance;

public class CompleteAgentTokenIssuanceEndpoint : BaseEndpoint<Command, Response>
{
  /// <summary>
  /// Complete an agent access-token issuance ceremony.
  /// </summary>
  /// <param name="command"></param>
  /// <returns><see cref="Response"/> carrying the bearer access token</returns>
  [HttpPost(Command.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Forbidden)]
  public Task<IActionResult> Process([FromBody] Command command) => Send(command);
}
