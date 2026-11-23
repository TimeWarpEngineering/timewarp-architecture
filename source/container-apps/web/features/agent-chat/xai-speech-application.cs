#region Purpose
// Builds the xAI speech-to-text request and maps its HTTP result. No network.
#endregion

#region Design
// POST {endpoint}/stt, Authorization: Bearer, multipart. model is written before file because
// the speech-to-text guide ignores fields that follow file on a streamable upload. Container
// audio (webm from the browser) is not given audio_format; the server detects the container.
// Map never copies the response body into a problem, so a refusal that echoes the key stays
// off the client. A 2xx with no text is "no speech", not a success.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using System.Net.Http.Headers;
using System.Text.Json;
using static TimeWarp.Architecture.Features.AgentChats.TranscribeAgentAudio;

/// <summary>xAI speech-to-text request and response mapping.</summary>
public static class XaiSpeech
{
  private static readonly JsonSerializerOptions Json = new()
  {
    PropertyNameCaseInsensitive = true
  };

  /// <summary>POST multipart for one clip. The caller disposes the message.</summary>
  public static HttpRequestMessage CreateRequest
  (
    string? endpoint,
    string apiKey,
    string mediaType,
    byte[] audio
  )
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
    ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
    ArgumentNullException.ThrowIfNull(audio);
    string canonical = CanonicalMediaType(mediaType)
      ?? throw new ArgumentException("MediaType is not a supported audio type.", nameof(mediaType));
    string root = string.IsNullOrWhiteSpace(endpoint)
      ? XaiChatDefaults.DefaultEndpoint
      : endpoint.TrimEnd('/');
    HttpRequestMessage request = new(HttpMethod.Post, root + XaiChatDefaults.SpeechToTextPath);
    bool armed = false;
    try
    {
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
      MultipartFormDataContent? form = new();
      StringContent? model = null;
      ByteArrayContent? file = null;
      try
      {
        model = new StringContent(XaiChatDefaults.SpeechToTextModel);
        form.Add(model, "model");
        model = null;
        file = new ByteArrayContent(audio);
        file.Headers.ContentType = new MediaTypeHeaderValue(canonical);
        form.Add(file, "file", FileName(canonical));
        file = null;
        request.Content = form;
        form = null;
      }
      finally
      {
        model?.Dispose();
        file?.Dispose();
        form?.Dispose();
      }

      armed = true;
      return request;
    }
    finally
    {
      if (!armed)
      {
        request.Dispose();
      }
    }
  }

  /// <summary>Transcript, or a problem whose text does not include <paramref name="body"/>.</summary>
  public static OneOf<string, SharedProblemDetails> Map(int status, string? body)
  {
    if (status is >= 200 and <= 299)
    {
      if (TryReadText(body, out string? text))
      {
        if (string.IsNullOrWhiteSpace(text))
        {
          return NoSpeech();
        }

        return text;
      }

      return UpstreamFailed();
    }

    if (status == 400)
    {
      return BadAudio();
    }

    if (status == 413)
    {
      return TooLong();
    }

    if (status == 429)
    {
      return AgentChatProblems.TooManyRequests();
    }

    return UpstreamFailed();
  }

  /// <summary>2xx with an empty transcript.</summary>
  public static SharedProblemDetails NoSpeech() => new()
  {
    Title = "No speech recognized",
    Status = 422,
    Detail = "The recording had no speech."
  };

  /// <summary>The provider rejected the clip.</summary>
  public static SharedProblemDetails BadAudio() => new()
  {
    Title = "Recording rejected",
    Status = 400,
    Detail = "The recording could not be transcribed."
  };

  /// <summary>The clip is over the provider's size limit.</summary>
  public static SharedProblemDetails TooLong() => new()
  {
    Title = "Recording too long",
    Status = 413,
    Detail = "The recording is too long."
  };

  /// <summary>The provider failed, or the success body was not a transcript.</summary>
  public static SharedProblemDetails UpstreamFailed() => new()
  {
    Title = "Voice request failed",
    Status = 502,
    Detail = "The recording could not be transcribed."
  };

  private static bool TryReadText(string? body, out string? text)
  {
    text = null;
    if (string.IsNullOrWhiteSpace(body))
    {
      return true;
    }

    try
    {
      TranscriptBody? parsed = JsonSerializer.Deserialize<TranscriptBody>(body, Json);
      text = parsed?.Text;
      return true;
    }
    catch (JsonException)
    {
      return false;
    }
  }

  private static string FileName(string canonical) => canonical switch
  {
    "audio/mpeg" or "audio/mp3" => "dictation.mp3",
    "audio/wav" or "audio/wave" or "audio/x-wav" => "dictation.wav",
    "audio/mp4" or "audio/m4a" or "audio/x-m4a" => "dictation.m4a",
    "audio/ogg" or "audio/opus" => "dictation.ogg",
    "audio/flac" => "dictation.flac",
    "audio/aac" => "dictation.aac",
    _ => "dictation.webm"
  };

  private sealed class TranscriptBody
  {
    public string? Text { get; set; }
  }
}
