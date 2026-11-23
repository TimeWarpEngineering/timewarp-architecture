#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property DefineConstants=$(DefineConstants);api

// Co-located Jaribu tests for dictation. No xAI key and no network.
// Run standalone: dotnet run --file source/container-apps/web/features/agent-chat/transcribe-agent-audio/transcribe-agent-audio-tests.cs

#region Purpose
// Proves the transcription handler refuses an anonymous caller, a missing key, and the rate cap,
// and that it forwards a clip. Proves the xAI request shape (Bearer, model before file) and that
// a mapped problem never copies the provider body.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.AgentChats
{
  using System.Net.Http;
  using System.Text.Json;
  using Microsoft.Extensions.FileProviders;
  using Microsoft.Extensions.Hosting;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Features.AgentChats.Application;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.AgentChats.TranscribeAgentAudio;
  using Handler = TimeWarp.Architecture.Features.AgentChats.Application.TranscribeAgentAudio.Handler;

  [TestTag("Unit")]
  public class TranscribeAgentAudioHandlerGiven
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<TranscribeAgentAudioHandlerGiven>();

    public static async Task ForwardsTheClip()
    {
      RecordingSpeech speech = new();
      using AgentChatAdmission admission = new();
      Handler handler = new(new FixedPrincipal(), speech, admission, Development());

      OneOf<Response, SharedProblemDetails> result = await handler.Handle(Clip(), CancellationToken.None);

      result.IsT0.ShouldBeTrue();
      result.AsT0.Text.ShouldBe(FakeTranscript);
      speech.Calls.ShouldBe(1);
      speech.LastCommand!.MediaType.ShouldBe("audio/webm");
      speech.LastCommand.Data.ShouldBe(Clip().Data);
    }

    public static async Task AnonymousReturns401AndDoesNotCallSpeech()
    {
      RecordingSpeech speech = new();
      using AgentChatAdmission admission = new();
      Handler handler = new(new FixedPrincipal(null), speech, admission, Development());

      OneOf<Response, SharedProblemDetails> result = await handler.Handle(Clip(), CancellationToken.None);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(401);
      speech.Calls.ShouldBe(0);
    }

    public static async Task MissingKeyInDevelopmentReturnsTheSetupCommand()
    {
      RecordingSpeech speech = new(configured: false);
      using AgentChatAdmission admission = new();
      Handler handler = new(new FixedPrincipal(), speech, admission, Development());

      OneOf<Response, SharedProblemDetails> result = await handler.Handle(Clip(), CancellationToken.None);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(503);
      result.AsT1.Detail.ShouldBe(XaiChatDefaults.SetupCommand);
      result.AsT1.Detail!.ShouldNotContain("Admin");
      speech.Calls.ShouldBe(0);
    }

    public static async Task MissingKeyOutsideDevelopmentDoesNotNameUserSecrets()
    {
      RecordingSpeech speech = new(configured: false);
      using AgentChatAdmission admission = new();
      Handler handler = new(
        new FixedPrincipal(),
        speech,
        admission,
        new NamedHostEnvironment("Production"));

      OneOf<Response, SharedProblemDetails> result = await handler.Handle(Clip(), CancellationToken.None);

      result.IsT1.ShouldBeTrue();
      string detail = result.AsT1.Detail!;
      detail.ShouldBe(XaiChatDefaults.DeployedSetupHint);
      detail.ShouldNotContain(XaiChatDefaults.UserSecretsId);
      detail.ShouldNotContain("user-secrets");
      detail.ShouldContain("XAI__ApiKey");
      detail.ShouldNotContain("Admin → AI");
      speech.Calls.ShouldBe(0);
    }

    public static async Task RateCapReturns429()
    {
      RecordingSpeech speech = new();
      using AgentChatAdmission admission = new();
      Handler handler = new(new FixedPrincipal(), speech, admission, Development());
      for (int index = 0; index < CompleteAgentChat.MaxRequestsPerMinute; index++)
      {
        OneOf<Response, SharedProblemDetails> ok = await handler.Handle(Clip(), CancellationToken.None);
        ok.IsT0.ShouldBeTrue();
      }

      OneOf<Response, SharedProblemDetails> denied = await handler.Handle(Clip(), CancellationToken.None);

      denied.IsT1.ShouldBeTrue();
      denied.AsT1.Status.ShouldBe(429);
    }

    private static NamedHostEnvironment Development() => new("Development");

    private static Command Clip() => new()
    {
      MediaType = "audio/webm",
      Data = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 })
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

    private sealed class RecordingSpeech : IAgentSpeechToText
    {
      public RecordingSpeech()
      {
        Configured = true;
      }

      public RecordingSpeech(bool configured)
      {
        Configured = configured;
      }

      private bool Configured { get; }

      public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken)
      {
        _ = cancellationToken;
        return Task.FromResult(Configured);
      }

      public string MissingKeyDetail(bool development) => XaiChatDefaults.HostSetupText(development);

      public int Calls { get; private set; }

      public Command? LastCommand { get; private set; }

      public Task<OneOf<Response, SharedProblemDetails>> TranscribeAsync
      (
        Command command,
        CancellationToken cancellationToken
      )
      {
        _ = cancellationToken;
        Calls++;
        LastCommand = command;
        return Task.FromResult<OneOf<Response, SharedProblemDetails>>(new Response(FakeTranscript));
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
  public class XaiSpeechGiven
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<XaiSpeechGiven>();

    public static async Task RequestPostsSttWithBearerAndFileLast()
    {
      byte[] audio = [1, 2, 3, 4];
      using HttpRequestMessage request = XaiSpeech.CreateRequest(
        "https://api.x.ai/v1/",
        "secret-value",
        "audio/webm;codecs=opus",
        audio);

      request.RequestUri!.ToString().ShouldBe("https://api.x.ai/v1/stt");
      request.RequestUri.ToString().ShouldNotContain("secret-value");
      request.Headers.Authorization!.Scheme.ShouldBe("Bearer");
      request.Headers.Authorization.Parameter.ShouldBe("secret-value");
      string body = await request.Content!.ReadAsStringAsync();
      int modelAt = NameAt(body, "model");
      int fileAt = NameAt(body, "file");
      modelAt.ShouldBeGreaterThanOrEqualTo(0);
      fileAt.ShouldBeGreaterThan(modelAt);
      body.ShouldContain(XaiChatDefaults.SpeechToTextModel);
      body.ShouldContain("audio/webm");
      body.ShouldNotContain("codecs");
    }

    public static Task MapReadsTextAndDropsTheBodyFromProblems()
    {
      OneOf<string, SharedProblemDetails> hello = XaiSpeech.Map(200, """{"text":"hello"}""");
      hello.IsT0.ShouldBeTrue();
      hello.AsT0.ShouldBe("hello");

      OneOf<string, SharedProblemDetails> blank = XaiSpeech.Map(200, """{"text":"  "}""");
      blank.IsT1.ShouldBeTrue();
      blank.AsT1.Status.ShouldBe(422);
      blank.AsT1.Detail.ShouldBe("The recording had no speech.");

      const string secret = "secret-value";
      OneOf<string, SharedProblemDetails> denied = XaiSpeech.Map(401, "{\"error\":\"" + secret + "\"}");
      denied.IsT1.ShouldBeTrue();
      denied.AsT1.Status.ShouldBe(502);
      denied.AsT1.Detail!.ShouldNotContain(secret);

      XaiSpeech.Map(413, secret).AsT1.Status.ShouldBe(413);
      XaiSpeech.Map(429, secret).AsT1.Status.ShouldBe(429);
      XaiSpeech.Map(429, secret).AsT1.Detail!.ShouldNotContain(secret);
      OneOf<string, SharedProblemDetails> broken = XaiSpeech.Map(200, "{");
      broken.IsT1.ShouldBeTrue();
      broken.AsT1.Status.ShouldBe(502);
      return Task.CompletedTask;
    }

    public static async Task ResponseRoundTripsTheTranscript()
    {
      string json = JsonSerializer.Serialize(new Response(FakeTranscript), ContractSerializationDefaults.Options);
      Response? again = JsonSerializer.Deserialize<Response>(json, ContractSerializationDefaults.Options);
      again.ShouldNotBeNull();
      again.Text.ShouldBe(FakeTranscript);
      await Task.CompletedTask;
    }

    private static int NameAt(string body, string name)
    {
      int quoted = body.IndexOf("name=\"" + name + "\"", StringComparison.Ordinal);
      if (quoted >= 0)
      {
        return quoted;
      }

      return body.IndexOf("name=" + name, StringComparison.Ordinal);
    }
  }

  [TestTag("Unit")]
  public class TranscribeAgentAudioValidatorGiven
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<TranscribeAgentAudioValidatorGiven>();

    public static async Task WebmWithACodecAndBase64IsValid()
    {
      Command command = new()
      {
        MediaType = "audio/webm;codecs=opus",
        Data = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 })
      };

      (await new Validator().ValidateAsync(command)).IsValid.ShouldBeTrue();
      CanonicalMediaType(command.MediaType).ShouldBe("audio/webm");
    }

    public static async Task PlainTextIsRejected()
    {
      Command command = new()
      {
        MediaType = "text/plain",
        Data = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 })
      };

      (await new Validator().ValidateAsync(command)).IsValid.ShouldBeFalse();
    }
  }
}
