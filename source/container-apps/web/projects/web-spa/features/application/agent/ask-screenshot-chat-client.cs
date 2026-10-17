#region Purpose
// Sends the screenshot on Ask's composer with the user message it was attached to.
#endregion

#region Design
// Task 303-001, the same shape as AskTagChatClient (task 346): the library MessageInput has no
// attachment hook, so this client sits in front of the tool-invoking client. When the request
// ends with a person's text message and a screenshot is waiting (ScreenshotState.AskAttachedFor
// the current conversation), it sends a copy of that message with the PNG as image content;
// RelayChatClient maps it to the turn's Images. Only after the inner call succeeds (the response,
// the first streamed update, or the end of an empty stream) does it dispatch MarkAskSent with the
// id it sent, so a failed send keeps the thumbnail. The conversation thread keeps the person's
// own message without the picture: later turns do not send it again, and page_context.screenshot
// says it was sent. Requests that do not end in a person's text pass through untouched.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Appends Ask's waiting screenshot to the outgoing user message.</summary>
public sealed class AskScreenshotChatClient : DelegatingChatClient
{
  private readonly Func<ScreenshotCapture?> Pending;
  private readonly Func<int, Task> MarkSent;

  public AskScreenshotChatClient
  (
    IChatClient inner,
    Func<ScreenshotCapture?> pending,
    Func<int, Task> markSent
  ) : base(inner)
  {
    Pending = pending ?? throw new ArgumentNullException(nameof(pending));
    MarkSent = markSent ?? throw new ArgumentNullException(nameof(markSent));
  }

  public override async Task<ChatResponse> GetResponseAsync
  (
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    CancellationToken cancellationToken = default
  )
  {
    (IEnumerable<ChatMessage> outgoing, int? sentId) = Attach(messages);
    ChatResponse response = await base.GetResponseAsync(outgoing, options, cancellationToken);
    await ReportAsync(sentId);
    return response;
  }

  public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
  (
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
  )
  {
    (IEnumerable<ChatMessage> outgoing, int? sentId) = Attach(messages);
    bool reported = sentId is null;
    await foreach (ChatResponseUpdate update in base.GetStreamingResponseAsync(outgoing, options, cancellationToken))
    {
      if (!reported)
      {
        reported = true;
        await ReportAsync(sentId);
      }

      yield return update;
    }

    if (!reported)
    {
      await ReportAsync(sentId);
    }
  }

  /// <summary>
  /// The request with <paramref name="capture"/> appended to its last message as image content, or
  /// null when that message is not a person's text.
  /// </summary>
  public static List<ChatMessage>? WithScreenshot(IEnumerable<ChatMessage> messages, ScreenshotCapture? capture)
  {
    ArgumentNullException.ThrowIfNull(messages);
    List<ChatMessage> list = [.. messages];
    if (capture is null || list.Count == 0)
    {
      return null;
    }

    ChatMessage last = list[^1];
    if (last.Role != ChatRole.User || !last.Contents.Any(static content => content is TextContent))
    {
      return null;
    }

    ChatMessage attached = last.Clone();
    attached.Contents = [.. last.Contents, new DataContent(capture.Png, ScreenshotState.ContentType)];
    list[^1] = attached;
    return list;
  }

  private (IEnumerable<ChatMessage> Messages, int? SentId) Attach(IEnumerable<ChatMessage> messages)
  {
    ScreenshotCapture? pending = Pending();
    List<ChatMessage>? attached = WithScreenshot(messages, pending);
    return attached is null ? (messages, null) : (attached, pending!.Id);
  }

  private Task ReportAsync(int? sentId) => sentId is { } id ? MarkSent(id) : Task.CompletedTask;
}
