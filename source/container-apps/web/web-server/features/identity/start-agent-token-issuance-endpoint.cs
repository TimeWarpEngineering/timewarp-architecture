#region Purpose
// HTTP surface for the StartAgentTokenIssuance command; delegates all behavior to the mediator pipeline.
#endregion

#region Design
// Anonymous by design: this endpoint issues a fresh challenge for a not-yet-authenticated agent —
// nothing to authorize yet.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using static StartAgentTokenIssuance;

public class StartAgentTokenIssuanceEndpoint : BaseEndpoint<Command, Response>
{
  /// <summary>
  /// Start an agent access-token issuance ceremony.
  /// </summary>
  /// <param name="command"></param>
  /// <returns><see cref="Response"/> carrying the token-issuance challenge</returns>
  [HttpPost(Command.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  public Task<IActionResult> Process([FromBody] Command command) => Send(command);
}
