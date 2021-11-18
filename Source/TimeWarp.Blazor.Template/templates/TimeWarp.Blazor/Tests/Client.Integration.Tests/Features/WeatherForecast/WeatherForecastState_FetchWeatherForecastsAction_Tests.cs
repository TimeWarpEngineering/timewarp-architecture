namespace WeatherForecastsState
{
  using Shouldly;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Client.Integration.Tests.Infrastructure;
  using TimeWarp.Blazor.Features.WeatherForecasts;
  using TimeWarp.Blazor.Testing;
  using static TimeWarp.Blazor.Features.WeatherForecasts.WeatherForecastsState;

  public class FetchWeatherForecastsAction_Should : BaseTest
  {
#pragma warning disable IDE0052 // Remove unread private members It is used simply because injecting it ensures it is constructed.
    private readonly TimeWarpBlazorServerApplication TimeWarpBlazorServerApplication;
#pragma warning restore IDE0052 // Remove unread private members

    private WeatherForecastsState WeatherForecastsState => Store.GetState<WeatherForecastsState>();

    public FetchWeatherForecastsAction_Should
    (
      TestClientApplication aClientHost,
      TimeWarpBlazorServerApplication aTimeWarpBlazorServerApplication
    )
      : base(aClientHost)
    {
      TimeWarpBlazorServerApplication = aTimeWarpBlazorServerApplication;
    }

    public async Task Update_WeatherForecastState_With_WeatherForecasts_From_Server()
    {
      var fetchWeatherForecastsRequest = new FetchWeatherForecastsAction();

      await Send(fetchWeatherForecastsRequest);

      WeatherForecastsState.WeatherForecasts.Count.ShouldBeGreaterThan(0);
    }
  }
}
