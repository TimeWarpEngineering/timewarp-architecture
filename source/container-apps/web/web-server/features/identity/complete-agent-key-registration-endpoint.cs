#region Purpose
// HTTP surface for the CompleteAgentKeyRegistration command; delegates all behavior to the mediator pipeline.
#endregion

#region Design
// Anonymous by design — same as StartAgentKeyRegistrationEndpoint: this IS the request that mints
// the agent's Principal; there is no prior session/token to authorize against, and no human sponsor
// is required (task requirement).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using static CompleteAgentKeyRegistration;

public class CompleteAgentKeyRegistrationEndpoint : BaseEndpoint<Command, Response>
{
  /// <summary>
  /// Complete an agent public-key registration ceremony.
  /// </summary>
  /// <param name="command"></param>
  /// <returns><see cref="Response"/> carrying the newly minted PrincipalId and KeyId</returns>
  [HttpPost(Command.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
  public Task<IActionResult> Process([FromBody] Command command) => Send(command);
}
