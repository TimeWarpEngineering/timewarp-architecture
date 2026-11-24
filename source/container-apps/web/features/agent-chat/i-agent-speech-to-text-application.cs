#region Purpose
// Server port the dictation relay calls. The application layer does not dial xAI itself.
#endregion

#region Design
// Same key slot as IAgentChatUpstream: BeginKeyResolution opens it, IsConfiguredAsync fills it,
// and OpenAsync consumes it, so one dictation does not read the key twice. A missing
// implementation of BeginKeyResolution is a no-op. UseFakeUpstream in Development or Testing
// counts as configured and opens a stream that answers with TranscribeAgentAudio.FakeTranscript.
// OpenAsync returns once the upstream is ready for audio, or a problem. The stream is one
// dictation: audio in, SpeechEvents out, Complete when the person stops.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

/// <summary>Opens a streaming transcription, or reports that no key is configured.</summary>
public interface IAgentSpeechToText
{
  /// <summary>Opens a one-read slot for the configuration check and the open that follow.</summary>
  void BeginKeyResolution()
  {
  }

  /// <summary>True when the Development/Testing fake is on, or a key can be read.</summary>
  Task<bool> IsConfiguredAsync(CancellationToken cancellationToken);

  /// <summary>Sentence for a missing key. The default is the host setup text.</summary>
  string MissingKeyDetail(bool development) => XaiChatDefaults.HostSetupText(development);

  /// <summary>Opens the upstream for PCM16 mono at <paramref name="sampleRate"/>. Does not log the key or the audio.</summary>
  Task<OneOf<IAgentSpeechSession, SharedProblemDetails>> OpenAsync
  (
    int sampleRate,
    CancellationToken cancellationToken
  );
}

/// <summary>One open transcription.</summary>
public interface IAgentSpeechSession : IAsyncDisposable
{
  /// <summary>Forwards one chunk of PCM16 audio.</summary>
  ValueTask SendAudioAsync(ReadOnlyMemory<byte> audio, CancellationToken cancellationToken);

  /// <summary>No more audio. The upstream flushes and sends its final events.</summary>
  ValueTask CompleteAsync(CancellationToken cancellationToken);

  /// <summary>The next event, or null when the upstream closed.</summary>
  ValueTask<SpeechEvent?> ReceiveAsync(CancellationToken cancellationToken);
}

/// <summary>What the upstream said.</summary>
public enum SpeechEventKind
{
  /// <summary>Text that may still change.</summary>
  Interim = 0,

  /// <summary>A locked chunk of the current utterance.</summary>
  ChunkFinal = 1,

  /// <summary>The whole utterance, replacing its chunks.</summary>
  UtteranceFinal = 2,

  /// <summary>All audio is transcribed. The upstream closes after it.</summary>
  Done = 3,

  /// <summary>The upstream failed. Its message is not kept.</summary>
  Error = 4,

  /// <summary>A message this relay does not use.</summary>
  Ignored = 5
}

/// <summary>One upstream event.</summary>
public readonly record struct SpeechEvent(SpeechEventKind Kind, string Text);
