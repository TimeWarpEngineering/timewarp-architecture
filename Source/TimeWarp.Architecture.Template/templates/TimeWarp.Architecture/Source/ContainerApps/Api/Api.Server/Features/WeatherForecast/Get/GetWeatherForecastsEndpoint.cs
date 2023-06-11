namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;
public class GetWeatherForecastsEndpoint : BaseEndpoint<Query, Response>
{
  /// <summary>
  /// Get Weather Forecasts
  /// </summary>
  /// <remarks>
  /// Gets Weather Forecasts for the number of days specified in the request
  /// `<see cref="Query.Days"/>`
  /// </remarks>
  /// <param name="query"></param>
  /// <returns><see cref="Response"/></returns>
  [HttpGet(Query.Route)]
  [SwaggerOperation(Tags = new[] { FeatureAnnotations.FeatureGroup })]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType((int)HttpStatusCode.BadRequest)]
  public Task<IActionResult> Process([FromQuery] Query query) =>
    Send(query);
}
