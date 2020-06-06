namespace TimeWarp.Blazor.Features.WeatherForecasts
{
  using Microsoft.AspNetCore.Mvc;
  using Swashbuckle.AspNetCore.Annotations;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Bases;

  public class GetWeatherForecastsEndpoint : BaseEndpoint<GetWeatherForecastsRequest, GetWeatherForecastsResponse>
  {
    [HttpGet(GetWeatherForecastsRequest.Route)]
    [SwaggerOperation(Tags = new[] { FeatureAnnotations.FeatureGroup })]
    public async Task<IActionResult> Process(GetWeatherForecastsRequest aGetWeatherForecastsRequest) => await Send(aGetWeatherForecastsRequest);
  }
}
