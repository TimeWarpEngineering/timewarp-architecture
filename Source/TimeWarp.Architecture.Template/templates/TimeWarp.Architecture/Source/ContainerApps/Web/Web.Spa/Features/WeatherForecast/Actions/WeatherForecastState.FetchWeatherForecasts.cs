namespace TimeWarp.Architecture.Features.WeatherForecasts;

internal partial class WeatherForecastsState
{
  public static class FetchWeatherForecasts
  {

    [TrackProcessing]
    internal record Action : BaseAction { }

    internal class Handler : BaseHandler<Action>
    {
      private readonly WebApiService WebApiService;

      public Handler(IStore store, WebApiService webApiService) : base(store)
      {
        WebApiService = webApiService;
      }

      public override async Task Handle(Action action, CancellationToken aCancellationToken)
      {
        IApiRequest getWeatherForecastsRequest = new GetWeatherForecasts.Query { Days = 10 };

        GetWeatherForecasts.Response response =
          await WebApiService.GetResponse<GetWeatherForecasts.Response>(getWeatherForecastsRequest);

        WeatherForecastsState._WeatherForecasts = response.WeatherForecasts.ToList();
      }
    }
  }
}
