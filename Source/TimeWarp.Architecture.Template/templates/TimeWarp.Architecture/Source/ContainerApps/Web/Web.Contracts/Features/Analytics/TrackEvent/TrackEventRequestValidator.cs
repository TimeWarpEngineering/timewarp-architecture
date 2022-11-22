namespace TimeWarp.Architecture.Features.Analytics;

public class TrackEventRequestValidator : AbstractValidator<TrackEventRequest>
{

  public TrackEventRequestValidator()
  {
    RuleFor(aGetWeatherForecastRequest => aGetWeatherForecastRequest.EventName)
      .NotEmpty();
  }
}
