#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/api/projects/api-contracts/api-contracts.csproj
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:package FluentValidation
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058

// Co-located Jaribu in-proc Api tests (tasks 135/145-002/145-005). Replaces the Fixie twins under
// tests/container-apps/api/api-server-integration-tests/features/weather-forecast/get/ (endpoint +
// handler + validator). Real Api host on :7255; serialized with other fixed-port suites.
// Run: dotnet run source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs

#region Purpose
// In-proc lane: HTTP endpoint happy path + validation, mediator Send happy path, and FluentValidation
// unit rules for GetWeatherForecasts (task 145-005 two-lane split).
#endregion

#region Design
// C-create: one HostGraph per host-using class (CreateApiAsync). Validator class is host-free.
// Closed-box OpenAPI coverage lives in suite-shaped api-server-integration-tests (Aspire process
// isolation), not here.
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
  using FluentValidation.Results;
  using FluentValidation.TestHelper;
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

    public static async Task _10DaysRequested_Should_Return10WeatherForecasts_OverHttp()
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

    public static async Task NegativeDays_Should_ReturnValidationError_OverHttp()
    {
      Query query = new() { Days = -1 };

      await Graph!.Api!.ConfirmEndpointValidationError<Response>(query, nameof(Query.Days));
    }

    public static async Task _10DaysRequested_Should_Return10WeatherForecasts_ViaMediatorSend()
    {
      Query query = new() { Days = 10 };

      OneOf<Response, SharedProblemDetails> result = await Graph!.Api!.Send(query);

      result.Switch
      (
        response =>
        {
          response.ShouldNotBeNull();
          response.WeatherForecasts.Count().ShouldBe(10);
        },
        problemDetails => throw new InvalidOperationException(
          $"Expected Response but got SharedProblemDetails: {problemDetails.Title}")
      );
    }
  }

  [TestTag("Unit")]
  public class GetWeatherForecastsValidator_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetWeatherForecastsValidator_Given_>();

    public static Task Be_Valid_Given_PositiveDays()
    {
      Validator validator = new();
      Query query = new() { Days = 5 };

      ValidationResult validationResult = validator.TestValidate(query);

      validationResult.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task HaveError_When_DaysAreNegative()
    {
      Validator validator = new();
      TestValidationResult<Query> result = validator.TestValidate(new Query { Days = -1 });

      result.ShouldHaveValidationErrorFor(query => query.Days);
      return Task.CompletedTask;
    }
  }

} // namespace TimeWarp.Architecture.Features.WeatherForecasts
