#region Purpose
// The constants ask-ai.ts needs to stream a dictation, passed from the one C# copy.
#endregion

#region Design
// Built from TranscribeAgentAudio so the browser and the relay cannot disagree on the path, the
// sample rates, the cap, the finalize time, or the message names. Epoch is the insert epoch the
// preview and the final insert must match. JS interop serializes it camelCase.
#endregion

namespace TimeWarp.Architecture.Services;

/// <summary>Options for <c>StartVoiceCapture</c> in ask-ai.ts.</summary>
public sealed class AskVoiceStreamOptions
{
  /// <summary>WebSocket path on the web server.</summary>
  public string Path { get; init; } = TranscribeAgentAudio.StreamPath;

  /// <summary>Query parameter naming the sample rate.</summary>
  public string SampleRateQuery { get; init; } = TranscribeAgentAudio.SampleRateQuery;

  /// <summary>Sample rates the server accepts.</summary>
  public IReadOnlyList<int> SampleRates { get; init; } = TranscribeAgentAudio.SampleRates;

  /// <summary>Longest dictation in seconds.</summary>
  public int MaxSeconds { get; init; } = TranscribeAgentAudio.MaxSeconds;

  /// <summary>Seconds the server waits for the final transcript.</summary>
  public int FinalizeSeconds { get; init; } = TranscribeAgentAudio.FinalizeSeconds;

  /// <summary>Text message that asks for the final transcript.</summary>
  public string DoneMessage { get; init; } = TranscribeAgentAudio.ClientMessages.Done;

  /// <summary>Server message type: ready.</summary>
  public string ReadyType { get; init; } = TranscribeAgentAudio.MessageTypes.Ready;

  /// <summary>Server message type: partial transcript.</summary>
  public string PartialType { get; init; } = TranscribeAgentAudio.MessageTypes.Partial;

  /// <summary>Server message type: final transcript.</summary>
  public string DoneType { get; init; } = TranscribeAgentAudio.MessageTypes.Done;

  /// <summary>Server message type: error.</summary>
  public string ErrorType { get; init; } = TranscribeAgentAudio.MessageTypes.Error;

  /// <summary>Insert epoch the preview and the final insert must match.</summary>
  public int Epoch { get; init; }
}
