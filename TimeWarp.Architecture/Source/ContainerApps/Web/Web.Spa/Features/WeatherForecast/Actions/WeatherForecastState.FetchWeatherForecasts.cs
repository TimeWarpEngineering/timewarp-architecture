namespace TimeWarp.Architecture.Features.WeatherForecasts;
using static GetWeatherForecasts;
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

    internal class Handler
    (
      IStore store,
      IApiServerApiService apiServerApiService,
      ISender sender
    ) : DefaultApiHandler<Action,Query,Response>(store, apiServerApiService, sender)
    {
      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        return Task.FromResult<Query?>(new Query { Days = action.Days ?? 10 });
      }
      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        WeatherForecastsState.WeatherForecastList = response.WeatherForecasts.ToList();
        return Task.CompletedTask;
      }
    }
  }
}
