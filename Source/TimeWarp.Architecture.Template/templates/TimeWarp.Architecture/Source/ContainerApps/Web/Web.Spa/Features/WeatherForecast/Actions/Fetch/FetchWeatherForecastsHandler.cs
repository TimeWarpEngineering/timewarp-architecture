namespace TimeWarp.Architecture.Features.WeatherForecasts;

using BlazorState;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.Bases;

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
      IApiRequest getWeatherForecastsRequest = new GetWeatherForecastsRequest { Days = 10 };

      GetWeatherForecastsResponse getWeatherForecastsResponse =
        await WebApiService.GetResponse<GetWeatherForecastsResponse>(getWeatherForecastsRequest)
          .ConfigureAwait(false);

      WeatherForecastsState._WeatherForecasts = getWeatherForecastsResponse.WeatherForecasts;
      return Unit.Value;
    }
  }
}
