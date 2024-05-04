namespace TimeWarp.Architecture.Features.WeatherForecasts;

internal partial class WeatherForecastsState
{
  public static class FetchWeatherForecasts
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
    {
      public int? Days { get; }
      public Action(int? days)
      {
        Days = days;
      }
    }

    [UsedImplicitly]
    internal class Handler
    (
      IStore store,
      IApiServerApiService apiServerApiService,
      ISender sender
    ) : DefaultFetchHandler<Action,GetWeatherForecasts.Response,GetWeatherForecasts.Query>(store, apiServerApiService, sender)
    {
      protected override Task<GetWeatherForecasts.Query?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        return Task.FromResult<GetWeatherForecasts.Query?>(new GetWeatherForecasts.Query { Days = action.Days ?? 10 });
      }
      protected override Task HandleSuccess(GetWeatherForecasts.Response response, CancellationToken cancellationToken)
      {
        WeatherForecastsState.WeatherForecastList = response.WeatherForecasts.ToList();
        return Task.CompletedTask;
      }
    }
  }
}
