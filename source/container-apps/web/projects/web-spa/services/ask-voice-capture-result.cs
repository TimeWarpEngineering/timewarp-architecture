#region Purpose
// What ask-ai.ts returns from a microphone start or stop.
#endregion

#region Design
// JS interop deserializes this from { error, text }. Error is a short code, not a sentence: a
// browser code (denied, unavailable, empty, failed, timed-out) or one of
// TranscribeAgentAudio.ErrorCodes the server sent. Text is the final transcript on a stop that
// worked; start returns no text. The key never travels with it. The server is the only caller of xAI.
#endregion

namespace TimeWarp.Architecture.Services;

/// <summary>One microphone start or stop.</summary>
public sealed class AskVoiceCaptureResult
{
  /// <summary>Null when the call worked. Otherwise a browser code or a server error code.</summary>
  public string? Error { get; set; }

  /// <summary>The final transcript. Null on start and on an error.</summary>
  public string? Text { get; set; }
}
