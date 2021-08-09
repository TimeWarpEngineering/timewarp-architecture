namespace TimeWarp.Blazor.Features.WeatherForecastsGrpc
{
  using System.ServiceModel;
  using System.Threading.Tasks;
  using ProtoBuf.Grpc;
  using TimeWarp.Blazor.Features.WeatherForecastsGrpc;

  [ServiceContract]
  public interface IWeatherForecastService
  {
    [OperationContract]
    Task<GetWeatherForecastsResponse> GetWeatherForecastsAsync
    (
      GetWeatherForecastsRequest aGetWeatherForecastsRequest,
      CallContext aCallContext = default
    );
  }
}
