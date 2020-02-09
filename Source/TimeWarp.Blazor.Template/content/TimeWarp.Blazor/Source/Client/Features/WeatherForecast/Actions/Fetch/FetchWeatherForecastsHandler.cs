namespace TimeWarp.Blazor.WeatherForecastFeature
{
  using BlazorState;
  using MediatR;
  using Microsoft.AspNetCore.Components;
  using System.Net.Http;
  using System.Threading;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Api.Features.WeatherForecast;
  using TimeWarp.Blazor.BaseFeature;

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
          await HttpClient.GetJsonAsync<GetWeatherForecastsResponse>(getWeatherForecastsRequest.RouteFactory);
        WeatherForecastsState._WeatherForecasts = getWeatherForecastsResponse.WeatherForecasts;
        return Unit.Value;
      }
    }
  }
}
