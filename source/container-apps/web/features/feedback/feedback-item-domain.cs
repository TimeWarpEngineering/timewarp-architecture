#region Purpose
// Aggregate for one feedback filing owned by the principal who submitted it.
#endregion

#region Design
// Private constructor and a single File factory: an item is complete at creation and has no
// later mutations (no status, assignment, or edit). Owner is the authenticated principal Guid,
// not a ProfileId — filing must succeed when the principal has no profile row.
// MaxTitleLength and MaxBodyLength are the domain length SSOT. Contract validators duplicate
// the literals because contracts must not reference domain.
// The nested private Invariants validator is the save-time half (TWA0011/TWA0012). Private
// nesting keeps it out of AddValidatorsFromAssemblyContaining.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Domain;

using FluentValidation;

public sealed class FeedbackItem : Entity<FeedbackItemId>, IAggregateRoot
{
  public const int MaxTitleLength = 200;
  public const int MaxBodyLength = 8000;

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
  }

  public Guid OwnerPrincipalId { get; }
  public FeedbackKind Kind { get; }
  public string Title { get; }
  public string Body { get; }
  public DateTimeOffset FiledAt { get; }

  public static FeedbackItem File(
    Guid ownerPrincipalId,
    FeedbackKind kind,
    string title,
    string body,
    DateTimeOffset filedAt)
  {
    if (ownerPrincipalId == Guid.Empty)
    {
      throw new ArgumentException("Owner principal id must be non-empty.", nameof(ownerPrincipalId));
    }

    if (!Enum.IsDefined(kind))
    {
      throw new ArgumentException("Kind must be a defined feedback kind.", nameof(kind));
    }

    string trimmedTitle = RequireText(title, MaxTitleLength, nameof(title));
    string trimmedBody = RequireText(body, MaxBodyLength, nameof(body));
    return new FeedbackItem(FeedbackItemId.New(), ownerPrincipalId, kind, trimmedTitle, trimmedBody, filedAt);
  }

  private static string RequireText(string value, int maxLength, string name)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
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
    }
  }
}
