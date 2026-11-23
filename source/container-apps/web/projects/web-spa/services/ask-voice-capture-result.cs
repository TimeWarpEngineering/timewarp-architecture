#region Purpose
// What ask-ai.ts returns from a microphone start or stop.
#endregion

#region Design
// JS interop deserializes this from { error, mediaType, data }. Error is a short code
// (denied, unavailable, empty, failed, too-long), not a sentence. Data is base64 with no
// data: prefix. too-long means the clip was dropped because it would pass the size limit.
// The key never travels with it. The server is the only caller of xAI.
#endregion

namespace TimeWarp.Architecture.Services;

/// <summary>One microphone start or stop.</summary>
public sealed class AskVoiceCaptureResult
{
  /// <summary>Null when the call worked. Otherwise denied, unavailable, empty, failed, or too-long.</summary>
  public string? Error { get; set; }

  /// <summary>The recorder's media type, including a codec parameter when the browser set one.</summary>
  public string? MediaType { get; set; }

  /// <summary>Base64 audio. Null until stop.</summary>
  public string? Data { get; set; }
}
