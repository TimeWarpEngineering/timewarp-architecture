namespace TimeWarp.Architecture.Features.WeatherForecasts;

using OneOf;

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
      WebApiService WebApiService,
      IPublisher Publisher
    ) : BaseHandler<Action>(store)
    {

      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        IApiRequest getWeatherForecastsRequest = new GetWeatherForecasts.Query { Days = action.Days ?? 10 };

        OneOf<GetWeatherForecasts.Response, SharedProblemDetails> response =
          await WebApiService.GetResponse<GetWeatherForecasts.Response>(getWeatherForecastsRequest,cancellationToken);

        response.Switch
        (
          weatherForecasts => WeatherForecastsState.WeatherForecastList = weatherForecasts.WeatherForecasts.ToList(),
          problemDetails => Publisher.Publish(new NotificationState.AddProblemDetails.Action(problemDetails), cancellationToken)
        );
      }
    }
  }
}
