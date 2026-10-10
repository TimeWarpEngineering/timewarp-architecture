#region Purpose
// Thread-safe in-memory IFeedbackStore for hosts without Postgres.
#endregion

#region Design
// Process-lifetime singleton, matching InMemoryProfileStore. PostgresDbModule swaps it for
// scoped EfFeedbackStore when a connection string is present. Add throws on a duplicate id.
// List order is FiledAt descending, then id, so the filer's list is stable.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using System.Collections.Concurrent;
using TimeWarp.Architecture.Features.Feedback.Domain;

public sealed class InMemoryFeedbackStore : IFeedbackStore
{
  private readonly ConcurrentDictionary<FeedbackItemId, FeedbackItem> Items = new();

  public Task AddAsync(FeedbackItem item, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(item);
    cancellationToken.ThrowIfCancellationRequested();
    if (!Items.TryAdd(item.Id, item))
    {
      throw new InvalidOperationException($"Feedback item '{item.Id}' already exists.");
    }

    return Task.CompletedTask;
  }

  public Task<FeedbackItem?> FindAsync(FeedbackItemId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    Items.TryGetValue(id, out FeedbackItem? item);
    return Task.FromResult(item);
  }

  public Task<IReadOnlyList<FeedbackItem>> ListByOwnerAsync(
    Guid ownerPrincipalId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var items = Items.Values
      .Where(item => item.OwnerPrincipalId == ownerPrincipalId)
      .OrderByDescending(item => item.FiledAt)
      .ThenBy(item => item.Id.Value)
      .ToList();
    return Task.FromResult<IReadOnlyList<FeedbackItem>>(items);
  }

  public Task RemoveAsync(FeedbackItemId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    Items.TryRemove(id, out _);
    return Task.CompletedTask;
  }
}
