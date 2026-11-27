#region Purpose
// Wire shape of one entry in a feedback item's activity thread.
#endregion

#region Design
// Shared by the filer's view (GetFeedback, ReplyAsFiler) and the admin view (FeedbackAdminView).
// The server decides what each audience sees: the filer never receives InternalNote or
// SeverityChange entries, and AuthorPrincipalId is null on the filer's copy so an
// administrator's identity is not disclosed (the kind already says "admin" or "you"). A status
// change has From/To status and no body or severity. A severity change has From/To severity
// and no body or status. Replies and notes have a body and neither. Null severity properties
// are omitted from JSON, so a filer document, which never contains a severity entry, has no
// severity field.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

using System.Text.Json.Serialization;

public sealed class FeedbackActivityEntry : ICloneable
{
  /// <summary>Immutable (get-only members), so a State clone can share the instance.</summary>
  public object Clone() => this;

  /// <summary>The entry's position in the thread.</summary>
  public int Sequence { get; }
  /// <summary>What kind of thread entry this is.</summary>
  public FeedbackActivityKind Kind { get; }
  /// <summary>
  /// The entry's author, projected from the root's Guid. The handler sets it to null on the filer's
  /// copy (see Design), so the wire type widens it to Guid?, which TWA0041 accepts on a read.
  /// </summary>
  public Guid? AuthorPrincipalId { get; }
  /// <summary>The entry text. Present on replies and notes.</summary>
  public string? Body { get; }
  /// <summary>The status before the change. Present on status changes.</summary>
  public FeedbackStatus? FromStatus { get; }
  /// <summary>The status after the change. Present on status changes.</summary>
  public FeedbackStatus? ToStatus { get; }
  /// <summary>When the entry was added.</summary>
  public DateTimeOffset At { get; }

  /// <summary>The severity before the change. Present on severity changes.</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public FeedbackSeverity? FromSeverity { get; }

  /// <summary>The severity after the change. Present on severity changes.</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public FeedbackSeverity? ToSeverity { get; }

  public FeedbackActivityEntry(
    int sequence,
    FeedbackActivityKind kind,
    Guid? authorPrincipalId,
    string? body,
    FeedbackStatus? fromStatus,
    FeedbackStatus? toStatus,
    DateTimeOffset at,
    FeedbackSeverity? fromSeverity = null,
    FeedbackSeverity? toSeverity = null)
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
    FromSeverity = fromSeverity;
    ToSeverity = toSeverity;
  }
}
