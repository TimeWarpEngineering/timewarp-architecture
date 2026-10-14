#region Purpose
// Postgres-backed IFeedbackAttachmentBlobStore for hosts without Azure Blob Storage.
#endregion

#region Design
// Scoped, depends on PostgresDbContext. PostgresDbModule registers it when a connection string
// is present and FeedbackAttachments:ConnectionString is empty, so mode selection is Azure, then
// Postgres, then in-memory. Each call commits on its own: upload writes the bytes before the
// attachment row and deletes them if the row insert throws, and remove deletes the row before
// the bytes, so "row exists" implies "bytes exist". A failure between the two calls leaves
// unreferenced bytes, never a row without bytes.
// Put tries an ExecuteUpdate first and inserts when no row matched, so it replaces like the
// other stores without loading the old bytes. The inserted entity is detached after save so a
// scoped context does not keep up to 5 MiB tracked for the rest of the request.
// Reads are not streamed. EF materializes bytea as byte[]; streaming would need a raw
// NpgsqlDataReader with CommandBehavior.SequentialAccess and a stream that owns the reader,
// command, and connection. FeedbackAttachmentRules.MaxBytes caps a value at 5 MiB, so OpenRead
// loads the column untracked and returns a read-only MemoryStream (seekable, so the download
// endpoint sets Content-Length). Revisit if the size cap grows.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Infrastructure;

using Microsoft.EntityFrameworkCore;
using TimeWarp.Architecture.Features.Feedback.Application;
using TimeWarp.Architecture.Persistence;

/// <summary>Postgres bytes for feedback attachments.</summary>
public sealed class EfFeedbackAttachmentBlobStore : IFeedbackAttachmentBlobStore
{
  private readonly PostgresDbContext Db;

  public EfFeedbackAttachmentBlobStore(PostgresDbContext db)
  {
    Db = db ?? throw new ArgumentNullException(nameof(db));
  }

  /// <inheritdoc />
  public async Task PutAsync(
    string key,
    byte[] content,
    string contentType,
    CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ArgumentNullException.ThrowIfNull(content);
    _ = contentType;
    cancellationToken.ThrowIfCancellationRequested();
    int replaced = await Db.FeedbackAttachmentBytes
      .Where(row => row.StorageKey == key)
      .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Content, content), cancellationToken)
      .ConfigureAwait(false);
    if (replaced > 0)
    {
      return;
    }

    FeedbackAttachmentBytes bytes = new(key, content);
    Db.FeedbackAttachmentBytes.Add(bytes);
    try
    {
      await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
    finally
    {
      Db.Entry(bytes).State = EntityState.Detached;
    }
  }

  /// <inheritdoc />
  public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    cancellationToken.ThrowIfCancellationRequested();
    byte[]? content = await Db.FeedbackAttachmentBytes.AsNoTracking()
      .Where(row => row.StorageKey == key)
      .Select(row => row.Content)
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);
    return content is null ? null : new MemoryStream(content, writable: false);
  }

  /// <inheritdoc />
  public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    cancellationToken.ThrowIfCancellationRequested();
    await Db.FeedbackAttachmentBytes
      .Where(row => row.StorageKey == key)
      .ExecuteDeleteAsync(cancellationToken)
      .ConfigureAwait(false);
  }
}
