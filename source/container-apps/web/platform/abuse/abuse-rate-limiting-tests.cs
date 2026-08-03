#!/usr/bin/env -S dotnet --
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058
#:property DefineConstants=$(DefineConstants);api

// Co-located Jaribu tests for app-level abuse rate limits (task 104-015).
// Run standalone: dotnet run source/container-apps/web/platform/abuse/abuse-rate-limiting-tests.cs

#region Purpose
// Real-host proof: tight per-IP windows yield structured 429 on register + payment challenge paths.
#endregion

#region Design
// HostGraphFactory C-create with PostConfigure AbuseRateLimitOptions (PermitLimit=2) so the suite
// proves rejection without waiting a production minute. Default production limits stay high enough
// not to trip other co-located suites that share localhost. Structured body is application/problem+json
// matching SharedProblemDetails (status/title/detail/policy extension). Edge rate limits are out of
// scope (104-023). Isolated HttpClient per flood call sequence.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Abuse
{

  using System.Net;
  using System.Net.Http.Json;
  using System.Text.Json;
  using Microsoft.Extensions.DependencyInjection;
  using Shouldly;
  using TimeWarp.Architecture.Testing;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Integration")]
  public class AbuseRateLimitingHttp_Given_
  {
    private static HostGraph? Graph;
    private static WebTestServerApplication Web => Graph!.Web!;

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<AbuseRateLimitingHttp_Given_>();

    public static async Task SetupOnce()
    {
#if(api)
      Graph = await HostGraphFactory.CreateWebWithApiAsync(configureWeb: TightenLimits);
#else
      Graph = await HostGraphFactory.CreateWebAsync(configureWeb: TightenLimits);
#endif
    }

    public static async Task CleanUpOnce()
    {
      if (Graph is not null)
      {
        await Graph.DisposeAsync();
        Graph = null;
      }
    }

    public static async Task PrincipalRegistration_Should_Return_Structured_429_After_Limit()
    {
      using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
      const string path = "/api/identity/agent/register/options";

      HttpStatusCode first = (await client.PostAsync(path, content: null)).StatusCode;
      HttpStatusCode second = (await client.PostAsync(path, content: null)).StatusCode;
      using HttpResponseMessage third = await client.PostAsync(path, content: null);

      // First two under limit (may be 200 or domain errors — not 429).
      first.ShouldNotBe(HttpStatusCode.TooManyRequests);
      second.ShouldNotBe(HttpStatusCode.TooManyRequests);
      third.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

      await AssertStructured429(third, AbuseRateLimitingModule.PrincipalRegistrationPolicy);
    }

    public static async Task PaymentChallenge_Should_Return_Structured_429_After_Limit()
    {
      using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
      const string path = "/api/tip";

      HttpStatusCode first = (await client.GetAsync(path)).StatusCode;
      HttpStatusCode second = (await client.GetAsync(path)).StatusCode;
      using HttpResponseMessage third = await client.GetAsync(path);

      first.ShouldNotBe(HttpStatusCode.TooManyRequests);
      second.ShouldNotBe(HttpStatusCode.TooManyRequests);
      third.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

      await AssertStructured429(third, AbuseRateLimitingModule.PaymentChallengePolicy);
    }

    public static async Task UnrelatedRoute_Should_Not_Be_Rate_Limited_By_Abuse_Policies()
    {
      // Health is not on either policy; flood must not 429 from principal/payment limiters.
      using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
      for (int i = 0; i < 8; i++)
      {
        using HttpResponseMessage response = await client.GetAsync("/api/health");
        response.StatusCode.ShouldNotBe(HttpStatusCode.TooManyRequests);
      }
    }

    private static void TightenLimits(IServiceCollection services)
    {
      services.PostConfigure<AbuseRateLimitOptions>(options =>
      {
        options.Enabled = true;
        options.PrincipalRegistration.PermitLimit = 2;
        options.PrincipalRegistration.WindowSeconds = 60;
        options.PrincipalRegistration.SegmentsPerWindow = 6;
        options.PaymentChallenge.PermitLimit = 2;
        options.PaymentChallenge.WindowSeconds = 60;
        options.PaymentChallenge.SegmentsPerWindow = 6;
      });
    }

    private static async Task AssertStructured429(HttpResponseMessage response, string expectedPolicy)
    {
      response.Content.Headers.ContentType.ShouldNotBeNull();
      response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");

      SharedProblemDetails? problem =
        await response.Content.ReadFromJsonAsync<SharedProblemDetails>(ContractSerializationDefaults.Options);
      problem.ShouldNotBeNull();
      problem!.Status.ShouldBe(429);
      problem.Title.ShouldBe("Too Many Requests");
      problem.Detail.ShouldNotBeNullOrWhiteSpace();

      problem.Extensions.ShouldContainKey("policy");
      string? policy = ReadExtensionString(problem.Extensions["policy"]);
      policy.ShouldBe(expectedPolicy);
    }

    private static string? ReadExtensionString(object? value) =>
      value switch
      {
        null => null,
        string s => s,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        JsonElement element => element.ToString(),
        _ => value.ToString(),
      };
  }
}
