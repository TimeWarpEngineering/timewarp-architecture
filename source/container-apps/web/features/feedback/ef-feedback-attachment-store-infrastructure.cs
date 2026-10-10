#region Purpose
// Postgres-backed IFeedbackAttachmentStore.
#endregion

#region Design
// Scoped, depends on PostgresDbContext. PostgresDbModule replaces the in-memory singleton when a
// connection string is present. Find and list are untracked. Link and remove load a tracked row.
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

  public async Task LinkAsync(
    FeedbackAttachmentId id,
    FeedbackItemId itemId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    FeedbackAttachment? attachment = await Db.FeedbackAttachments
      .FirstOrDefaultAsync(row => row.Id == id, cancellationToken)
      .ConfigureAwait(false);
    if (attachment is null)
    {
      throw new InvalidOperationException($"Feedback attachment '{id}' does not exist.");
    }

    attachment.Link(itemId);
    await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
