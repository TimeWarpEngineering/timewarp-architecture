#region Purpose
// Thread-safe in-memory IFeedbackStore for hosts without Postgres.
#endregion

#region Design
// Process-lifetime singleton, matching InMemoryProfileStore. PostgresDbModule swaps it for
// scoped EfFeedbackStore when a connection string is present. Add throws on a duplicate id.
// List order is FiledAt descending, then id, so the filer's list is stable. The id tie-break
// is .NET Guid order, which differs from Postgres uuid order in EfFeedbackStore, so two items
// with the same FiledAt can page in a different order per store; each store is stable alone.
// ListAll sorts per FeedbackListFilter before paging. Title compares ordinally, ignoring case;
// Postgres compares with the database collation, so mixed-case or accented titles can order
// differently per store. Kind and Status compare by name, matching EF's string columns.
// Find hands back the stored instance, so a handler's mutation is visible before Update; Update
// only checks the item still exists. There is no Version check here: this store is the
// zero-infra development path, and concurrent triage of one item is a Postgres concern.
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

  public Task<FeedbackPage> ListAllAsync(FeedbackListFilter filter, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(filter);
    cancellationToken.ThrowIfCancellationRequested();
    IEnumerable<FeedbackItem> matches = Items.Values
      .Where(item => filter.Statuses?.Contains(item.Status) != false)
      .Where(item => filter.Kind is null || item.Kind == filter.Kind)
      .Where(item => filter.Severity is null || item.Severity == filter.Severity)
      .Where(item => filter.FiledFrom is null || item.FiledAt >= filter.FiledFrom)
      .Where(item => filter.FiledTo is null || item.FiledAt < filter.FiledTo)
      .Where(item => string.IsNullOrWhiteSpace(filter.Search)
        || item.Title.Contains(filter.Search.Trim(), StringComparison.OrdinalIgnoreCase));
    List<FeedbackItem> sorted = [.. Sort(matches, filter)];
    List<FeedbackItem> page = [.. sorted.Skip(filter.Skip).Take(filter.Take)];
    return Task.FromResult(new FeedbackPage(page, sorted.Count));
  }

  private static IOrderedEnumerable<FeedbackItem> Sort(IEnumerable<FeedbackItem> items, FeedbackListFilter filter)
  {
    bool descending = filter.SortDescending;
    IOrderedEnumerable<FeedbackItem> ordered = filter.SortBy switch
    {
      FeedbackListSortColumn.Title => By(items, item => item.Title, descending, StringComparer.OrdinalIgnoreCase),
      FeedbackListSortColumn.Kind => By(items, item => item.Kind.ToString(), descending, StringComparer.Ordinal),
      FeedbackListSortColumn.Status => By(items, item => item.Status.ToString(), descending, StringComparer.Ordinal),
      FeedbackListSortColumn.FiledAt or _ => descending
        ? items.OrderByDescending(item => item.FiledAt)
        : items.OrderBy(item => item.FiledAt),
    };
    if (filter.SortBy is not FeedbackListSortColumn.FiledAt)
    {
      ordered = ordered.ThenByDescending(item => item.FiledAt);
    }

    return ordered.ThenBy(item => item.Id.Value);
  }

  private static IOrderedEnumerable<FeedbackItem> By(
    IEnumerable<FeedbackItem> items,
    Func<FeedbackItem, string> key,
    bool descending,
    StringComparer comparer) =>
    descending ? items.OrderByDescending(key, comparer) : items.OrderBy(key, comparer);

  public Task<bool> UpdateAsync(FeedbackItem item, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(item);
    cancellationToken.ThrowIfCancellationRequested();
    if (!Items.ContainsKey(item.Id))
    {
      throw new InvalidOperationException($"Feedback item '{item.Id}' does not exist.");
    }

    Items[item.Id] = item;
    return Task.FromResult(true);
  }

  public Task RemoveAsync(FeedbackItemId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    Items.TryRemove(id, out _);
    return Task.CompletedTask;
  }
}
