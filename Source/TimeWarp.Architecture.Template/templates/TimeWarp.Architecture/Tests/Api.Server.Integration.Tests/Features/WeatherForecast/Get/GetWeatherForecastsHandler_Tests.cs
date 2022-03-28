namespace GetWeatherForecastsHandler;

using FluentAssertions;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.WeatherForecasts;
using TimeWarp.Architecture.Testing;

public class Handle_Returns
{
  private readonly GetWeatherForecastsRequest GetWeatherForecastsRequest;
  private readonly TimeWarpBlazorServerApplication TimeWarpBlazorServerApplication;

  public Handle_Returns
  (
     TimeWarpBlazorServerApplication aTimeWarpBlazorServerApplication
  )
  {
    GetWeatherForecastsRequest = new GetWeatherForecastsRequest { Days = 10 };
    TimeWarpBlazorServerApplication = aTimeWarpBlazorServerApplication;
  }

  public async Task _10WeatherForecasts_Given_10DaysRequested()
  {
    GetWeatherForecastsResponse getWeatherForecastsResponse = await TimeWarpBlazorServerApplication.Send(GetWeatherForecastsRequest);

    ValidateGetWeatherForecastsResponse(getWeatherForecastsResponse);
  }

  private void ValidateGetWeatherForecastsResponse(GetWeatherForecastsResponse aGetWeatherForecastsResponse)
  {
    aGetWeatherForecastsResponse.CorrelationId.Should().Be(GetWeatherForecastsRequest.CorrelationId);
    aGetWeatherForecastsResponse.WeatherForecasts.Count.Should().Be(GetWeatherForecastsRequest.Days);
  }

}
