#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058
#:property DefineConstants=$(DefineConstants);api

// Co-located Jaribu integration test (task 261). Real Web host via HostGraphFactory (C-create) plus
// host-free checks of the environment gate, redactor and fail-closed handler.
// Run standalone: dotnet run source/container-apps/web/features/browser-logs/forward-browser-logs/forward-browser-logs-tests.cs

#region Purpose
// Jaribu runfile proving browser log forwarding: happy path, validation (empty/null entries,
// batch cap, level/source allow-lists, message and page-path caps), rate limit, the
// Development/Testing gate, redaction and the fail-closed Production handler.
#endregion

#region Design
// Host lifetime: HostGraphFactory.CreateWebWithApiAsync when api is present, else CreateWebAsync
// (C-create — fresh graph per class, no process statics), same shape as hello-tests.cs. The in-proc
// host is Development, so the endpoint is live. The rate-limit case runs against that host's
// singleton bucket; it is the only test class that drains it in this file's host. Gate/redactor
// classes are host-free.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.BrowserLogs
{

  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using Microsoft.Extensions.FileProviders;
  using Microsoft.Extensions.Hosting;
  using Microsoft.Extensions.Logging.Abstractions;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Features.BrowserLogs.Application;
  using TimeWarp.Architecture.Testing;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.BrowserLogs.ForwardBrowserLogs;

  internal sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
  {
    public string EnvironmentName { get; set; } = environmentName;
    public string ApplicationName { get; set; } = "Test";
    public string ContentRootPath { get; set; } = "/";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
  }

  internal static class BrowserLogBatches
  {
    public static Command Create(int count, string message = "boom") => new()
    {
      PagePath = "/counter",
      Entries = [.. System.Linq.Enumerable.Range(0, count).Select(_ => new Entry { Level = "error", Source = "console", Message = message })]
    };
  }

  [TestTag("Integration")]
  public class ForwardBrowserLogsEndpoint_Given_
  {
    private static HostGraph? Graph;
    private static WebTestServerApplication Web => Graph!.Web!;

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<ForwardBrowserLogsEndpoint_Given_>();

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

    public static async Task Ok_Given_Valid_Batch()
    {
      OneOf<Response, FileResponse, SharedProblemDetails> response =
        await Web.GetResponse<Response>(BrowserLogBatches.Create(3), CancellationToken.None);

      response.Switch
      (
        ok => ok.Accepted.ShouldBe(3),
        _ => throw new InvalidOperationException("Expected a Response but received a FileResponse."),
        problemDetails => throw new InvalidOperationException(
          $"Expected a Response but received SharedProblemDetails: Status={problemDetails.Status}, Detail={problemDetails.Detail}")
      );
    }

    public static async Task ValidationError_Given_Empty_Entries()
    {
      Command command = new() { Entries = [] };

      await Web.ConfirmEndpointValidationError<Response>(command, nameof(Command.Entries));
    }

    public static async Task ValidationError_Given_Over_Length_Message()
    {
      Command command = BrowserLogBatches.Create(1, new string('x', MaxMessageLength + 1));

      await Web.ConfirmEndpointValidationError<Response>(command, nameof(Entry.Message));
    }

    public static async Task ValidationError_Given_Null_Entries()
    {
      Command command = new() { Entries = null! };

      await Web.ConfirmEndpointValidationError<Response>(command, nameof(Command.Entries));
    }

    public static async Task ValidationError_Given_Over_Size_Batch()
    {
      Command command = BrowserLogBatches.Create(MaxEntriesPerBatch + 1);

      await Web.ConfirmEndpointValidationError<Response>(command, nameof(Command.Entries));
    }

    public static async Task ValidationError_Given_Unknown_Level()
    {
      Command command = BrowserLogBatches.Create(1);
      command.Entries[0].Level = "fatal";

      await Web.ConfirmEndpointValidationError<Response>(command, nameof(Entry.Level));
    }

    public static async Task ValidationError_Given_Unknown_Source()
    {
      Command command = BrowserLogBatches.Create(1);
      command.Entries[0].Source = "network";

      await Web.ConfirmEndpointValidationError<Response>(command, nameof(Entry.Source));
    }

    public static async Task ValidationError_Given_Over_Length_Page_Path()
    {
      Command command = BrowserLogBatches.Create(1);
      command.PagePath = "/" + new string('p', MaxPagePathLength);

      await Web.ConfirmEndpointValidationError<Response>(command, nameof(Command.PagePath));
    }

    public static async Task TooManyRequests_Given_Sustained_Batches()
    {
      int status = 0;

      for (int attempt = 0; attempt < 10 && status != 429; attempt++)
      {
        OneOf<Response, FileResponse, SharedProblemDetails> response =
          await Web.GetResponse<Response>(BrowserLogBatches.Create(MaxEntriesPerBatch), CancellationToken.None);

        if (response.IsT2)
        {
          status = response.AsT2.Status ?? 0;
        }
      }

      status.ShouldBe(429);
    }
  }

  [TestTag("Unit")]
  public class BrowserLogForwarding_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<BrowserLogForwarding_Given_>();

    public static async Task Disabled_Given_Production()
    {
      BrowserLogForwarding.IsEnabled(new TestHostEnvironment(Environments.Production)).ShouldBeFalse();
      await Task.CompletedTask;
    }

    public static async Task Enabled_Given_Development_Or_Testing()
    {
      BrowserLogForwarding.IsEnabled(new TestHostEnvironment(Environments.Development)).ShouldBeTrue();
      BrowserLogForwarding.IsEnabled(new TestHostEnvironment("Testing")).ShouldBeTrue();
      await Task.CompletedTask;
    }

    public static async Task NotFound_Given_Production_Handler()
    {
      Application.ForwardBrowserLogs.Handler handler = new
      (
        new TestHostEnvironment(Environments.Production),
        new BrowserLogRateLimiter(),
        NullLoggerFactory.Instance
      );

      OneOf<Response, SharedProblemDetails> result = await handler.Handle(BrowserLogBatches.Create(1), CancellationToken.None);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(404);
    }
  }

  [TestTag("Unit")]
  public class BrowserLogRedactor_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<BrowserLogRedactor_Given_>();

    public static async Task Redacts_Bearer_Token()
    {
      string redacted = BrowserLogRedactor.Redact("Authorization: Bearer abc.DEF-123_xyz failed");

      redacted.ShouldNotContain("abc.DEF-123_xyz");
      redacted.ShouldContain("[redacted]");
      await Task.CompletedTask;
    }

    public static async Task Redacts_Jwt()
    {
      const string jwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.c2lnbmF0dXJl";
      string redacted = BrowserLogRedactor.Redact($"token={jwt}; done");

      redacted.ShouldNotContain("eyJ");
      redacted.ShouldBe("token=[redacted]; done");
      await Task.CompletedTask;
    }

    public static async Task Redacts_Secret_Query_Parameters()
    {
      string redacted = BrowserLogRedactor.Redact(
        "GET https://host/signin-oidc?code=abc123&state=xyz#access_token=s3cr3t&id_token=t0k failed");

      redacted.ShouldNotContain("abc123");
      redacted.ShouldNotContain("s3cr3t");
      redacted.ShouldNotContain("t0k");
      redacted.ShouldContain("state=xyz");
      redacted.ShouldContain("code=[redacted]");
      await Task.CompletedTask;
    }
  }

} // namespace TimeWarp.Architecture.Features.BrowserLogs
