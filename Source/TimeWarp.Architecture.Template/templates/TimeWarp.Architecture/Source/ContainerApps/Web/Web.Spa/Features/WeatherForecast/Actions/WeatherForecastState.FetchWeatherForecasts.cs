namespace TimeWarp.Architecture.Features.WeatherForecasts;

internal partial class WeatherForecastsState
{
  public static class FetchWeatherForecasts
  {
    [TrackAction]
    internal sealed record Action
    (
      int? Days
    ) : BaseAction;

    [UsedImplicitly]
    internal class Handler
    (
      IStore store,
      WebApiService WebApiService
    ) : BaseHandler<Action>(store)
    {

      public override async Task Handle(Action action, CancellationToken aCancellationToken)
      {
        IApiRequest getWeatherForecastsRequest = new GetWeatherForecasts.Query { Days = action.Days ?? 10 };

        GetWeatherForecasts.Response response =
          await WebApiService.GetResponse<GetWeatherForecasts.Response>(getWeatherForecastsRequest) ??
          throw new InvalidOperationException();

        WeatherForecastsState.WeatherForecastList = response.WeatherForecasts.ToList();
      }
    }
  }
}
