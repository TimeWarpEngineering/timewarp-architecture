#region Purpose
// Application port for feedback attachment rows. Handlers never take a DbContext.
#endregion

#region Design
// Same dual-mode shape as IFeedbackStore: an in-memory singleton until PostgresDbModule
// sees a connection string and swaps in EfFeedbackAttachmentStore. Blob bytes are a
// separate port so a row and its object can fail independently.
// TryLink is a conditional write: it succeeds only for the owner's still-unlinked row, so
// two submits or a submit racing a remove cannot both claim one file. Unlink is the
// compensation when a filing cannot link everything. RemoveExpiredUnlinked deletes rows one
// at a time under the same unlinked condition and returns only the rows it deleted, so the
// caller deletes exactly those blobs.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;

/// <summary>Persistence for feedback attachment rows.</summary>
public interface IFeedbackAttachmentStore
{
  /// <summary>Inserts an attachment. Throws when the id already exists.</summary>
  Task AddAsync(FeedbackAttachment attachment, CancellationToken cancellationToken = default);

  /// <summary>Returns the attachment, or null.</summary>
  Task<FeedbackAttachment?> FindAsync(
    FeedbackAttachmentId id,
    CancellationToken cancellationToken = default);

  /// <summary>Attachments linked to one feedback item, oldest first.</summary>
  Task<IReadOnlyList<FeedbackAttachment>> ListByItemAsync(
    FeedbackItemId itemId,
    CancellationToken cancellationToken = default);

  /// <summary>Unlinked attachments owned by the principal.</summary>
  Task<int> CountUnlinkedByOwnerAsync(
    Guid ownerPrincipalId,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Links the attachment to the item when it exists, is owned by
  /// <paramref name="ownerPrincipalId"/>, and is still unlinked. False otherwise.
  /// </summary>
  Task<bool> TryLinkAsync(
    FeedbackAttachmentId id,
    Guid ownerPrincipalId,
    FeedbackItemId itemId,
    CancellationToken cancellationToken = default);

  /// <summary>Clears the link when the attachment is linked to <paramref name="itemId"/>.</summary>
  Task UnlinkAsync(
    FeedbackAttachmentId id,
    FeedbackItemId itemId,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Deletes the owner's unlinked attachments uploaded before <paramref name="uploadedBefore"/>
  /// and returns the deleted rows so their blobs can be deleted.
  /// </summary>
  Task<IReadOnlyList<FeedbackAttachment>> RemoveExpiredUnlinkedAsync(
    Guid ownerPrincipalId,
    DateTimeOffset uploadedBefore,
    CancellationToken cancellationToken = default);

  /// <summary>Deletes the row. Missing ids are a no-op.</summary>
  Task RemoveAsync(FeedbackAttachmentId id, CancellationToken cancellationToken = default);
}
