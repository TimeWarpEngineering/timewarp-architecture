//#WeatherForecast #GetWeatherForecasts #Request #Grpc
namespace TimeWarp.Blazor.Features.WeatherForecastsGrpc
{
  using System.Runtime.Serialization;

  [DataContract]
  public class GetWeatherForecastsRequest
  {
    [DataMember(Order = 1)]
    public int Days { get; set; }
  }
}
