namespace TimeWarp.Blazor.Features.WeatherForecast
{
  using Microsoft.AspNetCore.Mvc;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.WeatherForecast;
  using TimeWarp.Blazor.Features.Base;

  [Route(GetWeatherForecastsRequest.Route)]
  public class GetWeatherForecastsEndpoint : BaseEndpoint<GetWeatherForecastsRequest, GetWeatherForecastsResponse>
  {
    [HttpGet]
    public async Task<IActionResult> Process(GetWeatherForecastsRequest aRequest) => await Send(aRequest);
  }
}
