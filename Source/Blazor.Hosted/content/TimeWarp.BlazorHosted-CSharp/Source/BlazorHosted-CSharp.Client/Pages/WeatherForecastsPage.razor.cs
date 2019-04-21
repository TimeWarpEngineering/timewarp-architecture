namespace BlazorHosted_CSharp.Client.Pages
{
  using System.Threading.Tasks;
  using BlazorHosted_CSharp.Client.Features.Base.Components;

  public class WeatherForecastsPageModel : BaseComponent
  {
    protected override async Task OnInitAsync() =>
      await Mediator.Send(new Features.WeatherForecast.FetchWeatherForecastsRequest());
  }
}