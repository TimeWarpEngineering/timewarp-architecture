namespace GetWeatherForecastsHandler;

using FluentAssertions;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.WeatherForecasts;
using TimeWarp.Architecture.Testing;

public class Handle_Returns
{
  private readonly GetWeatherForecastsRequest GetWeatherForecastsRequest;
  private readonly ApiTestServerApplication ApiTestServerApplication;

  public Handle_Returns
  (
     ApiTestServerApplication aApiTestServerApplication
  )
  {
    GetWeatherForecastsRequest = new GetWeatherForecastsRequest { Days = 10 };
    ApiTestServerApplication = aApiTestServerApplication;
  }

  public async Task _10WeatherForecasts_Given_10DaysRequested()
  {
    GetWeatherForecastsResponse getWeatherForecastsResponse = await ApiTestServerApplication.Send(GetWeatherForecastsRequest);

    ValidateGetWeatherForecastsResponse(getWeatherForecastsResponse);
  }

  private void ValidateGetWeatherForecastsResponse(GetWeatherForecastsResponse aGetWeatherForecastsResponse)
  {
    aGetWeatherForecastsResponse.CorrelationId.Should().Be(GetWeatherForecastsRequest.CorrelationId);
    aGetWeatherForecastsResponse.WeatherForecasts.Count.Should().Be(GetWeatherForecastsRequest.Days);
  }

}
