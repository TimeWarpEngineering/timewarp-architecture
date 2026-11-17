#region Purpose
// The deterministic in-process chat client that stands in for xAI when tests or Development ask for it.
#endregion

#region Design
// XaiChatUpstream hands this client out only when UseFakeUpstream is set in Development or
// Testing. It scripts from the messages after the last user message. By default it calls
// page_context, then answers "page_context: {result}".
// "take me" or "go there" with a markdown link in an earlier assistant turn calls navigate
// with that url, when navigate is offered. "where" calls site_map, when site_map is offered.
// A site_map result is matched against the user's words (5+ letters) and answered with a
// markdown link to the first matching destination. Any other tool result is echoed as
// "{tool}: {result}".
// Images (task 303-001): when the last user message carries an image and no tool has answered
// since, the fake replies "image: {mediaType} {width}x{height} {bytes} bytes" (PNG size from the
// IHDR chunk), so a test can see the picture arrived. "screenshot" calls the capture tool when it
// is offered.
// "markdown sample" (task 380) replies with MarkdownSample: bold, a list, an in-app link, an
// external link and raw HTML, so a test can see how Ask renders a formatted reply.
// It holds no resources, so Dispose does nothing.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

/// <summary>A scripted chat client that answers without calling xAI.</summary>
internal sealed class XaiFakeChatClient : IChatClient
{
  public Task<ChatResponse> GetResponseAsync
  (
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    CancellationToken cancellationToken = default
  )
  {
    var all = messages.ToList();
    int lastUser = all.FindLastIndex(message => message.Role == ChatRole.User);
    string userText = lastUser < 0 ? string.Empty : all[lastUser].Text ?? string.Empty;
    FunctionResultContent? result = all
      .Skip(lastUser + 1)
      .SelectMany(message => message.Contents)
      .OfType<FunctionResultContent>()
      .LastOrDefault();
    DataContent? picture = lastUser < 0
      ? null
      : all[lastUser].Contents.OfType<DataContent>().FirstOrDefault(content => content.HasTopLevelMediaType("image"));
    if (picture is not null && result is null)
    {
      return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, DescribeImage(picture))));
    }

    if (result is not null)
    {
      string rendered = result.Result as string ?? JsonSerializer.Serialize(result.Result);
      string name = all
        .SelectMany(message => message.Contents)
        .OfType<FunctionCallContent>()
        .FirstOrDefault(call => call.CallId == result.CallId)?.Name ?? "page_context";
      string text = name == "site_map"
        ? AnswerSiteMap(userText, rendered)
        : $"{name}: {rendered}";
      return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));
    }

    if (userText.Contains("markdown sample", StringComparison.OrdinalIgnoreCase))
    {
      return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, MarkdownSample)));
    }

    bool Offers(string toolName) =>
      options?.Tools?.Any(tool => tool.Name == toolName) == true;

    if (userText.Contains("take me", StringComparison.OrdinalIgnoreCase) ||
        userText.Contains("go there", StringComparison.OrdinalIgnoreCase))
    {
      string? url = all
        .Take(Math.Max(lastUser, 0))
        .Where(message => message.Role == ChatRole.Assistant)
        .Select(message => LastLinkTarget(message.Text ?? string.Empty))
        .LastOrDefault(target => target is not null);
      if (url is not null && Offers("navigate"))
      {
        return Task.FromResult(ToolTurn("call-navigate", "navigate", new Dictionary<string, object?> { ["url"] = url }));
      }
    }
    else if (userText.Contains("screenshot", StringComparison.OrdinalIgnoreCase) && Offers(CaptureToolName))
    {
      return Task.FromResult(ToolTurn("call-capture", CaptureToolName, new Dictionary<string, object?>()));
    }
    else if (userText.Contains("where", StringComparison.OrdinalIgnoreCase) && Offers("site_map"))
    {
      return Task.FromResult(ToolTurn("call-site-map", "site_map", new Dictionary<string, object?>()));
    }

    return Task.FromResult(ToolTurn("call-1", "page_context", new Dictionary<string, object?>()));
  }

  public const string CaptureToolName = "Screenshot.CaptureScreenshot";

  public const string MarkdownSample =
    "Here is **bold** text.\n\n- first item\n- second item\n\n" +
    "Open [Sign in](/Login) or [Example](https://example.com).\n\n" +
    "<b data-qa=\"AskRawHtml\">raw</b>";

  public static string DescribeImage(DataContent picture)
  {
    ReadOnlySpan<byte> data = picture.Data.Span;
    string size = data.Length >= 24 && data[12..16].SequenceEqual("IHDR"u8)
      ? $"{System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(data[16..20])}x{System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(data[20..24])}"
      : "unknown-size";
    return $"image: {picture.MediaType} {size} {data.Length} bytes";
  }

  private static ChatResponse ToolTurn(string callId, string name, Dictionary<string, object?> arguments) =>
    new(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent(callId, name, arguments)]));

  private static string AnswerSiteMap(string userText, string rendered)
  {
    List<string> keywords = [];
    foreach (string word in Words(userText))
    {
      if (word.Length < 5)
      {
        continue;
      }

      keywords.Add(word);
      if (word.EndsWith('s', StringComparison.Ordinal))
      {
        keywords.Add(word[..^1]);
      }
    }

    try
    {
      using var document = JsonDocument.Parse(rendered);
      if (document.RootElement.TryGetProperty("destinations", out JsonElement destinations) &&
          destinations.ValueKind == JsonValueKind.Array)
      {
        foreach (JsonElement destination in destinations.EnumerateArray())
        {
          string title = ReadString(destination, "title");
          string url = ReadString(destination, "url");
          string haystack = $"{title} {ReadString(destination, "description")}";
          if (url.Length > 0 &&
              keywords.Any(keyword => haystack.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
          {
            return $"You can do that on the [{title}]({url}) page. Say \"take me there\" and I will open it.";
          }
        }
      }
    }
    catch (JsonException)
    {
      // Not JSON: fall through to the echo.
    }

    return $"site_map: {rendered}";
  }

  // The url of the last markdown link to an app path ("](/...)") in text, or null.
  private static string? LastLinkTarget(string text)
  {
    int start = text.LastIndexOf("](/", StringComparison.Ordinal);
    if (start < 0)
    {
      return null;
    }

    start += 2;
    int end = start;
    while (end < text.Length && text[end] != ')' && !char.IsWhiteSpace(text[end]))
    {
      end++;
    }

    return end < text.Length && text[end] == ')' ? text[start..end] : null;
  }

  // Lower-case runs of ASCII letters.
  private static IEnumerable<string> Words(string text)
  {
    var word = new System.Text.StringBuilder();
    foreach (char character in text.ToLowerInvariant())
    {
      if (character is >= 'a' and <= 'z')
      {
        word.Append(character);
        continue;
      }

      if (word.Length > 0)
      {
        yield return word.ToString();
        word.Clear();
      }
    }

    if (word.Length > 0)
    {
      yield return word.ToString();
    }
  }

  private static string ReadString(JsonElement element, string property) =>
    element.ValueKind == JsonValueKind.Object &&
    element.TryGetProperty(property, out JsonElement value) &&
    value.ValueKind == JsonValueKind.String
      ? value.GetString() ?? string.Empty
      : string.Empty;

  public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
  (
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default
  )
  {
    ChatResponse response = await GetResponseAsync(messages, options, cancellationToken)
      .ConfigureAwait(false);
    foreach (ChatResponseUpdate update in response.ToChatResponseUpdates())
    {
      yield return update;
    }
  }

  public object? GetService(Type serviceType, object? serviceKey = null) =>
    serviceType.IsInstanceOfType(this) ? this : null;

  public void Dispose()
  {
  }
}
