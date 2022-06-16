namespace TimeWarp.Architecture.Features.Analytics;

using FluentValidation;

public class TrackEventRequestValidator : AbstractValidator<TrackEventRequest>
{

  public TrackEventRequestValidator()
  {
    RuleFor(aGetWeatherForecastRequest => aGetWeatherForecastRequest.EventName)
      .NotEmpty();
  }
}
