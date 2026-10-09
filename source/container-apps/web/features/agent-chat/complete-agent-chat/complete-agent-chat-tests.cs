#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008
#:property DefineConstants=$(DefineConstants);api

// Co-located Jaribu tests for the chat relay handler. No xAI key and no network.
// Run standalone: dotnet run source/container-apps/web/features/agent-chat/complete-agent-chat/complete-agent-chat-tests.cs

#region Purpose
// Proves the completion handler refuses an anonymous caller, a missing key, and the rate cap,
// and that it forwards a fake upstream's function call without executing a tool.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.AgentChats
{

  using System.Net.Http.Json;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Features.AgentChats.Application;
  using TimeWarp.Architecture.Testing;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.AgentChats.CompleteAgentChat;
  using Handler = TimeWarp.Architecture.Features.AgentChats.Application.CompleteAgentChat.Handler;

  [TestTag("Unit")]
  public class CompleteAgentChatHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<CompleteAgentChatHandler_Given_>();

    public static async Task Forwards_Tool_Call_And_Does_Not_Invoke_It()
    {
      RecordingUpstream upstream = new(new Response
      {
        ToolCalls =
        [
          new ToolCall { Name = "page_context", CallId = "call-1", ArgumentsJson = "{}" }
        ]
      });
      Handler handler = new(new FixedPrincipal(), upstream, new AgentChatAdmission());

      OneOf<Response, SharedProblemDetails> result = await handler.Handle(UserTurn(), CancellationToken.None);

      result.IsT0.ShouldBeTrue();
      result.AsT0.ToolCalls.Single().Name.ShouldBe("page_context");
      upstream.Calls.ShouldBe(1);
      upstream.LastCommand!.Tools.ShouldBeEmpty();
    }

    public static async Task Missing_Key_Returns_Setup_Command()
    {
      Handler handler = new(new FixedPrincipal(), new RecordingUpstream(configured: false), new AgentChatAdmission());

      OneOf<Response, SharedProblemDetails> result = await handler.Handle(UserTurn(), CancellationToken.None);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(503);
      result.AsT1.Detail.ShouldBe(XaiChatDefaults.SetupCommand);
      result.AsT1.Title.ShouldBe("AI not configured");
    }

    public static async Task Anonymous_Returns_401()
    {
      RecordingUpstream upstream = new(new Response { Text = "nope" });
      Handler handler = new(new FixedPrincipal(null), upstream, new AgentChatAdmission());

      OneOf<Response, SharedProblemDetails> result = await handler.Handle(UserTurn(), CancellationToken.None);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(401);
      upstream.Calls.ShouldBe(0);
    }

    public static async Task Rate_Cap_Returns_429()
    {
      RecordingUpstream upstream = new(new Response { Text = "ok" });
      using AgentChatAdmission admission = new();
      Handler handler = new(new FixedPrincipal(), upstream, admission);
      for (int index = 0; index < MaxRequestsPerMinute; index++)
      {
        OneOf<Response, SharedProblemDetails> ok = await handler.Handle(UserTurn(), CancellationToken.None);
        ok.IsT0.ShouldBeTrue();
      }

      OneOf<Response, SharedProblemDetails> denied = await handler.Handle(UserTurn(), CancellationToken.None);

      denied.IsT1.ShouldBeTrue();
      denied.AsT1.Status.ShouldBe(429);
    }

    private static Command UserTurn() => new()
    {
      Messages = [new Turn { Role = "user", Text = "What is on this page?" }]
    };

    private sealed class FixedPrincipal : ICurrentPrincipalAccessor
    {
      private readonly PrincipalId? Id;

      public FixedPrincipal(PrincipalId? id)
      {
        Id = id;
      }

      public FixedPrincipal() : this(PrincipalId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")))
      {
      }

      public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Id);
    }

    private sealed class RecordingUpstream : IAgentChatUpstream
    {
      private readonly Response? Reply;

      public RecordingUpstream(Response reply)
      {
        Reply = reply;
        IsConfigured = true;
      }

      public RecordingUpstream(bool configured)
      {
        IsConfigured = configured;
      }

      public bool IsConfigured { get; }

      public string? Model => IsConfigured ? XaiChatDefaults.DefaultModel : null;

      public int Calls { get; private set; }

      public Command? LastCommand { get; private set; }

      public Task<OneOf<Response, SharedProblemDetails>> CompleteAsync
      (
        Command command,
        CancellationToken cancellationToken
      )
      {
        Calls++;
        LastCommand = command;
        return Task.FromResult<OneOf<Response, SharedProblemDetails>>(Reply!);
      }
    }
  }

  [TestTag("Integration")]
  public class CompleteAgentChatEndpoint_Given_
  {
    private static HostGraph? Graph;
    private static WebTestServerApplication Web => Graph!.Web!;

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<CompleteAgentChatEndpoint_Given_>();

    public static async Task SetupOnce()
    {
      Graph = await HostGraphFactory.CreateWebAsync();
    }

    public static async Task CleanUpOnce()
    {
      if (Graph is not null)
      {
        await Graph.DisposeAsync();
        Graph = null;
      }
    }

    public static async Task Unauthenticated_Post_Is_401()
    {
      using HttpRequestMessage request = new(HttpMethod.Post, "/api/agent-chat/completions")
      {
        Content = JsonContent.Create(new { messages = new[] { new { role = "user", text = "hi" } } })
      };
      using HttpResponseMessage response = await Web.HttpClient.SendAsync(request);
      ((int)response.StatusCode).ShouldBe(401);
    }

    public static Task Appsettings_Do_Not_Contain_An_Api_Key()
    {
      string root = FindRepoRoot();
      foreach (string path in Directory.EnumerateFiles(root, "appsettings*.json", SearchOption.AllDirectories))
      {
        if (path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
          || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
          continue;
        }

        string text = File.ReadAllText(path);
        text.ShouldNotContain("XAI:ApiKey");
        text.Contains("\"ApiKey\"", StringComparison.Ordinal).ShouldBeFalse(path);
      }

      string wwwroot = Path.Combine(root, "source/container-apps/web/projects/web-spa/wwwroot");
      foreach (string path in Directory.EnumerateFiles(wwwroot, "*.json", SearchOption.AllDirectories))
      {
        File.ReadAllText(path).ShouldNotContain("XAI:ApiKey");
      }

      return Task.CompletedTask;
    }

    private static string FindRepoRoot()
    {
      DirectoryInfo? directory = new(AppContext.BaseDirectory);
      while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "timewarp-architecture.slnx")))
      {
        directory = directory.Parent;
      }

      directory.ShouldNotBeNull();
      return directory.FullName;
    }
  }
}
