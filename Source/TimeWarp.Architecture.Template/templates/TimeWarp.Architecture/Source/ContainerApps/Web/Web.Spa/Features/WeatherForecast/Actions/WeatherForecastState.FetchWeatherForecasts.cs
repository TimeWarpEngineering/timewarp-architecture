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
      IApiRequest getWeatherForecastsRequest = new GetWeatherForecasts.Query { Days = 10 };

      GetWeatherForecasts.Response response =
        await WebApiService.GetResponse<GetWeatherForecasts.Response>(getWeatherForecastsRequest);

      WeatherForecastsState._WeatherForecasts = response.WeatherForecasts.ToList();
    }
  }
}
