namespace BlazorHosted_CSharp.Server.Features.WeatherForecast
{
  using System.Threading.Tasks;
  using BlazorHosted_CSharp.Server.Features.Base;
  using BlazorHosted_CSharp.Api.Features.WeatherForecast;
  using Microsoft.AspNetCore.Mvc;

  [Route(GetWeatherForecastsRequest.Route)]
  public class GetWeatherForecastsController : BaseController<GetWeatherForecastsRequest, GetWeatherForecastsResponse>
  {
    public async Task<IActionResult> Process(GetWeatherForecastsRequest aRequest) => await Send(aRequest);
  }
}