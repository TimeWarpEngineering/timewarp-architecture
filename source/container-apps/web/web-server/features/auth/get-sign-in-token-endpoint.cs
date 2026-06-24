namespace TimeWarp.Architecture.Features.Auth;

using static GetSignInToken;

public class GetSignInTokenEndpoint : BaseEndpoint<Query, Response>
{
  [HttpGet(Query.RouteTemplate)]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  public Task<IActionResult> Process([FromQuery] string userId) => Send(new Query { UserId = userId });
}
