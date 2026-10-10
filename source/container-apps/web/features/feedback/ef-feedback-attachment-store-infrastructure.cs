#region Purpose
// Postgres-backed IFeedbackAttachmentStore.
#endregion

#region Design
// Scoped, depends on PostgresDbContext. PostgresDbModule replaces the in-memory singleton when a
// connection string is present. Find and list are untracked. Remove loads a tracked row.
// TryLink, UnlinkAll, and RemoveExpiredUnlinked are single conditional statements (ExecuteUpdate /
// ExecuteDelete with the owner and link state in the WHERE clause) and read the affected row
// count, so a concurrent submit or remove cannot be overwritten by a stale tracked row.
// A unique id violation becomes InvalidOperationException, matching InMemoryFeedbackAttachmentStore.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Infrastructure;

using Microsoft.EntityFrameworkCore;
using TimeWarp.Architecture.Features.Feedback.Application;
using TimeWarp.Architecture.Features.Feedback.Domain;
using TimeWarp.Architecture.Persistence;

public sealed class EfFeedbackAttachmentStore : IFeedbackAttachmentStore
{
  private readonly PostgresDbContext Db;

  public EfFeedbackAttachmentStore(PostgresDbContext db)
  {
    Db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task AddAsync(FeedbackAttachment attachment, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(attachment);
    cancellationToken.ThrowIfCancellationRequested();
    Db.FeedbackAttachments.Add(attachment);
    try
    {
      await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (DbUpdateException exception) when (IsUniqueViolation(exception))
    {
      Db.Entry(attachment).State = EntityState.Detached;
      throw new InvalidOperationException($"Feedback attachment '{attachment.Id}' already exists.", exception);
    }
  }

  public async Task<FeedbackAttachment?> FindAsync(
    FeedbackAttachmentId id,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return await Db.FeedbackAttachments.AsNoTracking()
      .FirstOrDefaultAsync(attachment => attachment.Id == id, cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<FeedbackAttachment>> ListByItemAsync(
    FeedbackItemId itemId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return await Db.FeedbackAttachments.AsNoTracking()
      .Where(attachment => attachment.FeedbackItemId == itemId)
      .OrderBy(attachment => attachment.UploadedAt)
      .ThenBy(attachment => attachment.Id)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<int> CountUnlinkedByOwnerAsync(
    Guid ownerPrincipalId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return await Db.FeedbackAttachments.AsNoTracking()
      .CountAsync(
        attachment => attachment.OwnerPrincipalId == ownerPrincipalId && attachment.FeedbackItemId == null,
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<bool> TryLinkAsync(
    FeedbackAttachmentId id,
    Guid ownerPrincipalId,
    FeedbackItemId itemId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    FeedbackItemId? link = itemId;
    int affected = await Db.FeedbackAttachments
      .Where(row => row.Id == id && row.OwnerPrincipalId == ownerPrincipalId && row.FeedbackItemId == null)
      .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.FeedbackItemId, link), cancellationToken)
      .ConfigureAwait(false);
    return affected == 1;
  }

  public async Task UnlinkAllAsync(FeedbackItemId itemId, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    FeedbackItemId? linked = itemId;
    await Db.FeedbackAttachments
      .Where(row => row.FeedbackItemId == linked)
      .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.FeedbackItemId, (FeedbackItemId?)null), cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<FeedbackAttachment>> RemoveExpiredUnlinkedAsync(
    Guid ownerPrincipalId,
    DateTimeOffset uploadedBefore,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    List<FeedbackAttachment> candidates = await Db.FeedbackAttachments.AsNoTracking()
      .Where(row => row.OwnerPrincipalId == ownerPrincipalId
        && row.FeedbackItemId == null
        && row.UploadedAt < uploadedBefore)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    List<FeedbackAttachment> removed = [];
    foreach (FeedbackAttachment candidate in candidates)
    {
      FeedbackAttachmentId candidateId = candidate.Id;
      int affected = await Db.FeedbackAttachments
        .Where(row => row.Id == candidateId && row.FeedbackItemId == null)
        .ExecuteDeleteAsync(cancellationToken)
        .ConfigureAwait(false);
      if (affected == 1)
      {
        removed.Add(candidate);
      }
    }

    return removed;
  }

  public async Task RemoveAsync(FeedbackAttachmentId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    FeedbackAttachment? attachment = await Db.FeedbackAttachments
      .FirstOrDefaultAsync(row => row.Id == id, cancellationToken)
      .ConfigureAwait(false);
    if (attachment is null)
    {
      return;
    }

    Db.FeedbackAttachments.Remove(attachment);
    await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private static bool IsUniqueViolation(DbUpdateException exception)
  {
    string? message = exception.InnerException?.Message;
    return message is not null
      && (message.Contains("23505", StringComparison.Ordinal)
        || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase));
  }
}
