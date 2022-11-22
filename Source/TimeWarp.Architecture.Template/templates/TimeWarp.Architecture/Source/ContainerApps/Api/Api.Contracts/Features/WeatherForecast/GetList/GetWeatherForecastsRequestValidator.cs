namespace TimeWarp.Architecture.Features.WeatherForecasts;

public class GetWeatherForecastsRequestValidator : AbstractValidator<GetWeatherForecastsRequest>
{

  public GetWeatherForecastsRequestValidator()
  {
    RuleFor(aGetWeatherForecastRequest => aGetWeatherForecastRequest.Days)
      .NotEmpty().GreaterThan(0);
  }
}
