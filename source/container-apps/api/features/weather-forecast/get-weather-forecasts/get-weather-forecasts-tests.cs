#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/api/projects/api-contracts/api-contracts.csproj
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058

// Co-located Jaribu integration test (task 135, C-create via HostGraphFactory task 145-002).
// Duplicates tests/container-apps/api/api-server-integration-tests/features/weather-forecast/get/
// get-weather-forecasts-endpoint-tests.cs — real Api host on :7255 through FastEndpoints + mediator.
// Ensure no other process is bound to :7255 before running (serialized with Fixie suites).
// Run standalone: dotnet run source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs

#region Purpose
// Jaribu runfile proving co-located real-host integration (happy path + validation) and the
// HostGraphFactory C-create Api-only consumption shape (task 145-002).
#endregion

#region Design
// Host lifetime: HostGraphFactory.CreateApiAsync (C-create — fresh graph per class, no process
// statics). SetupOnce stores HostGraph; CleanUpOnce disposes reverse-order (Api only here).
// Jaribu SetupOnce/CleanUpOnce (beta.14+) are class-scoped; lifetime matches real Fixie
// per-class ServiceProvider behavior (task 143).
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.WeatherForecasts
{

  using System;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Testing;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

  [TestTag("Integration")]
  public class GetWeatherForecastsEndpoint_Given_
  {
    private static HostGraph? Graph;

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetWeatherForecastsEndpoint_Given_>();

    public static async Task SetupOnce()
    {
      Graph = await HostGraphFactory.CreateApiAsync();
    }

    public static async Task CleanUpOnce()
    {
      if (Graph is not null)
      {
        await Graph.DisposeAsync();
        Graph = null;
      }
    }

    public static async Task _10DaysRequested_Should_Return10WeatherForecasts()
    {
      Query query = new() { Days = 10 };

      OneOf<Response, FileResponse, SharedProblemDetails> response =
        await Graph!.Api!.GetResponse<Response>(query, CancellationToken.None);

      response.Switch
      (
        okResponse => okResponse.WeatherForecasts.Count().ShouldBe(10),
        _ => throw new InvalidOperationException("Expected a Response but received a FileResponse."),
        _ => throw new InvalidOperationException("Expected a Response but received SharedProblemDetails.")
      );
    }

    public static async Task NegativeDays_Should_ReturnValidationError()
    {
      Query query = new() { Days = -1 };

      await Graph!.Api!.ConfirmEndpointValidationError<Response>(query, nameof(Query.Days));
    }
  }

} // namespace TimeWarp.Architecture.Features.WeatherForecasts
