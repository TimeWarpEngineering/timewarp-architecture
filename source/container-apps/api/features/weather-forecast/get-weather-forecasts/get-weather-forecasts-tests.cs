#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/api/projects/api-contracts/api-contracts.csproj
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058

// Co-located Jaribu integration test (task 135, ported from the task 134 spike requirement 2,
// the PRIMARY case). Duplicates
// tests/container-apps/api/api-server-integration-tests/features/weather-forecast/get/
// get-weather-forecasts-endpoint-tests.cs — a real ApiTestServerApplication host (fixed port
// 7255) exercised over real HTTP through FastEndpoints and the real mediator pipeline, not a
// host-free unit test. Ensure no other process is bound to :7255 before running (same
// fixed-port / serialized-execution constraint as the Fixie suite it duplicates).
// Run standalone: dotnet run source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs

#region Purpose
// Jaribu runfile proving a co-located, real-host integration test: happy path + validation
// rejection through the real mediator/FastEndpoints pipeline (task 135; task 134 spike
// requirement 2).
#endregion

#region Design
// Shared host lifetime uses Jaribu class-scoped SetupOnce / CleanUpOnce (TimeWarp.Jaribu
// ≥ 1.0.0-beta.14; timewarp-jaribu#19 / jaribu task 029). SetupOnce runs before the first
// test that actually executes; CleanUpOnce disposes the host after the last test so the fixed
// port is released before process exit — required for JARIBU_MULTI aggregators and safer than
// the pre-beta.14 Lazy-never-dispose workaround. Spinning up WebApplication.CreateBuilder +
// FastEndpoints + Mediator per test would multiply the host's ~1s+ startup cost by the test
// count for zero isolation benefit (this endpoint has no mutable state).
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

// Jaribu's SUT_/Action_Given_ naming convention requires underscores in type/member names
// (CA1707, via the NoWarn preamble above); it also requires a block-scoped namespace (top-level
// statements above cannot be followed by a file-scoped namespace — hence IDE0161 is in the same
// NoWarn list). Both are structural to the multi-mode runfile template, not a style lapse — see
// create-role-tests.cs (task 135) for the same rationale.
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
    private static ApiTestServerApplication? Host;

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetWeatherForecastsEndpoint_Given_>();

    public static Task SetupOnce()
    {
      Host = new ApiTestServerApplication();
      return Task.CompletedTask;
    }

    public static async Task CleanUpOnce()
    {
      if (Host is not null)
      {
        await Host.DisposeAsync();
        Host = null;
      }
    }

    public static async Task _10DaysRequested_Should_Return10WeatherForecasts()
    {
      Query query = new() { Days = 10 };

      OneOf<Response, FileResponse, SharedProblemDetails> response =
        await Host!.GetResponse<Response>(query, CancellationToken.None);

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

      await Host!.ConfirmEndpointValidationError<Response>(query, nameof(Query.Days));
    }
  }

} // namespace TimeWarp.Architecture.Features.WeatherForecasts
