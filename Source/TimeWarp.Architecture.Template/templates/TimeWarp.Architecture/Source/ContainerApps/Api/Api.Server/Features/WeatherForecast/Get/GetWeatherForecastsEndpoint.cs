namespace TimeWarp.Architecture.Features.WeatherForecasts.Server;

using static TimeWarp.Architecture.Features.WeatherForecasts.Contracts.GetWeatherForecasts;
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
  [SwaggerOperation(Tags = new[] { Contracts.FeatureAnnotations.FeatureGroup })]
  [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
  [ProducesResponseType((int)HttpStatusCode.BadRequest)]
  public Task<IActionResult> Process([FromQuery] Query query) =>
    Send(query);
}
