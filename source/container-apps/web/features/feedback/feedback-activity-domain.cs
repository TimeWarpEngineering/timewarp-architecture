#region Purpose
// One entry in a feedback item's activity thread: a status change, a severity change, a reply, or an internal note.
#endregion

#region Design
// Owned by FeedbackItem and created only through its named mutations (internal factories).
// Sequence is 1-based and dense within the item; it orders the thread and, with the item id,
// is the row key, so two writers appending the same position collide instead of interleaving.
// A status change carries From/To status and no body or severity. A severity change carries
// From/To severity and no body or status. Replies and notes carry a body and neither.
// IsVisibleToFiler is an allow-list (StatusChange, AdminReply, FilerReply) so InternalNote
// and SeverityChange stay hidden and a new kind cannot leak by falling through a denial.
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
    FeedbackSeverity? fromSeverity,
    FeedbackSeverity? toSeverity,
    DateTimeOffset at)
  {
    Sequence = sequence;
    Kind = kind;
    AuthorPrincipalId = authorPrincipalId;
    Body = body;
    FromStatus = fromStatus;
    ToStatus = toStatus;
    FromSeverity = fromSeverity;
    ToSeverity = toSeverity;
    At = at;
  }

  public int Sequence { get; }
  public FeedbackActivityKind Kind { get; }
  public Guid AuthorPrincipalId { get; }
  public string? Body { get; }
  public FeedbackStatus? FromStatus { get; }
  public FeedbackStatus? ToStatus { get; }
  public FeedbackSeverity? FromSeverity { get; }
  public FeedbackSeverity? ToSeverity { get; }
  public DateTimeOffset At { get; }

  public bool IsVisibleToFiler => Kind is FeedbackActivityKind.StatusChange
    or FeedbackActivityKind.AdminReply
    or FeedbackActivityKind.FilerReply;

  internal static FeedbackActivity StatusChange(
    int sequence,
    Guid authorPrincipalId,
    FeedbackStatus fromStatus,
    FeedbackStatus toStatus,
    DateTimeOffset at) =>
    new(
      sequence,
      FeedbackActivityKind.StatusChange,
      authorPrincipalId,
      body: null,
      fromStatus,
      toStatus,
      fromSeverity: null,
      toSeverity: null,
      at);

  internal static FeedbackActivity SeverityChange(
    int sequence,
    Guid authorPrincipalId,
    FeedbackSeverity fromSeverity,
    FeedbackSeverity toSeverity,
    DateTimeOffset at) =>
    new(
      sequence,
      FeedbackActivityKind.SeverityChange,
      authorPrincipalId,
      body: null,
      fromStatus: null,
      toStatus: null,
      fromSeverity,
      toSeverity,
      at);

  internal static FeedbackActivity Message(
    int sequence,
    FeedbackActivityKind kind,
    Guid authorPrincipalId,
    string body,
    DateTimeOffset at) =>
    new(
      sequence,
      kind,
      authorPrincipalId,
      body,
      fromStatus: null,
      toStatus: null,
      fromSeverity: null,
      toSeverity: null,
      at);
}
