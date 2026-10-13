#region Purpose
// Wire shape of one feedback item as an administrator sees it: filing, status, attachments, thread.
#endregion

#region Design
// Returned by GetFeedbackForAdmin and by every admin mutation (status, reply, note), so the
// detail page always renders the server's state after a write without a second request.
// Activity includes internal notes. FilerHasEmail tells the admin whether a reply or a
// notifying status change will also reach the filer by email; the address itself is not sent.
// Attachments reuse GetFeedback.Attachment, whose DownloadPath the download endpoint serves to
// the owner and to admin.feedback.read.
// Task 336: OwnerDisplayName is the filer's profile display name, set only when the caller
// holds admin.profiles.read and the filer has a profile row; every admin endpoint that returns
// this view fills it the same way, so a mutation never blanks the name on the detail page.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

public sealed class FeedbackAdminView
{
  public Guid FeedbackItemId { get; }
  public Guid OwnerPrincipalId { get; }
  public FeedbackKind Kind { get; }
  public FeedbackStatus Status { get; }
  public string Title { get; }
  public string Body { get; }
  public DateTimeOffset FiledAt { get; }
  public bool FilerHasEmail { get; }
  public IReadOnlyList<GetFeedback.Attachment> Attachments { get; }
  public IReadOnlyList<FeedbackActivityEntry> Activity { get; }
  public string? OwnerDisplayName { get; }

  public FeedbackAdminView(
    Guid feedbackItemId,
    Guid ownerPrincipalId,
    FeedbackKind kind,
    FeedbackStatus status,
    string title,
    string body,
    DateTimeOffset filedAt,
    bool filerHasEmail,
    IReadOnlyList<GetFeedback.Attachment>? attachments = null,
    IReadOnlyList<FeedbackActivityEntry>? activity = null,
    string? ownerDisplayName = null)
  {
    FeedbackItemId = Guard.Against.NullOrEmpty(feedbackItemId);
    OwnerPrincipalId = Guard.Against.NullOrEmpty(ownerPrincipalId);
    if (!Enum.IsDefined(kind))
    {
      throw new ArgumentException("Kind must be a defined feedback kind.", nameof(kind));
    }

    if (!Enum.IsDefined(status))
    {
      throw new ArgumentException("Status must be a defined feedback status.", nameof(status));
    }

    Kind = kind;
    Status = status;
    Title = Guard.Against.NullOrWhiteSpace(title);
    Body = Guard.Against.NullOrWhiteSpace(body);
    FiledAt = filedAt;
    FilerHasEmail = filerHasEmail;
    Attachments = attachments ?? [];
    Activity = activity ?? [];
    OwnerDisplayName = ownerDisplayName;
  }

  /// <summary>Deterministic sample for mock mode: one item under review with a reply and a note.</summary>
  public static FeedbackAdminView Sample(Guid feedbackItemId, FeedbackStatus status = FeedbackStatus.UnderReview)
  {
    var owner = Guid.Parse("33333333-3333-3333-3333-333333333333");
    var admin = Guid.Parse("44444444-4444-4444-4444-444444444444");
    var filedAt = DateTimeOffset.Parse("2026-10-09T00:00:00Z");
    return new FeedbackAdminView(
      feedbackItemId,
      owner,
      FeedbackKind.BugReport,
      status,
      "Mock bug report",
      "Mock body",
      filedAt,
      filerHasEmail: true,
      activity:
      [
        new FeedbackActivityEntry(1, FeedbackActivityKind.StatusChange, admin, null, FeedbackStatus.New, status, filedAt.AddHours(1)),
        new FeedbackActivityEntry(2, FeedbackActivityKind.AdminReply, admin, "Thanks, we are looking.", null, null, filedAt.AddHours(2)),
        new FeedbackActivityEntry(3, FeedbackActivityKind.InternalNote, admin, "Reproduced locally.", null, null, filedAt.AddHours(3)),
      ],
      ownerDisplayName: "Ada Lovelace");
  }
}
