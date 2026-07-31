#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/api/projects/api-contracts/api-contracts.csproj
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058

// C-create HostGraphFactory smoke (task 145-002): Web+Api graph, MockAccessTokenProvider
// default wiring, and per-host configureServices override. Binds :7255 and :7000 — serialized.
// Run: dotnet run source/container-apps/api/features/host-graph/host-graph-factory-tests.cs

#region Purpose
// Prove HostGraphFactory C-create Web+Api boot, MockAccessTokenProvider, and override hook.
#endregion

#region Design
// Lives under api/features so api-jaribu-tests globs it (timewarp-testing ProjectReference).
// Full Web+Api is intentional — Web's built-in callback registers MockAccessTokenProvider;
// override hook is proven by a PostConfigure marker service.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.HostGraph
{

  using System.Threading.Tasks;
  using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
  using Microsoft.Extensions.DependencyInjection;
  using Shouldly;
  using TimeWarp.Architecture.Services;
  using TimeWarp.Architecture.Testing;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  /// <summary>Marker registered only when the per-host override runs.</summary>
  internal sealed class OverrideHookMarker;

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
  }

} // namespace TimeWarp.Architecture.Features.HostGraph
