#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/api/projects/api-contracts/api-contracts.csproj
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058

// C-create HostGraphFactory smoke (task 145-002): Web+Api graph, MockAccessTokenProvider
// default wiring + a real BFF->Api authenticated round trip, and per-host configureServices
// override. Binds :7255 and :7000 — serialized.
// Run: dotnet run tests/common/timewarp-testing-tests/host-graph-factory-tests.cs

#region Purpose
// Prove HostGraphFactory C-create Web+Api boot, MockAccessTokenProvider (registration AND
// behavioral bearer-attach through a real Web->Api call), and the per-host override hook.
#endregion

#region Design
// Task 145-002 R2-3: moved out of source/container-apps/api/features (a product slice tree) into
// this suite-shaped Jaribu MTP project — the file tests timewarp-testing infra (HostGraphFactory,
// the Test*ServerApplication family), not an api product feature, and it hard-references BOTH
// Web and Api types, so it cannot live under a single family's (!family) template guard (that was
// R2-2: shipped under api/features guarded only by (!api), broke `--api true --web false`
// generation). This project's csproj is instead excluded whenever EITHER web or api is absent
// (.template.config/template.json's (!web) / (!api) modifiers both list
// tests/common/timewarp-testing-tests/**) — the file only has a coherent home when both exist.
//
// R2-4: Should_Attach_MockAccessTokenProvider_Bearer_On_Real_ApiServerApiService_Call upgrades the
// mock proof from registration-only (ShouldBeOfType) to behavioral — it drives the SAME production
// code path Web.Server uses for BFF calls (ApiServerApiService -> BaseApiService -> HttpApiService)
// with the real DI-registered IAccessTokenProvider, issues a genuine HTTP round trip to the real
// Api.Server host in this HostGraph, and asserts both the attached Authorization header AND the
// real response — not just that a mock type got resolved. Kept dual-mode (standalone `dotnet run`
// + JARIBU_MULTI multi-mode compile here) since the preamble carries over unchanged from its old
// co-located location — the $(SourceDirectory)/$(TestsDirectory) #:project macros resolve the same
// way from any file under this repo (Directory.Build.props auto-imports upward to root regardless
// of the file's directory).
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Testing.HostGraphFactoryTests
{

  using System;
  using System.Linq;
  using System.Net.Http;
  using System.Net.Security;
  using System.Threading;
  using System.Threading.Tasks;
  using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
  using Microsoft.Extensions.DependencyInjection;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Services;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

  /// <summary>Marker registered only when the per-host override runs.</summary>
  internal sealed class OverrideHookMarker;

  /// <summary>Captures the Authorization header actually sent on the wire (behavioral proof, R2-4).</summary>
  internal sealed class CapturingHandler : DelegatingHandler
  {
    public string? LastAuthorizationHeader { get; private set; }

    public CapturingHandler() : base
    (
      new HttpClientHandler
      {
        // Same loopback-only untrusted-cert allowance as TestServerApplication (task 139) — CI
        // runners have no trusted ASP.NET dev certificate.
        ServerCertificateCustomValidationCallback = (message, _, _, errors) =>
          errors == SslPolicyErrors.None || message.RequestUri?.IsLoopback == true
      }
    )
    { }

    protected override async Task<HttpResponseMessage> SendAsync
    (
      HttpRequestMessage request,
      CancellationToken cancellationToken
    )
    {
      LastAuthorizationHeader = request.Headers.Authorization?.ToString();
      return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
  }

  [TestTag("Integration")]
  public class HostGraphFactory_CreateWebWithApi_Given_
  {
    private static HostGraph? Graph;
    private static bool OverrideInvoked;

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<HostGraphFactory_CreateWebWithApi_Given_>();

    public static async Task SetupOnce()
    {
      OverrideInvoked = false;
      Graph = await HostGraphFactory.CreateWebWithApiAsync(
        configureWeb: services =>
        {
          OverrideInvoked = true;
          services.AddSingleton<OverrideHookMarker>();
        });
    }

    public static async Task CleanUpOnce()
    {
      if (Graph is not null)
      {
        await Graph.DisposeAsync();
        Graph = null;
      }
    }

    public static Task Should_Register_MockAccessTokenProvider_On_Web()
    {
      Graph.ShouldNotBeNull();
      Graph.Web.ShouldNotBeNull();
      Graph.Api.ShouldNotBeNull();

      IAccessTokenProvider? provider =
        Graph.Web.WebApplicationHost.ServiceProvider.GetService<IAccessTokenProvider>();
      provider.ShouldNotBeNull();
      provider.ShouldBeOfType<MockAccessTokenProvider>();
      return Task.CompletedTask;
    }

    public static Task Should_Invoke_PerHost_ConfigureServices_Override()
    {
      OverrideInvoked.ShouldBeTrue();
      Graph!.Web!.WebApplicationHost.ServiceProvider.GetService<OverrideHookMarker>()
        .ShouldNotBeNull();
      return Task.CompletedTask;
    }

    /// <summary>
    /// R2-4: behavioral proof. Drives the real Web.Server BFF code path (ApiServerApiService)
    /// with the DI-registered MockAccessTokenProvider against the real Api.Server host in this
    /// graph — asserts the bearer header actually reached the wire AND the round trip succeeded,
    /// not just that DI can resolve a mock type.
    /// </summary>
    public static async Task Should_Attach_MockAccessTokenProvider_Bearer_On_Real_ApiServerApiService_Call()
    {
      Graph.ShouldNotBeNull();
      Graph!.Web.ShouldNotBeNull();
      Graph!.Api.ShouldNotBeNull();

      IAccessTokenProvider accessTokenProvider =
        Graph.Web!.WebApplicationHost.ServiceProvider.GetRequiredService<IAccessTokenProvider>();

      CapturingHandler capturingHandler = new();
      using HttpClient httpClient = new(capturingHandler)
      {
        BaseAddress = Graph.Api!.HttpClient.BaseAddress
      };

      ApiServerApiService apiServerApiService =
        new(httpClient, accessTokenProvider, ContractSerializationDefaults.Options);

      OneOf<Response, FileResponse, SharedProblemDetails> response =
        await apiServerApiService.GetResponse<Response>(new Query { Days = 1 }, CancellationToken.None);

      capturingHandler.LastAuthorizationHeader.ShouldBe("Bearer dummy-token");

      response.Switch
      (
        okResponse => okResponse.WeatherForecasts.Count().ShouldBe(1),
        _ => throw new InvalidOperationException("Expected a Response but received a FileResponse."),
        _ => throw new InvalidOperationException("Expected a Response but received SharedProblemDetails.")
      );
    }
  }

} // namespace TimeWarp.Architecture.Testing.HostGraphFactoryTests
