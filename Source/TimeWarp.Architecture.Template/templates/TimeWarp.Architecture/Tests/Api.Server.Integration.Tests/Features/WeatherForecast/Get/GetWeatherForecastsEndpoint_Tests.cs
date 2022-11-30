namespace GetWeatherForecastsEndpoint_;

using FluentAssertions;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.WeatherForecasts;
using TimeWarp.Architecture.Testing;

public class Returns
{
  private readonly GetWeatherForecastsRequest GetWeatherForecastsRequest;
  private readonly ApiTestServerApplication ApiTestServerApplication;

  public Returns
  (
    ApiTestServerApplication aApiTestServerApplication
  )
  {
    GetWeatherForecastsRequest = new GetWeatherForecastsRequest { Days = 10 };
    ApiTestServerApplication = aApiTestServerApplication;
  }

  public async Task _10WeatherForecasts_Given_10DaysRequested()
  {
    GetWeatherForecastsResponse getWeatherForecastsResponse =
      await ApiTestServerApplication.GetResponse<GetWeatherForecastsResponse>(GetWeatherForecastsRequest);

    ValidateGetWeatherForecastsResponse(getWeatherForecastsResponse);
  }

  public async Task ValidationError()
  {
    GetWeatherForecastsRequest.Days = -1;

    await ApiTestServerApplication.ConfirmEndpointValidationError<GetWeatherForecastsResponse>(GetWeatherForecastsRequest, nameof(GetWeatherForecastsRequest.Days));
  }

  private void ValidateGetWeatherForecastsResponse(GetWeatherForecastsResponse aGetWeatherForecastsResponse)
  {
    aGetWeatherForecastsResponse.WeatherForecasts.Count.Should().Be(GetWeatherForecastsRequest.Days);
  }
}
