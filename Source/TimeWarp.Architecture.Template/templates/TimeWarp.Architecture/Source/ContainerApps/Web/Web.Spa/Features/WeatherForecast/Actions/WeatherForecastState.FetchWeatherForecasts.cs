namespace TimeWarp.Architecture.Features.WeatherForecasts.Spa;

internal partial class WeatherForecastsState
{

  [TrackProcessing]
  internal sealed record FetchWeatherForecastsAction(int? Days) : BaseAction { }

  internal sealed class FetchWeatherForecastsHandler : BaseHandler<FetchWeatherForecastsAction>
  {
    private readonly WebApiService WebApiService;

    public FetchWeatherForecastsHandler(IStore aStore, WebApiService aWebApiService) : base(aStore)
    {
      WebApiService = aWebApiService;
    }

    public override async Task Handle
    (
      FetchWeatherForecastsAction aFetchWeatherForecastsAction,
      CancellationToken aCancellationToken
    )
    {
      IApiRequest getWeatherForecastsRequest =
        new Contracts.GetWeatherForecasts.Query { Days = aFetchWeatherForecastsAction.Days ?? 10 };

      Contracts.GetWeatherForecasts.Response response =
        await WebApiService.GetResponse<Contracts.GetWeatherForecasts.Response>(getWeatherForecastsRequest);

      WeatherForecastsState._WeatherForecasts = response.WeatherForecasts.ToList();
    }
  }
}
