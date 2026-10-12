#region Purpose
// Application port for feedback persistence. Handlers never take a DbContext.
#endregion

#region Design
// Dual-mode, same shape as IProfileStore: InMemoryFeedbackStore is the zero-infra singleton;
// EfFeedbackStore replaces it when PostgresDbModule sees a connection string. Remove exists
// only to roll back a filing whose attachments could not all be linked; nothing else deletes
// an item (task 314 decision 6: no admin delete). ListAll is the admin cross-owner query.
// Update persists the triage mutations (status and appended thread entries); the filing
// fields never change. It returns false when another writer saved the item first (the EF
// store's Version concurrency check), so the handler can answer 409 instead of overwriting.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;

public interface IFeedbackStore
{
  Task AddAsync(FeedbackItem item, CancellationToken cancellationToken = default);

  Task<FeedbackItem?> FindAsync(FeedbackItemId id, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<FeedbackItem>> ListByOwnerAsync(
    Guid ownerPrincipalId,
    CancellationToken cancellationToken = default);

  /// <summary>Every owner's items matching the filter, newest first, one page at a time.</summary>
  Task<FeedbackPage> ListAllAsync(FeedbackListFilter filter, CancellationToken cancellationToken = default);

  /// <summary>Saves status and new thread entries. False when the item changed since it was read.</summary>
  Task<bool> UpdateAsync(FeedbackItem item, CancellationToken cancellationToken = default);

  /// <summary>Deletes the item when a filing is rolled back. Missing ids are a no-op.</summary>
  Task RemoveAsync(FeedbackItemId id, CancellationToken cancellationToken = default);
}
