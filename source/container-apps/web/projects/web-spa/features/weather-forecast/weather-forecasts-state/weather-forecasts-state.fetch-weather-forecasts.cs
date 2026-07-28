#region Purpose
// FetchWeatherForecasts action: loads forecasts from the Api service into state.
#endregion

#region Design
// Built on DefaultApiHandler, which owns validation, transport, and routing failures to the
// toast state — this file supplies only the Query mapping and the success mutation, which is
// the pattern every REST-backed fetch action should copy.
// [TrackAction] lets UI bind loading indicators to the action's in-flight status.
// The 10-day default lives here, not in the contract: it is a client presentation choice.
#endregion

namespace TimeWarp.Architecture.Features.WeatherForecasts;
using static GetWeatherForecasts;
partial class WeatherForecastsState
{
  public static class FetchWeatherForecastsActionSet
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
      ISender sender,
      ILogger<Handler> logger
    ) : DefaultApiHandler<Action,Query,Response>(store, apiServerApiService, sender, logger)
    {
      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        return Task.FromResult<Query?>(new Query { Days = action.Days ?? 10 });
      }
      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        WeatherForecastsState.WeatherForecastList = [.. response.WeatherForecasts];
        return Task.CompletedTask;
      }
    }
  }
}
