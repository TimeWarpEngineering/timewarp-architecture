namespace TimeWarp.Architecture.Features.Hello;

public class HelloEndpoint : BaseEndpoint<HelloRequest, HelloResponse>
{
  /// <summary>
  /// Simple endpoint for testing
  /// </summary>
  /// <param name="aHelloRequest"><see cref="HelloRequest"/></param>
  /// <returns><see cref="HelloResponse"/></returns>
  [HttpGet(HelloRequest.RouteTemplate)]
  [SwaggerOperation(Tags = new[] { FeatureAnnotations.FeatureGroup })]
  [ProducesResponseType(typeof(HelloResponse), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  public Task<IActionResult> Process([FromQuery] HelloRequest aHelloRequest) => Send(aHelloRequest);

}
