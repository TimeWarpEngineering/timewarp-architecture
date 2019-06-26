namespace BlazorHosted_CSharp.Client.Features.WeatherForecast
{
  using System.Collections.Generic;
  using System.Net.Http;
  using System.Threading;
  using System.Threading.Tasks;
  using BlazorState;
  using BlazorHosted_CSharp.Shared.Features.WeatherForecast;
  using Microsoft.AspNetCore.Components;
  using System.Text.Json.Serialization;

  internal partial class WeatherForecastsState
  {
    public class FetchWeatherForecastsHandler : RequestHandler<FetchWeatherForecastsAction, WeatherForecastsState>
    {
      private readonly JsonSerializerOptions JsonSerializerOptions;
      public FetchWeatherForecastsHandler(
        IStore aStore, 
        HttpClient aHttpClient,
        JsonSerializerOptions aJsonSerializerOptions) : base(aStore)
      {
        HttpClient = aHttpClient;
        JsonSerializerOptions = aJsonSerializerOptions;
      }

      private HttpClient HttpClient { get; }
      private WeatherForecastsState WeatherForecastsState => Store.GetState<WeatherForecastsState>();

      public override async Task<WeatherForecastsState> Handle(
        FetchWeatherForecastsAction aFetchWeatherForecastsRequest,
        CancellationToken aCancellationToken)
      {
        var getWeatherForecastsRequest = new GetWeatherForecastsRequest { Days = 10 };

        using HttpResponseMessage httpResponseMessage = await HttpClient.GetAsync
        (
          $"{GetWeatherForecastsRequest.Route}?days={getWeatherForecastsRequest.Days}"
        );

        string content = await httpResponseMessage.Content.ReadAsStringAsync();

        GetWeatherForecastsResponse getWeatherForecastsResponse =
          JsonSerializer.Parse<GetWeatherForecastsResponse>(content, JsonSerializerOptions);

        List<WeatherForecastDto> weatherForecasts = getWeatherForecastsResponse.WeatherForecasts;
        WeatherForecastsState._WeatherForecasts = weatherForecasts;
        return WeatherForecastsState;
      }
    }
  }
}