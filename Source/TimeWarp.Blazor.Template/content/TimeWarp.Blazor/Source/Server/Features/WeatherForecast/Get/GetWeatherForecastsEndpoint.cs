namespace TimeWarp.Blazor.Features.WeatherForecasts.Server.GetWeatherForecasts
{
  using Microsoft.AspNetCore.Mvc;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Bases;

  [Route(GetWeatherForecastsRequest.Route)]
  public class GetWeatherForecastsEndpoint : BaseEndpoint<GetWeatherForecastsRequest, GetWeatherForecastsResponse>
  {
    [HttpGet]
    public async Task<IActionResult> Process(GetWeatherForecastsRequest aRequest) => await Send(aRequest);
  }
}
