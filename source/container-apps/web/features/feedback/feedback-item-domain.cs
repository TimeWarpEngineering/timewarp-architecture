#region Purpose
// Aggregate for one feedback filing: what the filer sent, its triage status, and its thread.
#endregion

#region Design
// Private constructor and a File factory: the filing (owner, kind, title, body, filed-at) is
// complete at creation and never edited. Owner is the authenticated principal Guid, not a
// ProfileId — filing must succeed when the principal has no profile row.
// Task 314 adds triage. Status starts at New. Severity starts at Unset (task 378) and means
// "not reviewed", including rows filed before severity existed. The named mutations are the
// only writers: ChangeStatus (never back to New, never to the current status), SetSeverity
// (a defined value, never Unset, never the current value; does not move Status), AddAdminReply,
// AddFilerReply (owner only) and AddInternalNote (never shown to the filer). Each appends one
// FeedbackActivity. Status always equals the last status change's To (New when there is none).
// Severity always equals the last severity change's To (Unset when there is none). A status
// change does not set severity. Assignee, tags, merging and kind re-classification are
// deferred (decision 5), and nothing deletes thread entries.
// Who may call the admin mutations is the endpoint's permission check; the aggregate only
// guards what it can know (owner identity for filer replies, non-empty principals).
// Activity is ordered by Sequence; EF loads owned rows in no particular order, so the getter
// sorts. MaxTitleLength, MaxBodyLength and MaxMessageLength are the domain length SSOT.
// Contract validators duplicate the literals because contracts must not reference domain.
// The nested private Invariants validator is the save-time half (TWA0011/TWA0012). Private
// nesting keeps it out of AddValidatorsFromAssemblyContaining.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Domain;

using FluentValidation;

public sealed class FeedbackItem : Entity<FeedbackItemId>, IAggregateRoot
{
  public const int MaxTitleLength = 200;
  public const int MaxBodyLength = 8000;
  public const int MaxMessageLength = 4000;

  private readonly List<FeedbackActivity> ActivityList = [];

  private FeedbackItem(
    FeedbackItemId id,
    Guid ownerPrincipalId,
    FeedbackKind kind,
    string title,
    string body,
    DateTimeOffset filedAt)
    : base(id)
  {
    OwnerPrincipalId = ownerPrincipalId;
    Kind = kind;
    Title = title;
    Body = body;
    FiledAt = filedAt;
    Status = FeedbackStatus.New;
  }

  public Guid OwnerPrincipalId { get; }
  public FeedbackKind Kind { get; }
  public string Title { get; }
  public string Body { get; }
  public DateTimeOffset FiledAt { get; }
  public FeedbackStatus Status { get; private set; }
  public FeedbackSeverity Severity { get; private set; } = FeedbackSeverity.Unset;

  /// <summary>The thread, oldest first.</summary>
  public IReadOnlyList<FeedbackActivity> Activity =>
    [.. ActivityList.OrderBy(entry => entry.Sequence)];

  public static FeedbackItem File(
    Guid ownerPrincipalId,
    FeedbackKind kind,
    string title,
    string body,
    DateTimeOffset filedAt)
  {
    RequirePrincipal(ownerPrincipalId, nameof(ownerPrincipalId));
    if (!Enum.IsDefined(kind))
    {
      throw new ArgumentException("Kind must be a defined feedback kind.", nameof(kind));
    }

    string trimmedTitle = RequireText(title, MaxTitleLength, nameof(title));
    string trimmedBody = RequireText(body, MaxBodyLength, nameof(body));
    return new FeedbackItem(FeedbackItemId.New(), ownerPrincipalId, kind, trimmedTitle, trimmedBody, filedAt);
  }

  /// <summary>True when <see cref="ChangeStatus"/> would accept <paramref name="status"/>.</summary>
  public bool CanChangeStatusTo(FeedbackStatus status) =>
    Enum.IsDefined(status) && status != FeedbackStatus.New && status != Status;

  /// <summary>Moves the item to another status and records who did it.</summary>
  public FeedbackActivity ChangeStatus(FeedbackStatus status, Guid adminPrincipalId, DateTimeOffset at)
  {
    RequirePrincipal(adminPrincipalId, nameof(adminPrincipalId));
    if (!Enum.IsDefined(status))
    {
      throw new ArgumentException("Status must be a defined feedback status.", nameof(status));
    }

    if (status == FeedbackStatus.New)
    {
      throw new InvalidOperationException("A feedback item cannot return to New.");
    }

    if (status == Status)
    {
      throw new InvalidOperationException($"The feedback item is already {status}.");
    }

    var entry = FeedbackActivity.StatusChange(NextSequence(), adminPrincipalId, Status, status, at);
    Status = status;
    ActivityList.Add(entry);
    return entry;
  }

  /// <summary>True when <see cref="SetSeverity"/> would accept <paramref name="severity"/>.</summary>
  public bool CanSetSeverityTo(FeedbackSeverity severity) =>
    Enum.IsDefined(severity) && severity != FeedbackSeverity.Unset && severity != Severity;

  /// <summary>Records a review severity. Does not move <see cref="Status"/>.</summary>
  public FeedbackActivity SetSeverity(FeedbackSeverity severity, Guid principalId, DateTimeOffset at)
  {
    RequirePrincipal(principalId, nameof(principalId));
    if (!Enum.IsDefined(severity))
    {
      throw new ArgumentException("Severity must be a defined feedback severity.", nameof(severity));
    }

    if (severity == FeedbackSeverity.Unset)
    {
      throw new InvalidOperationException("A feedback item cannot return to Unset.");
    }

    if (severity == Severity)
    {
      throw new InvalidOperationException($"The feedback item is already {severity}.");
    }

    var entry = FeedbackActivity.SeverityChange(NextSequence(), principalId, Severity, severity, at);
    Severity = severity;
    ActivityList.Add(entry);
    return entry;
  }

  /// <summary>Adds an administrator's reply, visible to the filer.</summary>
  public FeedbackActivity AddAdminReply(Guid adminPrincipalId, string body, DateTimeOffset at) =>
    AddMessage(FeedbackActivityKind.AdminReply, adminPrincipalId, body, at);

  /// <summary>Adds the filer's answer in the same thread. Only the owner may call it.</summary>
  public FeedbackActivity AddFilerReply(Guid filerPrincipalId, string body, DateTimeOffset at)
  {
    if (filerPrincipalId != OwnerPrincipalId)
    {
      throw new InvalidOperationException("Only the filer can add a filer reply.");
    }

    return AddMessage(FeedbackActivityKind.FilerReply, filerPrincipalId, body, at);
  }

  /// <summary>Adds an admin-only note. The filer never sees it.</summary>
  public FeedbackActivity AddInternalNote(Guid adminPrincipalId, string body, DateTimeOffset at) =>
    AddMessage(FeedbackActivityKind.InternalNote, adminPrincipalId, body, at);

  private FeedbackActivity AddMessage(FeedbackActivityKind kind, Guid authorPrincipalId, string body, DateTimeOffset at)
  {
    RequirePrincipal(authorPrincipalId, nameof(authorPrincipalId));
    string trimmed = RequireText(body, MaxMessageLength, nameof(body));
    var entry = FeedbackActivity.Message(NextSequence(), kind, authorPrincipalId, trimmed, at);
    ActivityList.Add(entry);
    return entry;
  }

  private int NextSequence() => ActivityList.Count == 0 ? 1 : ActivityList.Max(entry => entry.Sequence) + 1;

  private static void RequirePrincipal(Guid principalId, string name)
  {
    if (principalId == Guid.Empty)
    {
      throw new ArgumentException("Principal id must be non-empty.", name);
    }
  }

  private static string RequireText(string value, int maxLength, string name)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
    string trimmed = value.Trim();
    ArgumentOutOfRangeException.ThrowIfGreaterThan(trimmed.Length, maxLength, name);
    return trimmed;
  }

  private sealed class Invariants : AbstractValidator<FeedbackItem>
  {
    public Invariants()
    {
      RuleFor(item => item.OwnerPrincipalId).NotEmpty();
      RuleFor(item => item.Kind).IsInEnum();
      RuleFor(item => item.Title).NotEmpty().MaximumLength(MaxTitleLength);
      RuleFor(item => item.Body).NotEmpty().MaximumLength(MaxBodyLength);
      RuleFor(item => item.Status).IsInEnum();
      RuleFor(item => item.Severity).IsInEnum();
      RuleFor(item => item.Activity)
        .Must(HaveDenseSequences)
        .WithMessage("Activity sequences must run 1..n without gaps.");
      RuleFor(item => item)
        .Must(StatusMatchesLastChange)
        .WithMessage("Status must equal the last status change (New when there is none).");
      RuleFor(item => item)
        .Must(SeverityMatchesLastChange)
        .WithMessage("Severity must equal the last severity change (Unset when there is none).");
      RuleForEach(item => item.Activity).ChildRules(entry =>
      {
        entry.RuleFor(activity => activity.Kind).IsInEnum();
        entry.RuleFor(activity => activity.AuthorPrincipalId).NotEmpty();
        entry.When(activity => activity.Kind == FeedbackActivityKind.StatusChange, () =>
        {
          entry.RuleFor(activity => activity.FromStatus).NotNull().IsInEnum();
          entry.RuleFor(activity => activity.ToStatus).NotNull().IsInEnum().NotEqual(FeedbackStatus.New);
          entry.RuleFor(activity => activity.Body).Null();
          entry.RuleFor(activity => activity.FromSeverity).Null();
          entry.RuleFor(activity => activity.ToSeverity).Null();
        });
        entry.When(activity => activity.Kind == FeedbackActivityKind.SeverityChange, () =>
        {
          entry.RuleFor(activity => activity.FromSeverity).NotNull().IsInEnum();
          entry.RuleFor(activity => activity.ToSeverity).NotNull().IsInEnum().NotEqual(FeedbackSeverity.Unset);
          entry.RuleFor(activity => activity.ToSeverity).NotEqual(activity => activity.FromSeverity);
          entry.RuleFor(activity => activity.Body).Null();
          entry.RuleFor(activity => activity.FromStatus).Null();
          entry.RuleFor(activity => activity.ToStatus).Null();
        });
        entry.When(
          activity => activity.Kind is FeedbackActivityKind.AdminReply
            or FeedbackActivityKind.FilerReply
            or FeedbackActivityKind.InternalNote,
          () =>
          {
            entry.RuleFor(activity => activity.Body).NotEmpty().MaximumLength(MaxMessageLength);
            entry.RuleFor(activity => activity.FromStatus).Null();
            entry.RuleFor(activity => activity.ToStatus).Null();
            entry.RuleFor(activity => activity.FromSeverity).Null();
            entry.RuleFor(activity => activity.ToSeverity).Null();
          });
      });
      RuleForEach(item => item.Activity)
        .Must((item, activity) =>
          activity.Kind != FeedbackActivityKind.FilerReply || activity.AuthorPrincipalId == item.OwnerPrincipalId)
        .WithMessage("A filer reply must be written by the owner.");
    }

    private static bool HaveDenseSequences(IReadOnlyList<FeedbackActivity> activity)
    {
      for (int index = 0; index < activity.Count; index++)
      {
        if (activity[index].Sequence != index + 1)
        {
          return false;
        }
      }

      return true;
    }

    private static bool StatusMatchesLastChange(FeedbackItem item)
    {
      FeedbackActivity? last = item.Activity.LastOrDefault(entry => entry.Kind == FeedbackActivityKind.StatusChange);
      return item.Status == (last?.ToStatus ?? FeedbackStatus.New);
    }

    private static bool SeverityMatchesLastChange(FeedbackItem item)
    {
      FeedbackActivity? last = item.Activity.LastOrDefault(entry => entry.Kind == FeedbackActivityKind.SeverityChange);
      return item.Severity == (last?.ToSeverity ?? FeedbackSeverity.Unset);
    }
  }
}
