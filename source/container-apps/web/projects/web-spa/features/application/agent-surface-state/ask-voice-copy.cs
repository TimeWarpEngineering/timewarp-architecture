#region Purpose
// Sentences the Ask microphone shows. The Playwright proof reads the same constants.
#endregion

#region Design
// The server problems stay short and never include a key or an upstream body. A 503 is replaced
// with the key sentence so the composer does not paste the host setup command into the button.
// Any other detail is shown only when it is already a short sentence.
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

  /// <summary>Speech-to-text failed, or the status detail is too long to show.</summary>
  public const string Failed = "The recording could not be transcribed.";

  /// <summary>The transcript came back and the composer was gone.</summary>
  public const string InsertFailed = "The transcript could not be inserted.";

  /// <summary>The server has no xAI key.</summary>
  public const string NeedsKey = "Voice input needs an xAI key.";
}
