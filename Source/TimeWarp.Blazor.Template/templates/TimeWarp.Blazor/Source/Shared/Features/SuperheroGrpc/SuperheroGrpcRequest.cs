//#WeatherForecast #GetWeatherForecasts #Request #Grpc
namespace TimeWarp.Blazor.Features.SuperheroGrpc
{
  using System.Runtime.Serialization;

  [DataContract]
  public class SuperheroGrpcRequest
  {
    [DataMember(Order = 1)]
    public int NumberOfHero { get; set; }
  }
}
