namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

/// <summary>
/// Get Weather Forecasts
/// </summary>
/// <remarks>
/// Gets Weather Forecasts for the number of days specified in the request
/// `<see cref="Query.Days"/>`
/// </remarks>
public class GetWeatherForecastsEndpoint : Endpoint<Query, Response>
{
  private readonly IRequestHandler<Query, OneOf<Response, SharedProblemDetails>> Handler;

  public GetWeatherForecastsEndpoint
  (
    IRequestHandler<Query, OneOf<Response, SharedProblemDetails>> aHandler
  )
  {
    Handler = aHandler;
  }

  public override void Configure()
  {
    Get(GetWeatherForecasts.Query.RouteTemplate);
    AllowAnonymous();
    Description
    (
      d => d
        .Produces<Response>(200)
        .ProducesProblem(400)
        .WithTags(FeatureAnnotations.FeatureGroup)
    );
  }

  public override async Task HandleAsync
  (
    Query aQuery,
    CancellationToken aCancellationToken
  )
  {
    OneOf<Response, SharedProblemDetails> result = await Handler.Handle(aQuery, aCancellationToken);

    await result.Match
    (
      response => SendAsync(response, cancellation: aCancellationToken),
      problem =>
      {
        if (!string.IsNullOrEmpty(problem.Detail))
        {
          AddError(problem.Detail);
        }

        return SendErrorsAsync(problem.Status ?? 400, cancellation: aCancellationToken);
      }
    );
  }
}
