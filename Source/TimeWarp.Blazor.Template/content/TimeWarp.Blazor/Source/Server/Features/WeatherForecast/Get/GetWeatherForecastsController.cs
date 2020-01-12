namespace TimeWarp.Blazor.Server.Features.WeatherForecast
{
  using Microsoft.AspNetCore.Mvc;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Api.Features.WeatherForecast;
  using TimeWarp.Blazor.Server.Features.Base;

  [Route(GetWeatherForecastsRequest.Route)]
  public class GetWeatherForecastsController : BaseController<GetWeatherForecastsRequest, GetWeatherForecastsResponse>
  {
    [HttpGet]
    public async Task<IActionResult> Process(GetWeatherForecastsRequest aRequest) => await Send(aRequest);
  }
}
