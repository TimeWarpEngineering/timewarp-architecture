#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058
#:property DefineConstants=$(DefineConstants);api

// Co-located Jaribu integration test (task 145-004). Real Web host via HostGraphFactory
// CreateWebWithApiAsync (api present) / CreateWebAsync (api absent) (C-create).
// Ported from tests/.../hello/hello-endpoint-tests.cs.
// Run standalone: dotnet run source/container-apps/web/features/hello/hello/hello-tests.cs
//
// DefineConstants=api (task 145-004 R2-1): standalone `dotnet run` needs `api` defined so the
// api-flag-guarded branch below matches this monorepo's real topology (all flags on); generated
// apps never see this property — dotnet-new strips the losing branch's TEXT at generation time,
// independent of DefineConstants (see host-graph-factory.cs Design region).

#region Purpose
// Jaribu runfile proving co-located real-host Hello endpoint integration (happy path + validation)
// and HostGraphFactory C-create Web(+Api) consumption (task 145-004).
#endregion

#region Design
// Host lifetime: HostGraphFactory.CreateWebWithApiAsync when api is present, else CreateWebAsync
// (C-create — fresh graph per class, no process statics). Hello is a Web-only endpoint (this
// file's #:project preamble names only web-contracts/timewarp-testing, no api-contracts) and
// never touches Graph.Api, so it degrades cleanly to a Web-only host under `--api false` (task
// 145-004 R2-1: this file feeds the web JARIBU_MULTI aggregator, which ships regardless of the
// api flag, so it MUST compile+run web-only). SetupOnce stores HostGraph; CleanUpOnce disposes
// reverse-order. Hello is [EndpointAllowAnonymous], so no auth ceremony is required.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Hellos
{

  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Testing;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.Hellos.Hello;

  [TestTag("Integration")]
  public class HelloEndpoint_Given_
  {
    private static HostGraph? Graph;
    private static WebTestServerApplication Web => Graph!.Web!;

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<HelloEndpoint_Given_>();

    public static async Task SetupOnce()
    {
#if(api)
      Graph = await HostGraphFactory.CreateWebWithApiAsync();
#else
      Graph = await HostGraphFactory.CreateWebAsync();
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

    public static async Task Ok_Given_Valid_Request()
    {
      Query query = new() { Name = "Bob" };

      OneOf<Response, FileResponse, SharedProblemDetails> response =
        await Web.GetResponse<Response>(query, CancellationToken.None);

      response.Switch
      (
        ok =>
        {
          ok.ShouldNotBeNull();
          ok.Message.ShouldBe("Hello, Bob!");
        },
        _ => throw new InvalidOperationException("Expected a Response but received a FileResponse."),
        problemDetails => throw new InvalidOperationException(
          $"Expected a Response but received SharedProblemDetails: Status={problemDetails.Status}, Title={problemDetails.Title}, Detail={problemDetails.Detail}")
      );
    }

    public static async Task ValidationError_Given_Empty_Name()
    {
      Query query = new() { Name = "" };

      await Web.ConfirmEndpointValidationError<Response>(query, nameof(Query.Name));
    }
  }

} // namespace TimeWarp.Architecture.Features.Hellos
