namespace BlazorHosted_CSharp.Client.Features.WeatherForecast
{
  using System.Collections.Generic;
  using System.Reflection;
  using BlazorHosted_CSharp.Shared.Features.WeatherForecast;
  using BlazorState;

  public partial class WeatherForecastsState : State<WeatherForecastsState>
  {
    private List<WeatherForecastDto> _WeatherForecasts;

    public IReadOnlyList<WeatherForecastDto> WeatherForecasts => _WeatherForecasts.AsReadOnly();

    public WeatherForecastsState()
    {
      _WeatherForecasts = new List<WeatherForecastDto>();
    }

    protected WeatherForecastsState(WeatherForecastsState aState) : this()
    {
      foreach (WeatherForecastDto weatherForecastDto in aState.WeatherForecasts)
      {
        _WeatherForecasts.Add(weatherForecastDto.Clone() as WeatherForecastDto);
      }
    }

    public override object Clone() => new WeatherForecastsState(this);

    protected override void Initialize() { }

    private void Initialize(List<WeatherForecastDto> aWeatherForecastList)
    {
      ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
      _WeatherForecasts = aWeatherForecastList ??
        throw new System.ArgumentNullException(nameof(aWeatherForecastList));
    }
  }
}