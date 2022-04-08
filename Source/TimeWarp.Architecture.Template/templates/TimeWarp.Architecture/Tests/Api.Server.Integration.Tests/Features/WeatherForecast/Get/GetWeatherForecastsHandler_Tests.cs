namespace GetWeatherForecastsHandler;

using FluentAssertions;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.WeatherForecasts;
using TimeWarp.Architecture.Testing;

public class Handle_Returns
{
  private readonly GetWeatherForecastsRequest GetWeatherForecastsRequest;
  private readonly ApiTestServerApplication ApiServerApplication;

  public Handle_Returns
  (
     ApiTestServerApplication aApiServerApplication
  )
  {
    GetWeatherForecastsRequest = new GetWeatherForecastsRequest { Days = 10 };
    ApiServerApplication = aApiServerApplication;
  }

  public async Task _10WeatherForecasts_Given_10DaysRequested()
  {
    GetWeatherForecastsResponse getWeatherForecastsResponse = await ApiServerApplication.Send(GetWeatherForecastsRequest);

    ValidateGetWeatherForecastsResponse(getWeatherForecastsResponse);
  }

  private void ValidateGetWeatherForecastsResponse(GetWeatherForecastsResponse aGetWeatherForecastsResponse)
  {
    aGetWeatherForecastsResponse.CorrelationId.Should().Be(GetWeatherForecastsRequest.CorrelationId);
    aGetWeatherForecastsResponse.WeatherForecasts.Count.Should().Be(GetWeatherForecastsRequest.Days);
  }

}
