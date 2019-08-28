namespace TimeWarp.Blazor.Server.Features.WeatherForecast
{
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Server.Features.Base;
  using TimeWarp.Blazor.Api.Features.WeatherForecast;
  using Microsoft.AspNetCore.Mvc;

  [Route(GetWeatherForecastsRequest.Route)]
  public class GetWeatherForecastsController : BaseController<GetWeatherForecastsRequest, GetWeatherForecastsResponse>
  {
    public async Task<IActionResult> Process(GetWeatherForecastsRequest aRequest) => await Send(aRequest);
  }
}