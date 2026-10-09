#region Purpose
// Postgres-backed IFeedbackStore.
#endregion

#region Design
// Scoped, depends on PostgresDbContext. PostgresDbModule replaces the in-memory singleton when a
// connection string is present. Items are insert-only. A unique id violation becomes
// InvalidOperationException, matching InMemoryFeedbackStore.
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
      Db.Entry(item).State = EntityState.Detached;
      throw new InvalidOperationException($"Feedback item '{item.Id}' already exists.", exception);
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

  private static bool IsUniqueViolation(DbUpdateException exception)
  {
    string? message = exception.InnerException?.Message;
    return message is not null
      && (message.Contains("23505", StringComparison.Ordinal)
        || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase));
  }
}
