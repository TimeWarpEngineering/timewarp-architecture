#region Purpose
// Sends the composer's tag chips with the user message they were attached to.
#endregion

#region Design
// Task 346. The library MessageInput sends plain text and has no attachment hook, so the panel
// puts this client in front of the tool-invoking client. When the request ends with a person's
// text message and chips are waiting, it sends a copy of that message with one more text part,
// AskTagPayload.Message, and then reports the chips sent (MarkSent, a State action), so the
// composer clears them and page_context.tags keeps them. The conversation thread keeps the
// person's own text: the transcript shows what they typed, and later turns read the tags from
// page_context.tags. Requests that do not end in a text message (tool results, approval
// answers) pass through untouched, so a tag is sent once, with the message it was chosen for.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Appends waiting Ask tags to the outgoing user message.</summary>
public sealed class AskTagChatClient : DelegatingChatClient
{
  private readonly Func<IReadOnlyList<AskTag>> Pending;
  private readonly Func<Task> MarkSent;

  public AskTagChatClient(IChatClient inner, Func<IReadOnlyList<AskTag>> pending, Func<Task> markSent) : base(inner)
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
    IEnumerable<ChatMessage> outgoing = await AttachAsync(messages);
    return await base.GetResponseAsync(outgoing, options, cancellationToken);
  }

  public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
  (
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
  )
  {
    IEnumerable<ChatMessage> outgoing = await AttachAsync(messages);
    await foreach (ChatResponseUpdate update in base.GetStreamingResponseAsync(outgoing, options, cancellationToken))
    {
      yield return update;
    }
  }

  /// <summary>
  /// The request with <paramref name="tags"/> appended to its last message, or null when that message
  /// is not a person's text.
  /// </summary>
  public static List<ChatMessage>? WithTags(IEnumerable<ChatMessage> messages, IReadOnlyList<AskTag> tags)
  {
    ArgumentNullException.ThrowIfNull(messages);
    ArgumentNullException.ThrowIfNull(tags);
    List<ChatMessage> list = [.. messages];
    if (tags.Count == 0 || list.Count == 0)
    {
      return null;
    }

    ChatMessage last = list[^1];
    if (last.Role != ChatRole.User || !last.Contents.Any(static content => content is TextContent))
    {
      return null;
    }

    ChatMessage tagged = last.Clone();
    tagged.Contents = [.. last.Contents, new TextContent(AskTagPayload.Message(tags))];
    list[^1] = tagged;
    return list;
  }

  private async Task<IEnumerable<ChatMessage>> AttachAsync(IEnumerable<ChatMessage> messages)
  {
    List<ChatMessage>? tagged = WithTags(messages, Pending());
    if (tagged is null)
    {
      return messages;
    }

    await MarkSent();
    return tagged;
  }
}
