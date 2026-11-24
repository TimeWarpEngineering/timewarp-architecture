#region Purpose
// The Ask dictation stream: the WebSocket path, the audio the browser sends, and the messages the
// web server sends back. Shared by the browser capture and the server relay.
#endregion

#region Design
// Task 505 replaced the batch upload (one base64 clip, POST multipart to xAI /v1/stt) with a
// stream. The browser opens a WebSocket on StreamPath with its session cookie, sends mono PCM16
// little-endian at the sample rate it names in the query string, and sends the text message Done
// when the person stops. The web server relays the audio to xAI's streaming speech-to-text
// (wss://api.x.ai/v1/stt) with the server key, so the key never reaches the browser.
// Server messages are JSON with a type: ready (start sending), partial (the whole transcript so
// far, for the live preview), done (the final transcript), error (one of the ErrorCodes; the
// socket closes after it). The text is the transcript only; an upstream body or key never appears.
// MaxSeconds caps one dictation. The browser stops at it and the server stops relaying audio past
// it. FinalizeSeconds is how long the server waits for the final transcript after Done: measured
// 0.23-0.47 s on 2026-11-24 for 3-20 s clips, so 10 s is a stall, not a slow answer.
// It is not an [ApiEndpoint] contract: a WebSocket is not a request/response operation.
// FakeTranscript is what the Development/Testing fake upstream returns, so a browser proof can look
// for one sentence without a key.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

/// <summary>Ask dictation over a WebSocket.</summary>
public static class TranscribeAgentAudio
{
  /// <summary>WebSocket path on web-server. The query string carries <see cref="SampleRateQuery"/>.</summary>
  public const string StreamPath = "/api/agent-chat/transcriptions/stream";

  /// <summary>Query parameter naming the PCM sample rate in Hz.</summary>
  public const string SampleRateQuery = "sample_rate";

  /// <summary>Longest dictation in seconds. The browser stops at it; the server drops audio past it.</summary>
  public const int MaxSeconds = 300;

  /// <summary>Seconds the server waits for the final transcript after <see cref="ClientMessages.Done"/>.</summary>
  public const int FinalizeSeconds = 10;

  /// <summary>Transcript the fake upstream returns.</summary>
  public const string FakeTranscript = "Voice note.";

  /// <summary>Sample rates xAI streaming speech-to-text accepts for PCM.</summary>
  public static readonly int[] SampleRates = [8000, 16000, 22050, 24000, 44100, 48000];

  /// <summary>True when <paramref name="sampleRate"/> is one of <see cref="SampleRates"/>.</summary>
  public static bool IsSupportedSampleRate(int sampleRate) => Array.IndexOf(SampleRates, sampleRate) >= 0;

  /// <summary>PCM16 mono bytes for <see cref="MaxSeconds"/> at <paramref name="sampleRate"/>.</summary>
  public static long MaxAudioBytes(int sampleRate) => (long)sampleRate * 2 * MaxSeconds;

  /// <summary>Text messages the browser sends. Audio is binary frames.</summary>
  public static class ClientMessages
  {
    /// <summary>The person stopped. The server asks for the final transcript.</summary>
    public const string Done = """{"type":"done"}""";
  }

  /// <summary>The type field of a server message.</summary>
  public static class MessageTypes
  {
    /// <summary>The upstream is ready. Audio sent before it is buffered by the browser.</summary>
    public const string Ready = "ready";

    /// <summary>The whole transcript so far. It may still change.</summary>
    public const string Partial = "partial";

    /// <summary>The final transcript. The socket closes after it.</summary>
    public const string Done = "done";

    /// <summary>The dictation failed. The code is one of <see cref="ErrorCodes"/>.</summary>
    public const string Error = "error";
  }

  /// <summary>Why a dictation failed. The browser maps each code to a sentence.</summary>
  public static class ErrorCodes
  {
    /// <summary>No xAI key is configured.</summary>
    public const string NeedsKey = "needs-key";

    /// <summary>The caller is over the relay's concurrency or rate cap.</summary>
    public const string Busy = "busy";

    /// <summary>The final transcript had no words.</summary>
    public const string NoSpeech = "no-speech";

    /// <summary>The final transcript did not arrive within <see cref="FinalizeSeconds"/>.</summary>
    public const string TimedOut = "timed-out";

    /// <summary>The upstream could not be reached or failed.</summary>
    public const string Failed = "failed";

    /// <summary>The request was not a usable dictation (sample rate, not a WebSocket).</summary>
    public const string BadRequest = "bad-request";

    /// <summary>No signed-in principal.</summary>
    public const string Unauthenticated = "unauthenticated";
  }
}
