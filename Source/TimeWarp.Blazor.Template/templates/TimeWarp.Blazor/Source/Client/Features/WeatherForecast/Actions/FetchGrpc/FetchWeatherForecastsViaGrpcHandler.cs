namespace TimeWarp.Blazor.Features.WeatherForecasts
{
  using BlazorState;
  using MediatR;
  using System.Threading;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Bases;
  using TimeWarp.Blazor.Features.WeatherForecastsGrpc;

  internal partial class WeatherForecastsState
  {
    public class FetchWeatherForecastsViaGrpcHandler : BaseHandler<FetchWeatherForecastsViaGrpcAction>
    {
      private readonly IWeatherForecastService WeatherForecastService;

      public FetchWeatherForecastsViaGrpcHandler(IStore aStore, IWeatherForecastService aWeatherForecastService) : base(aStore)
      {
        WeatherForecastService = aWeatherForecastService;
      }

      public override async Task<Unit> Handle
      (
        FetchWeatherForecastsViaGrpcAction aFetchWeatherForecastsViaGrpcAction,
        CancellationToken aCancellationToken
      )
      {
        WeatherForecastsState._WeatherForecasts.Clear();
        var getWeatherForecastsRequest = new WeatherForecastsGrpc.GetWeatherForecastsRequest { Days = 10 };
        WeatherForecastsGrpc.GetWeatherForecastsResponse getWeatherForecastsResponse =
          await WeatherForecastService.GetWeatherForecastsAsync(getWeatherForecastsRequest);

        foreach (WeatherForecastsGrpc.GetWeatherForecastsResponse.WeatherForecastDto weatherForecastDto in getWeatherForecastsResponse.WeatherForecasts)
        {
          WeatherForecastsState._WeatherForecasts.Add
          (
            new WeatherForecastDto
            (
              weatherForecastDto.Date,
              weatherForecastDto.Summary,
              weatherForecastDto.TemperatureC
            )
          );
        }
        return Unit.Value;
      }
    }
  }
}
