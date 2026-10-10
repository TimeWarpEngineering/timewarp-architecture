#region Purpose
// Application port for feedback attachment rows. Handlers never take a DbContext.
#endregion

#region Design
// Same dual-mode shape as IFeedbackStore: an in-memory singleton until PostgresDbModule
// sees a connection string and swaps in EfFeedbackAttachmentStore. Blob bytes are a
// separate port so a row and its object can fail independently.
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

  /// <summary>Links an existing attachment to a feedback item.</summary>
  Task LinkAsync(
    FeedbackAttachmentId id,
    FeedbackItemId itemId,
    CancellationToken cancellationToken = default);

  /// <summary>Deletes the row. Missing ids are a no-op.</summary>
  Task RemoveAsync(FeedbackAttachmentId id, CancellationToken cancellationToken = default);
}
