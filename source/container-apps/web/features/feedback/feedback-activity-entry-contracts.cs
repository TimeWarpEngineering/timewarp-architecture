#region Purpose
// Wire shape of one entry in a feedback item's activity thread.
#endregion

#region Design
// Shared by the filer's view (GetFeedback, ReplyAsFiler) and the admin view (FeedbackAdminView).
// The server decides what each audience sees: the filer never receives InternalNote entries,
// and AuthorPrincipalId is null on the filer's copy so an administrator's identity is not
// disclosed (the kind already says "admin" or "you"). A status change has From/To and no
// body; replies and notes have a body and no status.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

public sealed class FeedbackActivityEntry : ICloneable
{
  /// <summary>Immutable (get-only members), so a State clone can share the instance.</summary>
  public object Clone() => this;

  public int Sequence { get; }
  public FeedbackActivityKind Kind { get; }
  public Guid? AuthorPrincipalId { get; }
  public string? Body { get; }
  public FeedbackStatus? FromStatus { get; }
  public FeedbackStatus? ToStatus { get; }
  public DateTimeOffset At { get; }

  public FeedbackActivityEntry(
    int sequence,
    FeedbackActivityKind kind,
    Guid? authorPrincipalId,
    string? body,
    FeedbackStatus? fromStatus,
    FeedbackStatus? toStatus,
    DateTimeOffset at)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequence);
    if (!Enum.IsDefined(kind))
    {
      throw new ArgumentException("Kind must be a defined activity kind.", nameof(kind));
    }

    Sequence = sequence;
    Kind = kind;
    AuthorPrincipalId = authorPrincipalId;
    Body = body;
    FromStatus = fromStatus;
    ToStatus = toStatus;
    At = at;
  }
}
