namespace GetWeatherForecastsEndpoint;

using FluentAssertions;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.WeatherForecasts;
using TimeWarp.Architecture.Testing;

public class Returns
{
  private readonly GetWeatherForecastsRequest GetWeatherForecastsRequest;
  private readonly ApiTestServerApplication ApiServerApplication;

  public Returns
  (
    ApiTestServerApplication aApiServerApplication
  )
  {
    GetWeatherForecastsRequest = new GetWeatherForecastsRequest { Days = 10 };
    ApiServerApplication = aApiServerApplication;
  }

  public async Task _10WeatherForecasts_Given_10DaysRequested()
  {
    GetWeatherForecastsResponse getWeatherForecastsResponse =
      await ApiServerApplication.GetResponse<GetWeatherForecastsResponse>(GetWeatherForecastsRequest);

    ValidateGetWeatherForecastsResponse(getWeatherForecastsResponse);
  }

  public async Task ValidationError()
  {
    GetWeatherForecastsRequest.Days = -1;

    await ApiServerApplication.ConfirmEndpointValidationError<GetWeatherForecastsResponse>(GetWeatherForecastsRequest, nameof(GetWeatherForecastsRequest.Days));
  }

  private void ValidateGetWeatherForecastsResponse(GetWeatherForecastsResponse aGetWeatherForecastsResponse)
  {
    aGetWeatherForecastsResponse.CorrelationId.Should().Be(GetWeatherForecastsRequest.CorrelationId);
    aGetWeatherForecastsResponse.WeatherForecasts.Count.Should().Be(GetWeatherForecastsRequest.Days);
  }
}
