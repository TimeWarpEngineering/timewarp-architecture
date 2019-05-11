namespace BlazorHosted_CSharp.Client.Features.WeatherForecast
{
  using BlazorHosted_CSharp.Shared.Features.WeatherForecast;
  using BlazorState;
  
  public partial class WeatherForecastsState : State<WeatherForecastsState>
  {
    private WeatherForecastsState(WeatherForecastsState aState) : this()
    {
      foreach (WeatherForecastDto weatherForecastDto in aState.WeatherForecasts)
      {
        _WeatherForecasts.Add(weatherForecastDto.Clone() as WeatherForecastDto);
      }
    }

    //public override object Clone() => new WeatherForecastsState(this);

    protected override void Initialize() { }
  }
}