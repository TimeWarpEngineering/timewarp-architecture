namespace BlazorHosted_CSharp.Server.Integration.Tests.Features.WeatherForecast.GetAll
{
  using System;
  using System.Threading.Tasks;
  using BlazorHosted_CSharp.Shared.Features.WeatherForecast;
  using MediatR;
  using Microsoft.Extensions.DependencyInjection;
  using Shouldly;

  class GetAllWeatherForecastsTests
  {

    public GetAllWeatherForecastsTests(TestFixture aTestFixture)
    {
      ServiceProvider = aTestFixture.ServiceProvider;
      Mediator = ServiceProvider.GetService<IMediator>();
    }

    private IServiceProvider ServiceProvider { get; }
    private IMediator Mediator { get; }

    public async Task ShouldGetAllWeatherForecasts()
    {
      var getWeatherForecastsRequest = new GetWeatherForecastsRequest { Days = 10 };

      GetWeatherForecastsResponse getWeatherForecastsResponse =
        await Mediator.Send(getWeatherForecastsRequest);

      getWeatherForecastsResponse.WeatherForecasts.Count.ShouldBe(10);

    }
  }
}
