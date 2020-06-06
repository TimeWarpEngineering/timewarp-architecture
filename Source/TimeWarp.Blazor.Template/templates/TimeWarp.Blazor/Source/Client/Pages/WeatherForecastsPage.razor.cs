namespace TimeWarp.Blazor.Pages
{
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Bases;
  using static TimeWarp.Blazor.Features.WeatherForecasts.WeatherForecastsState;

  public partial class WeatherForecastsPage : BaseComponent
  {
    public const string Route = "/weatherforecasts";

    protected override async Task OnInitializedAsync() =>
      await Mediator.Send(new FetchWeatherForecastsAction());
  }
}
