namespace TimeWarp.Architecture.Features.WeatherForecasts.Spa;

internal partial class WeatherForecastsState
{

  [TrackProcessing]
  internal record FetchWeatherForecastsAction : BaseAction { }

  internal class FetchWeatherForecastsHandler : BaseHandler<FetchWeatherForecastsAction>
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
      IApiRequest getWeatherForecastsRequest = new Contracts.GetWeatherForecasts.Query { Days = 10 };

      Contracts.GetWeatherForecasts.Response getWeatherForecastsResponse =
        await WebApiService.GetResponse<Contracts.GetWeatherForecasts.Response>(getWeatherForecastsRequest)
          .ConfigureAwait(false);

      WeatherForecastsState._WeatherForecasts = getWeatherForecastsResponse.WeatherForecasts.ToList();
    }
  }
}
