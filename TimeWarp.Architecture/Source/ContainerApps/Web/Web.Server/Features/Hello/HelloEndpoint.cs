namespace TimeWarp.Architecture.Features.Hellos;

using static TimeWarp.Architecture.Features.Hellos.Hello;

public class HelloEndpoint : BaseEndpoint<Query, Response>
{
  /// <summary>
  /// Simple endpoint for testing
  /// </summary>
  /// <param name="query"></param>
  /// <returns></returns>
  /// <returns><see cref="Response"/></returns>
  [HttpGet(Query.Route)]
  [SwaggerOperation(Tags = new[] { FeatureAnnotations.FeatureGroup })]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  public Task<IActionResult> Process([FromQuery] Query query) => Send(query);
}
