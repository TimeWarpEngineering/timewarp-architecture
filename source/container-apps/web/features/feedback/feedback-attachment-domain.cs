#region Purpose
// One file owned by the principal who uploaded it, optionally linked to a feedback item.
#endregion

#region Design
// Not an aggregate: FeedbackItem stays immutable after File, and the attachment's only
// mutation is Link, which runs once. Unlink exists only so a filing that could not link
// every attachment can undo the links it made before the item is removed. FeedbackItemId stays null until submit. Pending
// attachments are visible only to the uploader. Limits duplicate FeedbackAttachmentRules
// because domain does not reference contracts. The nested private Invariants validator is
// the save-time half and stays out of AddValidatorsFromAssemblyContaining.
// StorageKey is assigned by Create as {owner:N}/{attachment:N} with no leading slash.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Domain;

using FluentValidation;

/// <summary>A file uploaded for a feedback filing.</summary>
public sealed class FeedbackAttachment : Entity<FeedbackAttachmentId>
{
  /// <summary>Maximum accepted file size, in bytes.</summary>
  public const int MaxBytes = 5 * 1024 * 1024;

  /// <summary>Maximum file name length after normalization.</summary>
  public const int MaxFileNameLength = 200;

  /// <summary>Maximum attachments filed on one feedback item.</summary>
  public const int MaxPerItem = 8;

  /// <summary>Maximum stored content-type length.</summary>
  public const int MaxContentTypeLength = 127;

  /// <summary>Media types a feedback attachment may use.</summary>
  public static readonly string[] AllowedContentTypes =
  [
    "image/png",
    "image/jpeg",
    "image/gif",
    "image/webp",
    "application/pdf",
    "text/plain",
  ];

  private FeedbackAttachment(
    FeedbackAttachmentId id,
    Guid ownerPrincipalId,
    string fileName,
    string contentType,
    long size,
    string storageKey,
    DateTimeOffset uploadedAt)
    : base(id)
  {
    OwnerPrincipalId = ownerPrincipalId;
    FileName = fileName;
    ContentType = contentType;
    Size = size;
    StorageKey = storageKey;
    UploadedAt = uploadedAt;
  }

  /// <summary>Principal who uploaded the file.</summary>
  public Guid OwnerPrincipalId { get; }

  /// <summary>Feedback item this file was filed with. Null until submit links it.</summary>
  public FeedbackItemId? FeedbackItemId { get; private set; }

  /// <summary>Stored file name. No directory.</summary>
  public string FileName { get; }

  /// <summary>Allow-listed media type.</summary>
  public string ContentType { get; }

  /// <summary>Size in bytes.</summary>
  public long Size { get; }

  /// <summary>Blob key. No leading slash.</summary>
  public string StorageKey { get; }

  /// <summary>When the file was accepted.</summary>
  public DateTimeOffset UploadedAt { get; }

  /// <summary>Creates an unlinked attachment and its storage key.</summary>
  public static FeedbackAttachment Create(
    Guid ownerPrincipalId,
    string fileName,
    string contentType,
    long size,
    DateTimeOffset uploadedAt)
  {
    if (ownerPrincipalId == Guid.Empty)
    {
      throw new ArgumentException("Owner principal id must be non-empty.", nameof(ownerPrincipalId));
    }

    string? normalized = FeedbackAttachmentNames.Normalize(fileName);
    if (normalized is null)
    {
      throw new ArgumentException("File name is not storable.", nameof(fileName));
    }

    if (string.IsNullOrWhiteSpace(contentType)
      || !AllowedContentTypes.Contains(contentType, StringComparer.Ordinal))
    {
      throw new ArgumentException("Content type is not allowed.", nameof(contentType));
    }

    if (size is <= 0 or > MaxBytes)
    {
      throw new ArgumentOutOfRangeException(nameof(size), "Size must be between 1 and MaxBytes.");
    }

    var id = FeedbackAttachmentId.New();
    string storageKey = $"{ownerPrincipalId:N}/{id.Value:N}";
    return new FeedbackAttachment(id, ownerPrincipalId, normalized, contentType, size, storageKey, uploadedAt);
  }

  /// <summary>Associates this attachment with a feedback item. Runs once.</summary>
  public void Link(FeedbackItemId feedbackItemId)
  {
    if (feedbackItemId.IsEmpty)
    {
      throw new ArgumentException("Feedback item id must be non-empty.", nameof(feedbackItemId));
    }

    if (FeedbackItemId is not null)
    {
      throw new InvalidOperationException("Attachment is already linked.");
    }

    FeedbackItemId = feedbackItemId;
  }

  /// <summary>Reverses <see cref="Link"/> for a filing that is being rolled back.</summary>
  public void Unlink(FeedbackItemId feedbackItemId)
  {
    if (FeedbackItemId != feedbackItemId)
    {
      throw new InvalidOperationException("Attachment is not linked to this item.");
    }

    FeedbackItemId = null;
  }

  private sealed class Invariants : AbstractValidator<FeedbackAttachment>
  {
    public Invariants()
    {
      RuleFor(attachment => attachment.OwnerPrincipalId).NotEmpty();
      RuleFor(attachment => attachment.FileName).NotEmpty().MaximumLength(MaxFileNameLength);
      RuleFor(attachment => attachment.ContentType).NotEmpty().MaximumLength(MaxContentTypeLength);
      RuleFor(attachment => attachment.Size).GreaterThan(0).LessThanOrEqualTo(MaxBytes);
      RuleFor(attachment => attachment.StorageKey).NotEmpty();
    }
  }
}
