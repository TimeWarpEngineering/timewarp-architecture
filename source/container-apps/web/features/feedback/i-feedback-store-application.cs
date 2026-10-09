#region Purpose
// Application port for feedback persistence. Handlers never take a DbContext.
#endregion

#region Design
// Dual-mode, same shape as IProfileStore: InMemoryFeedbackStore is the zero-infra singleton;
// EfFeedbackStore replaces it when PostgresDbModule sees a connection string. Items are
// immutable after File, so stores only add, find, and list.
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
}
