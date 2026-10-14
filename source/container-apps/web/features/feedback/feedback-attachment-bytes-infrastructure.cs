#region Purpose
// Postgres row that holds one feedback attachment's bytes, keyed by its storage key.
#endregion

#region Design
// Persistence shape only, not a domain type: the bytes belong to the blob port, and
// FeedbackAttachment stays the metadata row. Keyed by StorageKey so the port contract
// (opaque string keys) holds without a second id. There is no foreign key to the
// attachment row: upload writes the bytes before the row, and delete removes the row
// before the bytes, so "row exists" implies "bytes exist" without an ordering constraint
// the database would reject.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Infrastructure;

/// <summary>Stored bytes for one feedback attachment.</summary>
public sealed class FeedbackAttachmentBytes
{
  public FeedbackAttachmentBytes(string storageKey, byte[] content)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
    ArgumentNullException.ThrowIfNull(content);
    StorageKey = storageKey;
    Content = content;
  }

  /// <summary>The attachment's storage key.</summary>
  public string StorageKey { get; }

  /// <summary>The file bytes.</summary>
  public byte[] Content { get; }
}
