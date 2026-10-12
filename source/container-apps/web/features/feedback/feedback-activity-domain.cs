#region Purpose
// One entry in a feedback item's activity thread: a status change, a reply, or an internal note.
#endregion

#region Design
// Owned by FeedbackItem and created only through its named mutations (internal factories).
// Sequence is 1-based and dense within the item; it orders the thread and, with the item id,
// is the row key, so two writers appending the same position collide instead of interleaving.
// A status change carries From/To and no body; replies and notes carry a body and no status.
// IsVisibleToFiler is derived from Kind so visibility can never disagree with the entry type.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Domain;

public sealed class FeedbackActivity
{
  private FeedbackActivity(
    int sequence,
    FeedbackActivityKind kind,
    Guid authorPrincipalId,
    string? body,
    FeedbackStatus? fromStatus,
    FeedbackStatus? toStatus,
    DateTimeOffset at)
  {
    Sequence = sequence;
    Kind = kind;
    AuthorPrincipalId = authorPrincipalId;
    Body = body;
    FromStatus = fromStatus;
    ToStatus = toStatus;
    At = at;
  }

  public int Sequence { get; }
  public FeedbackActivityKind Kind { get; }
  public Guid AuthorPrincipalId { get; }
  public string? Body { get; }
  public FeedbackStatus? FromStatus { get; }
  public FeedbackStatus? ToStatus { get; }
  public DateTimeOffset At { get; }

  public bool IsVisibleToFiler => Kind != FeedbackActivityKind.InternalNote;

  internal static FeedbackActivity StatusChange(
    int sequence,
    Guid authorPrincipalId,
    FeedbackStatus fromStatus,
    FeedbackStatus toStatus,
    DateTimeOffset at) =>
    new(sequence, FeedbackActivityKind.StatusChange, authorPrincipalId, body: null, fromStatus, toStatus, at);

  internal static FeedbackActivity Message(
    int sequence,
    FeedbackActivityKind kind,
    Guid authorPrincipalId,
    string body,
    DateTimeOffset at) =>
    new(sequence, kind, authorPrincipalId, body, fromStatus: null, toStatus: null, at);
}
