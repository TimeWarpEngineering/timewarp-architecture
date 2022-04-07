namespace GetWeatherForecastsEndpoint;

using FluentAssertions;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.WeatherForecasts;
using TimeWarp.Architecture.Testing;

public class Returns
{
  private readonly GetWeatherForecastsRequest GetWeatherForecastsRequest;
  private readonly WebServerApplication TimeWarpBlazorServerApplication;

  public Returns
  (
    WebServerApplication aTimeWarpBlazorServerApplication
  )
  {
    GetWeatherForecastsRequest = new GetWeatherForecastsRequest { Days = 10 };
    TimeWarpBlazorServerApplication = aTimeWarpBlazorServerApplication;
  }

  public async Task _10WeatherForecasts_Given_10DaysRequested()
  {
    GetWeatherForecastsResponse getWeatherForecastsResponse =
      await TimeWarpBlazorServerApplication.GetResponse<GetWeatherForecastsResponse>(GetWeatherForecastsRequest);

    ValidateGetWeatherForecastsResponse(getWeatherForecastsResponse);
  }

  public async Task ValidationError()
  {
    GetWeatherForecastsRequest.Days = -1;

    await TimeWarpBlazorServerApplication.ConfirmEndpointValidationError<GetWeatherForecastsResponse>(GetWeatherForecastsRequest, nameof(GetWeatherForecastsRequest.Days));
  }

  private void ValidateGetWeatherForecastsResponse(GetWeatherForecastsResponse aGetWeatherForecastsResponse)
  {
    aGetWeatherForecastsResponse.CorrelationId.Should().Be(GetWeatherForecastsRequest.CorrelationId);
    aGetWeatherForecastsResponse.WeatherForecasts.Count.Should().Be(GetWeatherForecastsRequest.Days);
  }
}
