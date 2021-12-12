namespace TimeWarp.Blazor.Features.WeatherForecasts
{
  using BlazorState;
  using MediatR;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Bases;

  internal partial class WeatherForecastsState
  {
    public class FetchWeatherForecastsHandler : BaseHandler<FetchWeatherForecastsAction>
    {
      private readonly WebApiService WebApiService;

      public FetchWeatherForecastsHandler(IStore aStore, WebApiService aWebApiService) : base(aStore)
      {
        WebApiService = aWebApiService;
      }

      public override async Task<Unit> Handle
      (
        FetchWeatherForecastsAction aFetchWeatherForecastsAction,
        CancellationToken aCancellationToken
      )
      {
        var getWeatherForecastsRequest = new GetWeatherForecastsRequest { Days = 10 };

        GetWeatherForecastsResponse getWeatherForecastsResponse =
          await WebApiService.GetResponse<GetWeatherForecastsResponse>(getWeatherForecastsRequest)
            .ConfigureAwait(false);

        WeatherForecastsState._WeatherForecasts = getWeatherForecastsResponse.WeatherForecasts;
        return Unit.Value;
      }
    }
  }
}
