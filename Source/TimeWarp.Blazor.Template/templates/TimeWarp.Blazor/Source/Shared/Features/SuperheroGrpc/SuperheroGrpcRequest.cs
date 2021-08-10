//#WeatherForecast #GetWeatherForecasts #Request #Grpc
namespace TimeWarp.Blazor.Features.SuperheroGrpc
{
  using ProtoBuf;

  [ProtoContract]
  public class SuperheroGrpcRequest
  {
    [ProtoMember(1)]
    public int NumberOfHero { get; set; }
  }
}
