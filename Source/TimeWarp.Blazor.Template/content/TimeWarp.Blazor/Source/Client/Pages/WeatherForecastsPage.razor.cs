namespace TimeWarp.Blazor.Client.Pages
{
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Client.BaseFeature;
  using static TimeWarp.Blazor.Client.WeatherForecastFeature.WeatherForecastsState;

  public class WeatherForecastsPageBase : BaseComponent
  {
    public const string Route = "/weatherforecasts";

    protected override async Task OnInitializedAsync() =>
      await Mediator.Send(new FetchWeatherForecastsAction());
  }
}
