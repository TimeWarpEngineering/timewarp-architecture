#region Purpose
// Sends the composer's tag chips with the user message they were attached to.
#endregion

#region Design
// Task 346. The library MessageInput sends plain text and has no attachment hook, so the panel puts
// this client in front of the tool-invoking client. When the request ends with a person's text
// message and chips are waiting, it sends a copy of that message with one more text part,
// AskTagPayload.Message. Only after the inner call succeeds (the response, or the first streamed
// update, or the end of an empty stream; never an exception or cancellation) does it report the ids
// it sent (MarkSent, a State action), so the composer clears just those chips, a failed send keeps
// them attached, and page_context.tags keeps them. The conversation thread keeps the person's own
// text: the transcript shows what they typed, and later turns read the tags from page_context.tags.
// The panel builds its instructions (which embed page_context) once per agent, so the optional
// instructions delegate re-renders them on a clone of the request's ChatOptions; every request
// therefore carries the tags sent so far. Requests that do not end in a text message (tool results,
// approval answers) pass through untouched, so a tag is sent once, with the message it was chosen
// for.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Appends waiting Ask tags to the outgoing user message.</summary>
public sealed class AskTagChatClient : DelegatingChatClient
{
  private readonly Func<IReadOnlyList<AskTag>> Pending;
  private readonly Func<IReadOnlyList<int>, Task> MarkSent;
  private readonly Func<string>? Instructions;

  public AskTagChatClient
  (
    IChatClient inner,
    Func<IReadOnlyList<AskTag>> pending,
    Func<IReadOnlyList<int>, Task> markSent,
    Func<string>? instructions = null
  ) : base(inner)
  {
    Pending = pending ?? throw new ArgumentNullException(nameof(pending));
    MarkSent = markSent ?? throw new ArgumentNullException(nameof(markSent));
    Instructions = instructions;
  }

  public override async Task<ChatResponse> GetResponseAsync
  (
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    CancellationToken cancellationToken = default
  )
  {
    (IEnumerable<ChatMessage> outgoing, IReadOnlyList<int> sentIds) = Attach(messages);
    ChatResponse response = await base.GetResponseAsync(outgoing, Refresh(options), cancellationToken);
    await ReportAsync(sentIds);
    return response;
  }

  public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
  (
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
  )
  {
    (IEnumerable<ChatMessage> outgoing, IReadOnlyList<int> sentIds) = Attach(messages);
    bool reported = sentIds.Count == 0;
    await foreach (ChatResponseUpdate update in base.GetStreamingResponseAsync(outgoing, Refresh(options), cancellationToken))
    {
      if (!reported)
      {
        reported = true;
        await ReportAsync(sentIds);
      }

      yield return update;
    }

    if (!reported)
    {
      await ReportAsync(sentIds);
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

  private (IEnumerable<ChatMessage> Messages, IReadOnlyList<int> SentIds) Attach(IEnumerable<ChatMessage> messages)
  {
    IReadOnlyList<AskTag> pending = Pending();
    List<ChatMessage>? tagged = WithTags(messages, pending);
    if (tagged is null)
    {
      return (messages, []);
    }

    return (tagged, [.. pending.Select(static tag => tag.Id)]);
  }

  private Task ReportAsync(IReadOnlyList<int> sentIds) => sentIds.Count == 0 ? Task.CompletedTask : MarkSent(sentIds);

  /// <summary>The options with the instructions rebuilt for this request, so page_context.tags is current.</summary>
  private ChatOptions? Refresh(ChatOptions? options)
  {
    if (Instructions is null || options is null)
    {
      return options;
    }

    ChatOptions refreshed = options.Clone();
    refreshed.Instructions = Instructions();
    return refreshed;
  }
}
