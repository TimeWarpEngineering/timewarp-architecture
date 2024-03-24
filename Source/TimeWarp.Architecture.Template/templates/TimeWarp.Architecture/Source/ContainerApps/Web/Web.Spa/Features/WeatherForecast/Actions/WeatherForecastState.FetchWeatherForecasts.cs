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
      ApiService ApiService,
      IPublisher Publisher
    ) : BaseHandler<Action>(store)
    {

      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        IApiRequest getWeatherForecastsRequest = new GetWeatherForecasts.Query { Days = action.Days ?? 10 };

        OneOf.OneOf<GetWeatherForecasts.Response, SharedProblemDetails> response =
          await ApiService.GetResponse<GetWeatherForecasts.Response>(getWeatherForecastsRequest,cancellationToken);

        response.Switch
        (
          weatherForecasts => WeatherForecastsState.WeatherForecastList = weatherForecasts.WeatherForecasts.ToList(),
          problemDetails => Publisher.Publish(new NotificationState.AddProblemDetails.Action(problemDetails), cancellationToken)
        );
      }
    }
  }
}
