#region Purpose
// In-memory transcript of one Ask conversation, kept outside the panel so a rebuilt agent restores it.
#endregion

#region Design
// UIAgent writes a turn here only after it finishes streaming: AppendUserMessage, AppendUpdate for
// every later message and response update, then CompleteTurn. GetUpdates returns committed turns
// only, so a turn cut off by close or navigation is not replayed. UIAgent.RestoreAsync rebuilds
// both the model history and the rendered blocks from GetUpdates, so the model sees prior turns.
// The relay holds no server-side conversation, so IsStateful is false and ConversationId is null.
// An approval request that was never answered (the panel closed while it waited) is left out of
// GetUpdates: replaying it would draw a Pending card nobody can answer and would send the model a
// request with no response. Answered approvals are replayed; ApprovalAnswer reports the recorded
// decision so the restored card shows it instead of Approve and Reject buttons.
// Memory only, scoped to the WASM app (the circuit). A reload starts empty.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using Microsoft.AspNetCore.Components.AI;

/// <summary>Committed Ask turns for one conversation generation.</summary>
public sealed class AskConversationThread : IConversationThread
{
  private readonly List<ChatResponseUpdate> Committed = [];
  private readonly List<ChatResponseUpdate> Pending = [];

  public string ThreadId { get; } = Guid.CreateVersion7().ToString();

  public bool IsStateful => false;

  public string? ConversationId => null;

  /// <summary>Number of committed turns.</summary>
  public int TurnCount { get; private set; }

  public void AppendUserMessage(ChatMessage message)
  {
    ArgumentNullException.ThrowIfNull(message);
    Pending.Clear();
    Pending.Add
    (
      new ChatResponseUpdate
      {
        Role = message.Role,
        MessageId = message.MessageId,
        Contents = [.. message.Contents],
      }
    );
  }

  public void AppendUpdate(ChatResponseUpdate update)
  {
    ArgumentNullException.ThrowIfNull(update);
    Pending.Add(update);
  }

  public void CompleteTurn()
  {
    Committed.AddRange(Pending);
    Pending.Clear();
    TurnCount++;
  }

  public IReadOnlyList<ChatResponseUpdate> GetUpdates()
  {
    HashSet<string> answered = AnsweredRequestIds();
    List<ChatResponseUpdate> updates = [];
    foreach (ChatResponseUpdate update in Committed)
    {
      bool dangling = false;
      foreach (AIContent content in update.Contents)
      {
        if (content is ToolApprovalRequestContent request && !answered.Contains(request.RequestId))
        {
          dangling = true;
          break;
        }
      }

      if (!dangling)
      {
        updates.Add(update);
        continue;
      }

      List<AIContent> kept = [];
      foreach (AIContent content in update.Contents)
      {
        if (content is ToolApprovalRequestContent request && !answered.Contains(request.RequestId))
        {
          continue;
        }

        kept.Add(content);
      }

      if (kept.Count == 0)
      {
        continue;
      }

      ChatResponseUpdate trimmed = update.Clone();
      trimmed.Contents = kept;
      updates.Add(trimmed);
    }

    return updates;
  }

  /// <summary>The recorded answer for an approval request, or null while it is still open.</summary>
  public bool? ApprovalAnswer(string requestId)
  {
    foreach (ChatResponseUpdate update in Committed)
    {
      foreach (AIContent content in update.Contents)
      {
        if (content is ToolApprovalResponseContent response
          && string.Equals(response.RequestId, requestId, StringComparison.Ordinal))
        {
          return response.Approved;
        }
      }
    }

    return null;
  }

  private HashSet<string> AnsweredRequestIds()
  {
    HashSet<string> answered = [];
    foreach (ChatResponseUpdate update in Committed)
    {
      foreach (AIContent content in update.Contents)
      {
        if (content is ToolApprovalResponseContent response)
        {
          answered.Add(response.RequestId);
        }
      }
    }

    return answered;
  }
}
