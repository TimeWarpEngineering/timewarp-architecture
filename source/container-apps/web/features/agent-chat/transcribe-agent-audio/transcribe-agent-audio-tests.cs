#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property DefineConstants=$(DefineConstants);api

// Co-located Jaribu tests for streaming dictation. No xAI key and no network.
// Run standalone: dotnet run --file source/container-apps/web/features/agent-chat/transcribe-agent-audio/transcribe-agent-audio-tests.cs

#region Purpose
// Proves the dictation relay refuses an anonymous caller, a bad sample rate, a missing key, and
// the rate cap; relays audio and streams the transcript; reports a stalled finalize as timed-out,
// an empty transcript as no-speech, and an upstream error as failed; ends quietly when the browser
// leaves; and stops relaying past the length cap. Proves the xAI stream URI, the event reading,
// and the transcript built from the event order xAI was seen to send on 2026-11-24.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.AgentChats
{
  using System.Text.Json;
  using System.Threading.Channels;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Features.AgentChats.Application;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.AgentChats.TranscribeAgentAudio;

  [TestTag("Unit")]
  public class DictationRelayGiven
  {
    private const int Rate = 16000;

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<DictationRelayGiven>();

    public static async Task RefusesAnAnonymousCaller()
    {
      await using ScriptedSpeech speech = new();
      using AgentChatAdmission admission = new();
      ScriptedChannel channel = new();

      await new DictationRelay(new FixedPrincipal(null), speech, admission).RunAsync(channel, Rate, CancellationToken.None);

      channel.Sent.ShouldBe([DictationMessages.Error(ErrorCodes.Unauthenticated)]);
      speech.Opens.ShouldBe(0);
    }

    public static async Task RefusesAnUnsupportedSampleRate()
    {
      await using ScriptedSpeech speech = new();
      using AgentChatAdmission admission = new();
      ScriptedChannel channel = new();

      await new DictationRelay(new FixedPrincipal(), speech, admission).RunAsync(channel, 12345, CancellationToken.None);

      channel.Sent.ShouldBe([DictationMessages.Error(ErrorCodes.BadRequest)]);
      speech.Opens.ShouldBe(0);
    }

    public static async Task RefusesAMissingKeyWithoutDialing()
    {
      await using ScriptedSpeech speech = new() { Configured = false };
      using AgentChatAdmission admission = new();
      ScriptedChannel channel = new();

      await new DictationRelay(new FixedPrincipal(), speech, admission).RunAsync(channel, Rate, CancellationToken.None);

      channel.Sent.ShouldBe([DictationMessages.Error(ErrorCodes.NeedsKey)]);
      speech.Opens.ShouldBe(0);
    }

    public static async Task RefusesWhenTheCapIsFull()
    {
      await using ScriptedSpeech speech = new();
      using AgentChatAdmission admission = new();
      using AgentChatAdmission.AdmissionLease first = await admission.TryAdmitAsync(FixedPrincipal.Key, CancellationToken.None);
      using AgentChatAdmission.AdmissionLease second = await admission.TryAdmitAsync(FixedPrincipal.Key, CancellationToken.None);
      ScriptedChannel channel = new();

      await new DictationRelay(new FixedPrincipal(), speech, admission).RunAsync(channel, Rate, CancellationToken.None);

      channel.Sent.ShouldBe([DictationMessages.Error(ErrorCodes.Busy)]);
      speech.Opens.ShouldBe(0);
    }

    public static async Task RelaysAudioAndStreamsTheTranscript()
    {
      await using ScriptedSpeech speech = new()
      {
        OnAudio = [new SpeechEvent(SpeechEventKind.Interim, "Hel"), new SpeechEvent(SpeechEventKind.ChunkFinal, "Hello")],
        OnComplete = [new SpeechEvent(SpeechEventKind.UtteranceFinal, "Hello there."), new SpeechEvent(SpeechEventKind.Done, "")]
      };
      using AgentChatAdmission admission = new();
      ScriptedChannel channel = new(Audio(4), Audio(6), Done());

      await new DictationRelay(new FixedPrincipal(), speech, admission).RunAsync(channel, Rate, CancellationToken.None);

      channel.Sent.ShouldBe(
      [
        DictationMessages.Ready,
        DictationMessages.Partial("Hel"),
        DictationMessages.Partial("Hello"),
        DictationMessages.Partial("Hello there."),
        DictationMessages.Done("Hello there.")
      ]);
      speech.AudioBytes.ShouldBe(10);
      speech.Completes.ShouldBe(1);
      speech.Disposed.ShouldBeTrue();
    }

    public static async Task ReportsAStalledFinalizeAsTimedOut()
    {
      await using ScriptedSpeech speech = new();
      using AgentChatAdmission admission = new();
      ScriptedChannel channel = new(Audio(4), Done());
      DictationRelay relay = new(new FixedPrincipal(), speech, admission) { FinalizeTimeout = TimeSpan.FromMilliseconds(100) };

      await relay.RunAsync(channel, Rate, CancellationToken.None);

      channel.Sent[^1].ShouldBe(DictationMessages.Error(ErrorCodes.TimedOut));
      speech.Completes.ShouldBe(1);
    }

    public static async Task ReportsAnEmptyTranscriptAsNoSpeech()
    {
      await using ScriptedSpeech speech = new() { OnComplete = [new SpeechEvent(SpeechEventKind.Done, "")] };
      using AgentChatAdmission admission = new();
      ScriptedChannel channel = new(Audio(4), Done());

      await new DictationRelay(new FixedPrincipal(), speech, admission).RunAsync(channel, Rate, CancellationToken.None);

      channel.Sent.ShouldBe([DictationMessages.Ready, DictationMessages.Error(ErrorCodes.NoSpeech)]);
    }

    public static async Task ReportsAnUpstreamErrorAsFailed()
    {
      await using ScriptedSpeech speech = new() { OnAudio = [new SpeechEvent(SpeechEventKind.Error, "")] };
      using AgentChatAdmission admission = new();
      ScriptedChannel channel = new(Audio(4));

      await new DictationRelay(new FixedPrincipal(), speech, admission).RunAsync(channel, Rate, CancellationToken.None);

      channel.Sent.ShouldBe([DictationMessages.Ready, DictationMessages.Error(ErrorCodes.Failed)]);
    }

    public static async Task ReportsAFailedOpenWithItsCode()
    {
      await using ScriptedSpeech speech = new() { OpenProblem = AgentChatProblems.UpstreamFailed() };
      using AgentChatAdmission admission = new();
      ScriptedChannel channel = new(Audio(4));

      await new DictationRelay(new FixedPrincipal(), speech, admission).RunAsync(channel, Rate, CancellationToken.None);

      channel.Sent.ShouldBe([DictationMessages.Error(ErrorCodes.Failed)]);
    }

    public static async Task EndsQuietlyWhenTheBrowserLeaves()
    {
      await using ScriptedSpeech speech = new() { OnAudio = [new SpeechEvent(SpeechEventKind.Interim, "Hel")] };
      using AgentChatAdmission admission = new();
      ScriptedChannel channel = new(Audio(4), Closed());

      await new DictationRelay(new FixedPrincipal(), speech, admission).RunAsync(channel, Rate, CancellationToken.None);

      channel.Sent.ShouldNotContain(message => message.Contains("\"done\"", StringComparison.Ordinal));
      channel.Sent.ShouldNotContain(message => message.Contains("\"error\"", StringComparison.Ordinal));
      speech.Completes.ShouldBe(0);
      speech.Disposed.ShouldBeTrue();
    }

    public static async Task StopsRelayingPastTheLengthCap()
    {
      const int rate = 8000;
      const int frame = 64 * 1024;
      long limit = MaxAudioBytes(rate);
      int frames = (int)(limit / frame) + 3;
      await using ScriptedSpeech speech = new()
      {
        OnComplete = [new SpeechEvent(SpeechEventKind.UtteranceFinal, "Long note."), new SpeechEvent(SpeechEventKind.Done, "")]
      };
      using AgentChatAdmission admission = new();
      ScriptedChannel channel = new([.. Enumerable.Range(0, frames).Select(_ => Audio(frame))]);

      await new DictationRelay(new FixedPrincipal(), speech, admission).RunAsync(channel, rate, CancellationToken.None);

      speech.AudioBytes.ShouldBeLessThanOrEqualTo(limit);
      speech.Completes.ShouldBe(1);
      channel.Sent[^1].ShouldBe(DictationMessages.Done("Long note."));
    }

    private static DictationFrame Audio(int count) => new(DictationFrameKind.Audio, count);

    private static DictationFrame Done() => new(DictationFrameKind.Done, 0);

    private static DictationFrame Closed() => new(DictationFrameKind.Closed, 0);

    private sealed class FixedPrincipal : ICurrentPrincipalAccessor
    {
      internal const string Key = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

      private readonly PrincipalId? Id;

      public FixedPrincipal(PrincipalId? id)
      {
        Id = id;
      }

      public FixedPrincipal() : this(PrincipalId.From(Guid.Parse(Key)))
      {
      }

      public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Id);
    }

    /// <summary>Hands out the scripted frames, then waits until the relay cancels.</summary>
    private sealed class ScriptedChannel(params DictationFrame[] frames) : IDictationChannel
    {
      private readonly Queue<DictationFrame> Frames = new(frames);

      public List<string> Sent { get; } = [];

      public async ValueTask<DictationFrame> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
      {
        if (Frames.TryDequeue(out DictationFrame frame))
        {
          return frame;
        }

        await Task.Delay(Timeout.Infinite, cancellationToken);
        return new DictationFrame(DictationFrameKind.Closed, 0);
      }

      public ValueTask SendAsync(string json, CancellationToken cancellationToken)
      {
        lock (Sent)
        {
          Sent.Add(json);
        }

        return ValueTask.CompletedTask;
      }
    }

    /// <summary>Emits OnAudio after the first audio chunk and OnComplete after complete.</summary>
    private sealed class ScriptedSpeech : IAgentSpeechToText, IAgentSpeechSession
    {
      private readonly Channel<SpeechEvent> Events = Channel.CreateUnbounded<SpeechEvent>();

      public bool Configured { get; init; } = true;

      public SharedProblemDetails? OpenProblem { get; init; }

      public SpeechEvent[] OnAudio { get; init; } = [];

      public SpeechEvent[] OnComplete { get; init; } = [];

      public int Opens { get; private set; }

      public long AudioBytes { get; private set; }

      public int Completes { get; private set; }

      public bool Disposed { get; private set; }

      public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken) => Task.FromResult(Configured);

      public Task<OneOf<IAgentSpeechSession, SharedProblemDetails>> OpenAsync(int sampleRate, CancellationToken cancellationToken)
      {
        Opens++;
        return OpenProblem is null
          ? Task.FromResult<OneOf<IAgentSpeechSession, SharedProblemDetails>>(this)
          : Task.FromResult<OneOf<IAgentSpeechSession, SharedProblemDetails>>(OpenProblem);
      }

      public ValueTask SendAudioAsync(ReadOnlyMemory<byte> audio, CancellationToken cancellationToken)
      {
        if (AudioBytes == 0)
        {
          foreach (SpeechEvent speechEvent in OnAudio)
          {
            Events.Writer.TryWrite(speechEvent);
          }
        }

        AudioBytes += audio.Length;
        return ValueTask.CompletedTask;
      }

      public ValueTask CompleteAsync(CancellationToken cancellationToken)
      {
        Completes++;
        foreach (SpeechEvent speechEvent in OnComplete)
        {
          Events.Writer.TryWrite(speechEvent);
        }

        return ValueTask.CompletedTask;
      }

      public async ValueTask<SpeechEvent?> ReceiveAsync(CancellationToken cancellationToken)
      {
        if (await Events.Reader.WaitToReadAsync(cancellationToken) && Events.Reader.TryRead(out SpeechEvent speechEvent))
        {
          return speechEvent;
        }

        return null;
      }

      public ValueTask DisposeAsync()
      {
        Disposed = true;
        Events.Writer.TryComplete();
        return ValueTask.CompletedTask;
      }
    }
  }

  [TestTag("Unit")]
  public class XaiSpeechGiven
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<XaiSpeechGiven>();

    public static async Task StreamUriIsWssPcmWithInterimResults()
    {
      Uri uri = XaiSpeech.StreamUri(null, 16000);

      uri.ToString().ShouldBe
      (
        "wss://api.x.ai/v1/stt?sample_rate=16000&encoding=pcm&interim_results=true&model=" + XaiChatDefaults.SpeechToTextModel
      );
      await Task.CompletedTask;
    }

    public static async Task StreamUriKeepsAPlainHttpEndpointOnWs()
    {
      Uri uri = XaiSpeech.StreamUri("http://localhost:5099/v1/", 48000);

      uri.Scheme.ShouldBe("ws");
      uri.Authority.ShouldBe("localhost:5099");
      uri.AbsolutePath.ShouldBe("/v1/stt");
      uri.Query.ShouldContain("sample_rate=48000");
      await Task.CompletedTask;
    }

    public static async Task StreamUriRefusesAnUnsupportedRate()
    {
      Should.Throw<ArgumentOutOfRangeException>(() => XaiSpeech.StreamUri(null, 12345));
      await Task.CompletedTask;
    }

    public static async Task ParseReadsEachEventKind()
    {
      XaiSpeech.Parse("""{"type":"transcript.partial","text":"Hel","is_final":false,"speech_final":false}""")
        .ShouldBe(new SpeechEvent(SpeechEventKind.Interim, "Hel"));
      XaiSpeech.Parse("""{"type":"transcript.partial","text":"Hello","is_final":true,"speech_final":false}""")
        .ShouldBe(new SpeechEvent(SpeechEventKind.ChunkFinal, "Hello"));
      XaiSpeech.Parse("""{"type":"transcript.partial","text":"Hello there.","is_final":true,"speech_final":true}""")
        .ShouldBe(new SpeechEvent(SpeechEventKind.UtteranceFinal, "Hello there."));
      XaiSpeech.Parse("""{"type":"transcript.done","text":"","words":[],"duration":3.0}""")
        .ShouldBe(new SpeechEvent(SpeechEventKind.Done, ""));
      XaiSpeech.Parse("""{"type":"transcript.created","id":"x"}""").Kind.ShouldBe(SpeechEventKind.Ignored);
      await Task.CompletedTask;
    }

    public static async Task ParseDropsTheUpstreamErrorMessage()
    {
      XaiSpeech.Parse("""{"type":"error","message":"bad key xai-secret"}""")
        .ShouldBe(new SpeechEvent(SpeechEventKind.Error, ""));
      XaiSpeech.Parse("not json").Kind.ShouldBe(SpeechEventKind.Error);
      await Task.CompletedTask;
    }

    public static async Task IsCreatedMatchesOnlyTheReadyEvent()
    {
      XaiSpeech.IsCreated("""{"type":"transcript.created","id":"x"}""").ShouldBeTrue();
      XaiSpeech.IsCreated("""{"type":"error","message":"no"}""").ShouldBeFalse();
      XaiSpeech.IsCreated("nope").ShouldBeFalse();
      await Task.CompletedTask;
    }
  }

  [TestTag("Unit")]
  public class DictationTranscriptGiven
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<DictationTranscriptGiven>();

    public static async Task BuildsTheTextInTheOrderXaiSends()
    {
      DictationTranscript transcript = new();
      transcript.Apply(new SpeechEvent(SpeechEventKind.Interim, "I want to unders-")).ShouldBeTrue();
      transcript.Text.ShouldBe("I want to unders-");
      transcript.Apply(new SpeechEvent(SpeechEventKind.Interim, "I want to understand why the voice input times out."));
      transcript.Apply(new SpeechEvent(SpeechEventKind.ChunkFinal, "I want to understand why the voice"));
      transcript.Apply(new SpeechEvent(SpeechEventKind.Interim, "input times out. Please"));
      transcript.Text.ShouldBe("I want to understand why the voice input times out. Please");
      transcript.Apply(new SpeechEvent(SpeechEventKind.ChunkFinal, "input times out. Please look."));
      transcript.Apply(new SpeechEvent(SpeechEventKind.UtteranceFinal, "I want to understand why the voice input times out. Please look."));
      transcript.Text.ShouldBe("I want to understand why the voice input times out. Please look.");
      transcript.Apply(new SpeechEvent(SpeechEventKind.Interim, "Then list"));
      transcript.Text.ShouldBe("I want to understand why the voice input times out. Please look. Then list");
      transcript.Apply(new SpeechEvent(SpeechEventKind.Done, "")).ShouldBeTrue();
      transcript.Text.ShouldBe("I want to understand why the voice input times out. Please look.");
      await Task.CompletedTask;
    }

    public static async Task UsesDoneTextOnlyWhenNothingElseArrived()
    {
      DictationTranscript empty = new();
      empty.Apply(new SpeechEvent(SpeechEventKind.Done, "Short."));
      empty.Text.ShouldBe("Short.");

      DictationTranscript heard = new();
      heard.Apply(new SpeechEvent(SpeechEventKind.UtteranceFinal, "Heard."));
      heard.Apply(new SpeechEvent(SpeechEventKind.Done, "Other."));
      heard.Text.ShouldBe("Heard.");
      await Task.CompletedTask;
    }
  }

  [TestTag("Unit")]
  public class DictationMessagesGiven
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<DictationMessagesGiven>();

    public static async Task WritesTheTypesTheBrowserReads()
    {
      using var ready = JsonDocument.Parse(DictationMessages.Ready);
      ready.RootElement.GetProperty("type").GetString().ShouldBe(MessageTypes.Ready);

      using var partial = JsonDocument.Parse(DictationMessages.Partial("Say \"hi\""));
      partial.RootElement.GetProperty("type").GetString().ShouldBe(MessageTypes.Partial);
      partial.RootElement.GetProperty("text").GetString().ShouldBe("Say \"hi\"");

      using var done = JsonDocument.Parse(DictationMessages.Done("End."));
      done.RootElement.GetProperty("type").GetString().ShouldBe(MessageTypes.Done);

      using var error = JsonDocument.Parse(DictationMessages.Error(ErrorCodes.TimedOut));
      error.RootElement.GetProperty("type").GetString().ShouldBe(MessageTypes.Error);
      error.RootElement.GetProperty("code").GetString().ShouldBe(ErrorCodes.TimedOut);
      await Task.CompletedTask;
    }

    public static async Task ClientDoneIsTheTextTheRelayMatches()
    {
      using var done = JsonDocument.Parse(ClientMessages.Done);
      done.RootElement.GetProperty("type").GetString().ShouldBe("done");
      await Task.CompletedTask;
    }
  }
}
