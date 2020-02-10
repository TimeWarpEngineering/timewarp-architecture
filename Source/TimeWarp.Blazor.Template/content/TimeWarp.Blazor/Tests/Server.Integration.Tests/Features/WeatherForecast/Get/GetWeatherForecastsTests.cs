namespace TimeWarp.Blazor.Features.WeatherForecasts.Tests.Server
{
  using Shouldly;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.WeatherForecasts.Server.GetWeatherForecasts;
  using TimeWarp.Blazor.Server.Integration.Tests.Infrastructure;
  using Microsoft.AspNetCore.Mvc.Testing;
  using System.Text.Json;
  using TimeWarp.Blazor.Server;

  internal class GetWeatherForecastsTests : BaseTest
  {
    private readonly GetWeatherForecastsRequest GetWeatherForecastsRequest;

    public GetWeatherForecastsTests
    (
      WebApplicationFactory<Startup> aWebApplicationFactory,
      JsonSerializerOptions aJsonSerializerOptions
    ) : base(aWebApplicationFactory, aJsonSerializerOptions) 
    {
      GetWeatherForecastsRequest = new GetWeatherForecastsRequest { Days = 10 };
    }

    public async Task FeatureReturnsGetWeatherForecastsResponse()
    {
      GetWeatherForecastsResponse getWeatherForecastsResponse = await Send(GetWeatherForecastsRequest);

      ValidateGetWeatherForecastsResponse(getWeatherForecastsResponse);
    }

    public async Task ApiReturnsGetWeatherForecastsResponse()
    {
      GetWeatherForecastsResponse getWeatherForecastsResponse =
        await GetJsonAsync<GetWeatherForecastsResponse>(GetWeatherForecastsRequest.RouteFactory);

      ValidateGetWeatherForecastsResponse(getWeatherForecastsResponse);
    }

    private void ValidateGetWeatherForecastsResponse(GetWeatherForecastsResponse aGetWeatherForecastsResponse)
    {
      aGetWeatherForecastsResponse.RequestId.ShouldBe(GetWeatherForecastsRequest.Id);
      aGetWeatherForecastsResponse.ResponseId.ShouldNotBeNull();
      aGetWeatherForecastsResponse.WeatherForecasts.Count.ShouldBe(GetWeatherForecastsRequest.Days);
    }
  }
}
