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
      private readonly HttpClient HttpClient;

      public FetchWeatherForecastsHandler(IStore aStore, HttpClient aHttpClient) : base(aStore)
      {
        HttpClient = aHttpClient;
      }

      public override async Task<Unit> Handle
      (
        FetchWeatherForecastsAction aFetchWeatherForecastsAction,
        CancellationToken aCancellationToken
      )
      {
        var getWeatherForecastsRequest = new GetWeatherForecastsRequest { Days = 10 };

        GetWeatherForecastsResponse getWeatherForecastsResponse =
          await HttpClient.GetFromJsonAsync<GetWeatherForecastsResponse>
          (
            getWeatherForecastsRequest.GetRoute(), aCancellationToken
          )
          .ConfigureAwait(false);

        WeatherForecastsState._WeatherForecasts = getWeatherForecastsResponse.WeatherForecasts;
        return Unit.Value;
      }
    }
  }
}
