namespace GetWeatherForecastsEndpoint
{
  using FluentAssertions;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.WeatherForecasts;
  using TimeWarp.Blazor.Testing;

  public class Returns
  {
    private readonly GetWeatherForecastsRequest GetWeatherForecastsRequest;
    private readonly TimeWarpBlazorServerApplication TimeWarpBlazorServerApplication;

    public Returns
    (
      TimeWarpBlazorServerApplication aTimeWarpBlazorServerApplication
    )
    {
      GetWeatherForecastsRequest = new GetWeatherForecastsRequest { Days = 10 };
      TimeWarpBlazorServerApplication = aTimeWarpBlazorServerApplication;
    }

    public async Task _10WeatherForecasts_Given_10DaysRequested()
    {
      GetWeatherForecastsResponse getWeatherForecastsResponse =
        await TimeWarpBlazorServerApplication.WebApiTestService.GetJsonAsync<GetWeatherForecastsResponse>(GetWeatherForecastsRequest.GetRoute());

      ValidateGetWeatherForecastsResponse(getWeatherForecastsResponse);
    }

    public async Task ValidationError()
    {
      GetWeatherForecastsRequest.Days = -1;

      await TimeWarpBlazorServerApplication.WebApiTestService.ConfirmEndpointValidationError<GetWeatherForecastsResponse>(GetWeatherForecastsRequest.GetRoute(), GetWeatherForecastsRequest, nameof(GetWeatherForecastsRequest.Days));
    }

    private void ValidateGetWeatherForecastsResponse(GetWeatherForecastsResponse aGetWeatherForecastsResponse)
    {
      aGetWeatherForecastsResponse.CorrelationId.Should().Be(GetWeatherForecastsRequest.CorrelationId);
      aGetWeatherForecastsResponse.WeatherForecasts.Count.Should().Be(GetWeatherForecastsRequest.Days);
    }
  }
}
