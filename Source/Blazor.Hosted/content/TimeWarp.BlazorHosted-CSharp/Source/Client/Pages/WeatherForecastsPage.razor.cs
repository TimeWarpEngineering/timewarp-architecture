namespace BlazorHosted_CSharp.Client.Pages
{
  using System.Threading.Tasks;
  using BlazorHosted_CSharp.Client.Features.Base.Components;

  public class WeatherForecastsPageBase : BaseComponent
  {
    public const string Route = "/weatherforecasts";

    protected override async Task OnInitializedAsync() =>
      await Mediator.Send(new Features.WeatherForecast.FetchWeatherForecastsAction());
  }
}