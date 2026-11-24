#region Purpose
// Sentences the Ask microphone shows. The Playwright proof reads the same constants.
#endregion

#region Design
// The dictation socket reports a short code (TranscribeAgentAudio.ErrorCodes, or a browser code
// from ask-ai.ts), never a key or an upstream body. ForCode maps each code to one sentence here,
// so the composer never shows the host setup command. TimedOut is task 505's case: the final
// transcript did not arrive in time, which is not the same as a generic failure. The dictation
// cap (TranscribeAgentAudio.MaxSeconds) is not an error: the transcript so far is inserted.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Ask microphone copy.</summary>
public static class AskVoiceCopy
{
  /// <summary>Idle button name.</summary>
  public const string Dictate = "Dictate a prompt";

  /// <summary>Recording button name.</summary>
  public const string Stop = "Stop recording";

  /// <summary>Button name and status while the clip is in flight.</summary>
  public const string Transcribing = "Transcribing";

  /// <summary>Error button name.</summary>
  public const string Retry = "Try voice input again";

  /// <summary>The browser refused the microphone.</summary>
  public const string Denied = "Microphone access was denied.";

  /// <summary>This browser cannot capture audio.</summary>
  public const string Unavailable = "This browser has no microphone.";

  /// <summary>Stop produced no bytes.</summary>
  public const string Empty = "The recording was empty.";

  /// <summary>Capture threw before a clip existed.</summary>
  public const string StartFailed = "The recording could not be started.";

  /// <summary>Speech-to-text failed, or the socket closed before the transcript.</summary>
  public const string Failed = "The recording could not be transcribed.";

  /// <summary>The transcript came back and the composer was gone.</summary>
  public const string InsertFailed = "The transcript could not be inserted.";

  /// <summary>The server has no xAI key.</summary>
  public const string NeedsKey = "Voice input needs an xAI key.";

  /// <summary>The final transcript did not arrive within the finalize time.</summary>
  public const string TimedOut = "The recording took too long to transcribe. Try a shorter one.";

  /// <summary>The final transcript had no words.</summary>
  public const string NoSpeech = "The recording had no speech.";

  /// <summary>The caller is over the relay's cap.</summary>
  public const string Busy = "Too many voice requests. Wait a moment and try again.";

  /// <summary>The sentence for a start or stop error code.</summary>
  public static string ForCode(string? code) => code switch
  {
    "denied" => Denied,
    "unavailable" => Unavailable,
    "empty" => Empty,
    TranscribeAgentAudio.ErrorCodes.NeedsKey => NeedsKey,
    TranscribeAgentAudio.ErrorCodes.Busy => Busy,
    TranscribeAgentAudio.ErrorCodes.NoSpeech => NoSpeech,
    TranscribeAgentAudio.ErrorCodes.TimedOut => TimedOut,
    _ => Failed
  };
}
