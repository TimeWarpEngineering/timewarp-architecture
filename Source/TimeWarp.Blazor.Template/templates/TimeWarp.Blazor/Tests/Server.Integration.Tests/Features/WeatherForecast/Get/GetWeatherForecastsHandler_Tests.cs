//namespace GetWeatherForecastsHandler
//{
//  using FluentAssertions;
//  using Microsoft.AspNetCore.Mvc.Testing;
//  using System.Text.Json;
//  using System.Threading.Tasks;
//  using TimeWarp.Blazor.Features.WeatherForecasts;
//  using TimeWarp.Blazor.Server;
//  using TimeWarp.Blazor.Server.Integration.Tests.Infrastructure;

//  public class Handle_Returns
//  {
//    private readonly GetWeatherForecastsRequest GetWeatherForecastsRequest;

//    public Handle_Returns
//    (
//      WebApplicationFactory<Startup> aWebApplicationFactory,
//      JsonSerializerOptions aJsonSerializerOptions
//    ) 
//    {
//      GetWeatherForecastsRequest = new GetWeatherForecastsRequest { Days = 10 };
//    }

//    public async Task _10WeatherForecasts_Given_10DaysRequested()
//    {
//      GetWeatherForecastsResponse getWeatherForecastsResponse = await Send(GetWeatherForecastsRequest);

//      ValidateGetWeatherForecastsResponse(getWeatherForecastsResponse);
//    }

//    private void ValidateGetWeatherForecastsResponse(GetWeatherForecastsResponse aGetWeatherForecastsResponse)
//    {
//      aGetWeatherForecastsResponse.CorrelationId.Should().Be(GetWeatherForecastsRequest.CorrelationId);
//      aGetWeatherForecastsResponse.WeatherForecasts.Count.Should().Be(GetWeatherForecastsRequest.Days);
//    }

//  }
//}
