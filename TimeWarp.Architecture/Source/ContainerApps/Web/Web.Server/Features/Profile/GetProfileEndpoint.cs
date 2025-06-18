namespace TimeWarp.Architecture.Features.Profiles;

using static GetProfile;

public sealed class GetProfileEndpoint : BaseEndpoint<Query, Response>
{
  [HttpGet(Query.RouteTemplate)]
  [SwaggerOperation(Tags = [FeatureAnnotations.FeatureGroup])]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  public Task<IActionResult> Process([FromQuery] Query query) => Send(query);
}
