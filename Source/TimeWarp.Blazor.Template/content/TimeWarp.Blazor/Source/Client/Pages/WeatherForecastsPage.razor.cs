namespace TimeWarp.Blazor.Client.Pages
{
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Client.Features.Base.Components;
  using static TimeWarp.Blazor.Client.Features.WeatherForecast.WeatherForecastsState;

  public class WeatherForecastsPageBase : BaseComponent
  {
    public const string Route = "/weatherforecasts";

    protected override async Task OnInitializedAsync() =>
      await Mediator.Send(new FetchWeatherForecastsAction());
  }
}