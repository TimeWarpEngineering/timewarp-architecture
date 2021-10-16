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
    private readonly TimeWarpBlazorServerApplication TimeWarpBlazorServerApplication;
    private WeatherForecastsState WeatherForecastsState => Store.GetState<WeatherForecastsState>();

    public FetchWeatherForecastsAction_Should
    (
      ClientHost aClientHost,
      TimeWarpBlazorServerApplication aTimeWarpBlazorServerApplication
    )
      : base(aClientHost)
    {
      TimeWarpBlazorServerApplication = aTimeWarpBlazorServerApplication;
    }

    //[Skip("Want to see what isn't freeing up")]
    public async Task Update_WeatherForecastState_With_WeatherForecasts_From_Server()
    {
      var fetchWeatherForecastsRequest = new FetchWeatherForecastsAction();

      await Send(fetchWeatherForecastsRequest);

      WeatherForecastsState.WeatherForecasts.Count.ShouldBeGreaterThan(0);
    }
  }
}
