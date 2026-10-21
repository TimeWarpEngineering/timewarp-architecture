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
// and that it forwards a fake upstream's function call without executing a tool. Also proves the
// validator's image rules (task 303-001) reject each bad image shape, and that null turns, tools,
// and tool calls are rejected without throwing.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.AgentChats
{

  using System.Net.Http.Json;
  using Microsoft.Extensions.FileProviders;
  using Microsoft.Extensions.Hosting;
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
      Handler handler = new(new FixedPrincipal(), upstream, new AgentChatAdmission(), Development());

      OneOf<Response, SharedProblemDetails> result = await handler.Handle(UserTurn(), CancellationToken.None);

      result.IsT0.ShouldBeTrue();
      result.AsT0.ToolCalls.Single().Name.ShouldBe("page_context");
      upstream.Calls.ShouldBe(1);
      upstream.LastCommand!.Tools.ShouldBeEmpty();
    }

    public static async Task Missing_Key_Returns_Setup_Command()
    {
      Handler handler = new(new FixedPrincipal(), new RecordingUpstream(configured: false), new AgentChatAdmission(), Development());

      OneOf<Response, SharedProblemDetails> result = await handler.Handle(UserTurn(), CancellationToken.None);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(503);
      result.AsT1.Detail.ShouldBe(XaiChatDefaults.SetupCommand);
      result.AsT1.Title.ShouldBe("AI not configured");
    }

    public static async Task Missing_Key_Outside_Development_Does_Not_Name_User_Secrets()
    {
      Handler handler = new(
        new FixedPrincipal(),
        new RecordingUpstream(configured: false),
        new AgentChatAdmission(),
        new NamedHostEnvironment("Production"));

      OneOf<Response, SharedProblemDetails> result = await handler.Handle(UserTurn(), CancellationToken.None);

      result.IsT1.ShouldBeTrue();
      string detail = result.AsT1.Detail!;
      detail.ShouldBe(XaiChatDefaults.DeployedSetupHint);
      detail.ShouldNotContain(XaiChatDefaults.UserSecretsId);
      detail.ShouldNotContain("user-secrets");
      detail.ShouldContain("XAI__ApiKey");
    }

    public static async Task Anonymous_Returns_401()
    {
      RecordingUpstream upstream = new(new Response { Text = "nope" });
      Handler handler = new(new FixedPrincipal(null), upstream, new AgentChatAdmission(), Development());

      OneOf<Response, SharedProblemDetails> result = await handler.Handle(UserTurn(), CancellationToken.None);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(401);
      upstream.Calls.ShouldBe(0);
    }

    public static async Task Rate_Cap_Returns_429()
    {
      RecordingUpstream upstream = new(new Response { Text = "ok" });
      using AgentChatAdmission admission = new();
      Handler handler = new(new FixedPrincipal(), upstream, admission, Development());
      for (int index = 0; index < MaxRequestsPerMinute; index++)
      {
        OneOf<Response, SharedProblemDetails> ok = await handler.Handle(UserTurn(), CancellationToken.None);
        ok.IsT0.ShouldBeTrue();
      }

      OneOf<Response, SharedProblemDetails> denied = await handler.Handle(UserTurn(), CancellationToken.None);

      denied.IsT1.ShouldBeTrue();
      denied.AsT1.Status.ShouldBe(429);
    }

    private static NamedHostEnvironment Development() => new("Development");

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

    private sealed class NamedHostEnvironment(string environmentName) : IHostEnvironment
    {
      public string EnvironmentName { get; set; } = environmentName;

      public string ApplicationName { get; set; } = "tests";

      public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

      public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
  }

  [TestTag("Unit")]
  public class CompleteAgentChatValidator_Given_
  {
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 16, 0x4A, 0x46, 0x49];

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<CompleteAgentChatValidator_Given_>();

    public static Task Png_And_Jpeg_On_A_User_Turn_Are_Valid()
    {
      Command command = WithImages(PngImage(), new TurnImage { MediaType = "image/jpeg", Data = Convert.ToBase64String(Jpeg) });

      new Validator().Validate(command).IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task Image_On_A_Non_User_Turn_Is_Rejected()
    {
      Command command = new()
      {
        Messages = [new Turn { Role = "assistant", Text = "Here it is.", Images = [PngImage()] }]
      };

      ShouldReject(command, "Only a user turn can carry images.");
      return Task.CompletedTask;
    }

    public static Task More_Than_Max_Images_Is_Rejected()
    {
      Command command = WithImages([.. Enumerable.Range(0, MaxImagesPerRequest + 1).Select(_ => PngImage())]);

      ShouldReject(command, $"A request can carry at most {MaxImagesPerRequest} images.");
      return Task.CompletedTask;
    }

    public static Task Max_Images_Across_Turns_Is_Counted_Per_Request()
    {
      Command command = new()
      {
        Messages =
        [
          new Turn { Role = "user", Text = "first", Images = [.. Enumerable.Range(0, MaxImagesPerRequest).Select(_ => PngImage())] },
          new Turn { Role = "user", Text = "second", Images = [PngImage()] },
        ]
      };

      ShouldReject(command, $"A request can carry at most {MaxImagesPerRequest} images.");
      return Task.CompletedTask;
    }

    public static Task Disallowed_Media_Type_Is_Rejected()
    {
      Command command = WithImages(new TurnImage { MediaType = "image/gif", Data = Convert.ToBase64String(Png) });

      ShouldReject(command, "An image must be image/png or image/jpeg.");
      return Task.CompletedTask;
    }

    public static Task Data_Over_The_Max_Length_Is_Rejected()
    {
      // Valid base64 of a PNG signature, padded with "A" blocks past MaxImageDataLength.
      string head = Convert.ToBase64String(Png);
      int blocks = (MaxImageDataLength - head.Length) / 4 + 1;
      string data = head + new string('A', blocks * 4);
      data.Length.ShouldBeGreaterThan(MaxImageDataLength);
      Command command = WithImages(new TurnImage { MediaType = "image/png", Data = data });

      new Validator().Validate(command).Errors
        .ShouldContain(error => error.PropertyName.EndsWith(".Data", StringComparison.Ordinal) && error.ErrorCode == "MaximumLengthValidator");
      return Task.CompletedTask;
    }

    public static Task Non_Base64_Data_Is_Rejected()
    {
      Command command = WithImages(new TurnImage { MediaType = "image/png", Data = "not base64!!" });

      ShouldReject(command, "Image data must be base64.");
      return Task.CompletedTask;
    }

    public static Task Null_Images_Are_Rejected()
    {
      Command command = new()
      {
        Messages = [new Turn { Role = "user", Text = "hi", Images = null! }]
      };

      ShouldReject(command, "Images must not be null.");
      return Task.CompletedTask;
    }

    public static Task Null_Image_Element_Is_Rejected()
    {
      Command command = WithImages([null!]);

      ShouldReject(command, "An image must not be null.");
      return Task.CompletedTask;
    }

    public static Task Null_Turn_Is_Rejected()
    {
      Command command = new() { Messages = [null!] };

      ShouldReject(command, "A turn must not be null.");
      return Task.CompletedTask;
    }

    public static Task Null_Tool_Is_Rejected()
    {
      Command command = WithImages(PngImage());
      command.Tools = [null!];

      ShouldReject(command, "A tool must not be null.");
      return Task.CompletedTask;
    }

    public static Task Null_Tool_Call_Is_Rejected()
    {
      Command command = new()
      {
        Messages = [new Turn { Role = "assistant", Text = "Calling.", ToolCalls = [null!] }]
      };

      ShouldReject(command, "A tool call must not be null.");
      return Task.CompletedTask;
    }

    public static Task Bytes_That_Do_Not_Match_The_Media_Type_Are_Rejected()
    {
      Command jpegAsPng = WithImages(new TurnImage { MediaType = "image/png", Data = Convert.ToBase64String(Jpeg) });
      Command pngAsJpeg = WithImages(new TurnImage { MediaType = "image/jpeg", Data = Convert.ToBase64String(Png) });
      Command text = WithImages(new TurnImage { MediaType = "image/png", Data = Convert.ToBase64String("hello, world"u8.ToArray()) });

      ShouldReject(jpegAsPng, "Image bytes must match the declared media type.");
      ShouldReject(pngAsJpeg, "Image bytes must match the declared media type.");
      ShouldReject(text, "Image bytes must match the declared media type.");
      return Task.CompletedTask;
    }

    private static TurnImage PngImage() => new() { MediaType = "image/png", Data = Convert.ToBase64String(Png) };

    private static Command WithImages(params TurnImage[] images) => new()
    {
      Messages = [new Turn { Role = "user", Text = "What is wrong here?", Images = [.. images] }]
    };

    private static void ShouldReject(Command command, string message)
    {
      // Should.NotThrow: a null entry must surface as a validation error, never as an exception (a 500).
      FluentValidation.Results.ValidationResult result = Should.NotThrow(() => new Validator().Validate(command));
      result.IsValid.ShouldBeFalse();
      result.Errors.Select(error => error.ErrorMessage).ShouldContain(message);
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
