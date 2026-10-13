#region Purpose
// Postgres-backed IFeedbackStore.
#endregion

#region Design
// Scoped, depends on PostgresDbContext. PostgresDbModule replaces the in-memory singleton when a
// connection string is present. Remove rolls back a filing whose attachments could not be
// linked. A unique id violation becomes InvalidOperationException, matching
// InMemoryFeedbackStore. Reads are AsNoTracking, so Update attaches the detached item: the
// filing columns stay unchanged, Status is marked modified, and only thread entries whose
// Sequence is above the stored maximum are inserted (the thread is append-only). The root is
// Modified, so AggregateDbContext runs the invariants and the Version check; a concurrency
// conflict or a duplicate (item, sequence) key from a racing writer returns false. Add and
// Update both clear the change tracker when they finish, so no tracked instance outlives a call
// and a later Update in the same scope can attach its detached copy (task 314 review).
// ListAll orders in SQL before Skip/Take (task 327). Kind and Status are text columns, so they
// order by name. No index was added for the new sorts: the admin list filters by status first
// (the (Status, FiledAt) index), a feedback table stays small enough for an in-memory sort of
// the match set, and an index per sortable column would tax every filing for a rare view.
// Search (task 345) is a parameterized lower(title) LIKE '%term%' with the term lower-cased by
// ToLowerInvariant on the client. Postgres lower() folds by the database collation while
// InMemoryFeedbackStore uses OrdinalIgnoreCase, so non-ASCII titles (Turkish dotted I, sharp s)
// can match differently between the two stores. Accepted: Search narrows an @ picker or an admin
// list, not an authorization or uniqueness check, and ASCII titles match the same in both.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Infrastructure;

using Microsoft.EntityFrameworkCore;
using TimeWarp.Architecture.Features.Feedback.Application;
using TimeWarp.Architecture.Features.Feedback.Domain;
using TimeWarp.Architecture.Persistence;

public sealed class EfFeedbackStore : IFeedbackStore
{
  private readonly PostgresDbContext Db;

  public EfFeedbackStore(PostgresDbContext db)
  {
    Db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task AddAsync(FeedbackItem item, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(item);
    cancellationToken.ThrowIfCancellationRequested();
    Db.FeedbackItems.Add(item);
    try
    {
      await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (DbUpdateException exception) when (IsUniqueViolation(exception))
    {
      throw new InvalidOperationException($"Feedback item '{item.Id}' already exists.", exception);
    }
    finally
    {
      Db.ChangeTracker.Clear();
    }
  }

  public async Task<FeedbackItem?> FindAsync(FeedbackItemId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return await Db.FeedbackItems.AsNoTracking()
      .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<FeedbackItem>> ListByOwnerAsync(
    Guid ownerPrincipalId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return await Db.FeedbackItems.AsNoTracking()
      .Where(item => item.OwnerPrincipalId == ownerPrincipalId)
      .OrderByDescending(item => item.FiledAt)
      .ThenBy(item => item.Id)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<FeedbackPage> ListAllAsync(FeedbackListFilter filter, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(filter);
    cancellationToken.ThrowIfCancellationRequested();
    IQueryable<FeedbackItem> query = Db.FeedbackItems.AsNoTracking();
    if (filter.Statuses is not null)
    {
      List<FeedbackStatus> statuses = [.. filter.Statuses];
      query = query.Where(item => statuses.Contains(item.Status));
    }

    if (filter.Kind is { } kind)
    {
      query = query.Where(item => item.Kind == kind);
    }

    if (filter.FiledFrom is { } from)
    {
      query = query.Where(item => item.FiledAt >= from);
    }

    if (filter.FiledTo is { } to)
    {
      query = query.Where(item => item.FiledAt < to);
    }

    if (!string.IsNullOrWhiteSpace(filter.Search))
    {
      string lowered = filter.Search.Trim().ToLowerInvariant();
      // CA1862 wants a StringComparison overload, which EF cannot translate to SQL; lower-cased
      // contains is the translatable form (lower(x) LIKE '%term%').
#pragma warning disable CA1862
      query = query.Where(item => item.Title.ToLower().Contains(lowered));
#pragma warning restore CA1862
    }

    int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
    List<FeedbackItem> page = await Sort(query, filter)
      .Skip(filter.Skip)
      .Take(filter.Take)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    return new FeedbackPage(page, total);
  }

  private static IOrderedQueryable<FeedbackItem> Sort(IQueryable<FeedbackItem> query, FeedbackListFilter filter)
  {
    bool descending = filter.SortDescending;
    IOrderedQueryable<FeedbackItem> ordered = filter.SortBy switch
    {
      FeedbackListSortColumn.Title => descending
        ? query.OrderByDescending(item => item.Title)
        : query.OrderBy(item => item.Title),
      FeedbackListSortColumn.Kind => descending
        ? query.OrderByDescending(item => item.Kind)
        : query.OrderBy(item => item.Kind),
      FeedbackListSortColumn.Status => descending
        ? query.OrderByDescending(item => item.Status)
        : query.OrderBy(item => item.Status),
      FeedbackListSortColumn.FiledAt or _ => descending
        ? query.OrderByDescending(item => item.FiledAt)
        : query.OrderBy(item => item.FiledAt),
    };
    if (filter.SortBy is not FeedbackListSortColumn.FiledAt)
    {
      ordered = ordered.ThenByDescending(item => item.FiledAt);
    }

    return ordered.ThenBy(item => item.Id);
  }

  public async Task<bool> UpdateAsync(FeedbackItem item, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(item);
    cancellationToken.ThrowIfCancellationRequested();
    int storedMaxSequence = await Db.FeedbackItems.AsNoTracking()
      .Where(row => row.Id == item.Id)
      .SelectMany(row => row.Activity)
      .Select(entry => (int?)entry.Sequence)
      .MaxAsync(cancellationToken)
      .ConfigureAwait(false) ?? 0;

    Db.FeedbackItems.Attach(item);
    foreach (FeedbackActivity entry in item.Activity)
    {
      if (entry.Sequence > storedMaxSequence)
      {
        Db.Entry(entry).State = EntityState.Added;
      }
    }

    Db.Entry(item).Property(row => row.Status).IsModified = true;
    try
    {
      await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
      return true;
    }
    catch (DbUpdateConcurrencyException)
    {
      return false;
    }
    catch (DbUpdateException exception) when (IsUniqueViolation(exception))
    {
      return false;
    }
    finally
    {
      Db.ChangeTracker.Clear();
    }
  }

  public async Task RemoveAsync(FeedbackItemId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    await Db.FeedbackItems
      .Where(item => item.Id == id)
      .ExecuteDeleteAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  private static bool IsUniqueViolation(DbUpdateException exception)
  {
    string? message = exception.InnerException?.Message;
    return message is not null
      && (message.Contains("23505", StringComparison.Ordinal)
        || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase));
  }
}
