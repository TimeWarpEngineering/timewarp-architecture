namespace GetWeatherForecastsEndpoint
{
  using FluentAssertions;
  using Microsoft.AspNetCore.Mvc.Testing;
  using Shouldly;
  using System.Net;
  using System.Net.Http;
  using System.Text.Json;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.WeatherForecasts;
  using TimeWarp.Blazor.Server;
  using TimeWarp.Blazor.Server.Integration.Tests.Infrastructure;

  public class Returns : BaseTest
  {
    private readonly GetWeatherForecastsRequest GetWeatherForecastsRequest;

    public Returns
    (
      WebApplicationFactory<Startup> aWebApplicationFactory,
      JsonSerializerOptions aJsonSerializerOptions
    ) : base(aWebApplicationFactory, aJsonSerializerOptions)
    {
      GetWeatherForecastsRequest = new GetWeatherForecastsRequest { Days = 10 };
    }

    public async Task _10WeatherForecasts_Given_10DaysRequested()
    {
      GetWeatherForecastsResponse getWeatherForecastsResponse =
        await GetJsonAsync<GetWeatherForecastsResponse>(GetWeatherForecastsRequest.RouteFactory);

      ValidateGetWeatherForecastsResponse(getWeatherForecastsResponse);
    }

    public async Task ValidationError()
    {
      GetWeatherForecastsRequest.Days = -1;

      HttpResponseMessage httpResponseMessage = await HttpClient.GetAsync(GetWeatherForecastsRequest.RouteFactory);

      string json = await httpResponseMessage.Content.ReadAsStringAsync();

      httpResponseMessage.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
      json.Should().Contain("errors"); // we are getting errors
      json.Should().Contain(nameof(GetWeatherForecastsRequest.Days));
    }

    private void ValidateGetWeatherForecastsResponse(GetWeatherForecastsResponse aGetWeatherForecastsResponse)
    {
      aGetWeatherForecastsResponse.RequestId.Should().Be(GetWeatherForecastsRequest.Id);
      aGetWeatherForecastsResponse.WeatherForecasts.Count.Should().Be(GetWeatherForecastsRequest.Days);
    }
  }
}
