#region Purpose
// Builds the xAI streaming speech-to-text URI and reads its events. No network.
#endregion

#region Design
// wss://api.x.ai/v1/stt (docs.x.ai stt-streaming.ws.json, checked 2026-11-24). The endpoint
// option is the https API root; its scheme becomes wss (http becomes ws for a local fake). Query:
// sample_rate, encoding=pcm (signed 16-bit little-endian), interim_results=true for the live
// preview, and the model. Auth is the Authorization: Bearer header the server sets.
// Upstream events: transcript.created (ready), transcript.partial with is_final and speech_final,
// transcript.done, error. Observed on 2026-11-24: an interim partial carries the text of the
// current chunk so far, a chunk final locks that chunk, an utterance final (speech_final) repeats
// the whole utterance, and transcript.done arrives after audio.done with empty text. So the
// transcript is built here (DictationTranscript), not read from transcript.done. Done text is used
// only when nothing else arrived. An error event's message is not kept, so it cannot reach a log
// or the browser.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using System.Text;
using System.Text.Json;

/// <summary>xAI streaming speech-to-text URI and event reading.</summary>
public static class XaiSpeech
{
  /// <summary>Upstream type sent once the session can take audio.</summary>
  public const string CreatedType = "transcript.created";

  /// <summary>Text message that asks the upstream to flush and finish.</summary>
  public const string AudioDoneMessage = """{"type":"audio.done"}""";

  /// <summary>Streaming URI for PCM16 mono at <paramref name="sampleRate"/>.</summary>
  public static Uri StreamUri(string? endpoint, int sampleRate)
  {
    if (!TranscribeAgentAudio.IsSupportedSampleRate(sampleRate))
    {
      throw new ArgumentOutOfRangeException(nameof(sampleRate), sampleRate, "Sample rate is not supported.");
    }

    string root = string.IsNullOrWhiteSpace(endpoint)
      ? XaiChatDefaults.DefaultEndpoint
      : endpoint.TrimEnd('/');
    string socketRoot;
    if (root.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
    {
      socketRoot = "ws://" + root["http://".Length..];
    }
    else if (root.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
      socketRoot = "wss://" + root["https://".Length..];
    }
    else
    {
      socketRoot = root;
    }

    string query =
      "?sample_rate=" + sampleRate.ToString(System.Globalization.CultureInfo.InvariantCulture)
      + "&encoding=pcm&interim_results=true&model=" + Uri.EscapeDataString(XaiChatDefaults.SpeechToTextModel);
    return new Uri(socketRoot + XaiChatDefaults.SpeechToTextPath + query);
  }

  /// <summary>True when <paramref name="json"/> is the transcript.created event.</summary>
  public static bool IsCreated(string json) => TypeOf(json) == CreatedType;

  /// <summary>Reads one upstream event. Malformed JSON is an error.</summary>
  public static SpeechEvent Parse(string json)
  {
    try
    {
      using var document = JsonDocument.Parse(json);
      JsonElement root = document.RootElement;
      if (root.ValueKind != JsonValueKind.Object)
      {
        return new SpeechEvent(SpeechEventKind.Error, string.Empty);
      }

      string? type = StringProperty(root, "type");
      string text = StringProperty(root, "text") ?? string.Empty;
      return type switch
      {
        "transcript.partial" when BoolProperty(root, "is_final") && BoolProperty(root, "speech_final") =>
          new SpeechEvent(SpeechEventKind.UtteranceFinal, text),
        "transcript.partial" when BoolProperty(root, "is_final") => new SpeechEvent(SpeechEventKind.ChunkFinal, text),
        "transcript.partial" => new SpeechEvent(SpeechEventKind.Interim, text),
        "transcript.done" => new SpeechEvent(SpeechEventKind.Done, text),
        "error" => new SpeechEvent(SpeechEventKind.Error, string.Empty),
        _ => new SpeechEvent(SpeechEventKind.Ignored, string.Empty)
      };
    }
    catch (JsonException)
    {
      return new SpeechEvent(SpeechEventKind.Error, string.Empty);
    }
  }

  private static string? TypeOf(string json)
  {
    try
    {
      using var document = JsonDocument.Parse(json);
      return document.RootElement.ValueKind == JsonValueKind.Object
        ? StringProperty(document.RootElement, "type")
        : null;
    }
    catch (JsonException)
    {
      return null;
    }
  }

  private static string? StringProperty(JsonElement element, string name) =>
    element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
      ? value.GetString()
      : null;

  private static bool BoolProperty(JsonElement element, string name) =>
    element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.True;
}

/// <summary>Builds one dictation's text from upstream events.</summary>
public sealed class DictationTranscript
{
  private readonly List<string> Utterances = [];
  private readonly List<string> Chunks = [];
  private string Interim = string.Empty;
  private string DoneText = string.Empty;

  /// <summary>Applies one event. Returns true when the visible text changed.</summary>
  public bool Apply(SpeechEvent speechEvent)
  {
    string before = Text;
    switch (speechEvent.Kind)
    {
      case SpeechEventKind.Interim:
        Interim = speechEvent.Text;
        break;
      case SpeechEventKind.ChunkFinal:
        Chunks.Add(speechEvent.Text);
        Interim = string.Empty;
        break;
      case SpeechEventKind.UtteranceFinal:
        Utterances.Add(speechEvent.Text);
        Chunks.Clear();
        Interim = string.Empty;
        break;
      case SpeechEventKind.Done:
        Interim = string.Empty;
        DoneText = speechEvent.Text;
        break;
      default:
        return false;
    }

    return !string.Equals(before, Text, StringComparison.Ordinal);
  }

  /// <summary>Utterances, then locked chunks, then the interim text, one space apart.</summary>
  public string Text
  {
    get
    {
      StringBuilder builder = new();
      foreach (string part in Utterances)
      {
        Append(builder, part);
      }

      foreach (string part in Chunks)
      {
        Append(builder, part);
      }

      Append(builder, Interim);
      if (builder.Length == 0)
      {
        Append(builder, DoneText);
      }

      return builder.ToString();
    }
  }

  private static void Append(StringBuilder builder, string part)
  {
    string trimmed = part.Trim();
    if (trimmed.Length == 0)
    {
      return;
    }

    if (builder.Length > 0)
    {
      builder.Append(' ');
    }

    builder.Append(trimmed);
  }
}
