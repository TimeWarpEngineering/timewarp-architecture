namespace TimeWarp.Blazor.Pages
{
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Bases;
  using static TimeWarp.Blazor.Features.WeatherForecasts.WeatherForecastsState;

  public partial class WeatherForecastsGrpcPage : BaseComponent
  {
    private const string RouteTemplate = "/WeatherForecastsGrpc";

    public static string GetRoute() => RouteTemplate;

    protected override async Task OnInitializedAsync() =>
      await Send(new FetchWeatherForecastsViaGrpcAction()).ConfigureAwait(false);
  }
}
