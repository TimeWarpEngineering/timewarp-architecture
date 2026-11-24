#region Purpose
// The feedback store ports for a host with no database: nothing to read, and every write refused.
#endregion

#region Design
// Task 522 removed the in-memory feedback stores. A host built with the postgres flag but no
// connection string (an unconfigured consumer, the direct-hosted Web.Server integration tests)
// still registers the feedback handlers, and Development validates every registration at build,
// so the ports need an implementation. This is it, the same shape as UnavailableProtectedSecretStore:
// FeedbackModule registers it, and PostgresDbModule replaces it with the EF stores when a
// connection string is present (Azure replaces the byte port when configured).
// Reads are empty: there are no filings. Writes throw so a filing or an upload is never reported
// as saved when nothing keeps it; that is the loss task 352 and task 522 removed. Deletes and
// unlinks of rows that cannot exist are no-ops.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;

/// <summary>No feedback storage on this host. Configure a Postgres connection string to keep filings.</summary>
public sealed class UnavailableFeedbackStorage : IFeedbackStore, IFeedbackAttachmentStore, IFeedbackAttachmentBlobStore
{
  private const string Unavailable =
    "Feedback storage is not available on this host. Configure a Postgres connection string.";

  /// <inheritdoc />
  public Task AddAsync(FeedbackItem item, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(item);
    throw new InvalidOperationException(Unavailable);
  }

  /// <inheritdoc />
  public Task<FeedbackItem?> FindAsync(FeedbackItemId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult<FeedbackItem?>(null);
  }

  /// <inheritdoc />
  public Task<IReadOnlyList<FeedbackItem>> ListByOwnerAsync(
    Guid ownerPrincipalId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult<IReadOnlyList<FeedbackItem>>([]);
  }

  /// <inheritdoc />
  public Task<FeedbackPage> ListAllAsync(FeedbackListFilter filter, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(filter);
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(new FeedbackPage([], 0));
  }

  /// <inheritdoc />
  public Task<bool> UpdateAsync(FeedbackItem item, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(item);
    throw new InvalidOperationException(Unavailable);
  }

  /// <inheritdoc />
  public Task RemoveAsync(FeedbackItemId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task AddAsync(FeedbackAttachment attachment, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(attachment);
    throw new InvalidOperationException(Unavailable);
  }

  /// <inheritdoc />
  public Task<FeedbackAttachment?> FindAsync(FeedbackAttachmentId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult<FeedbackAttachment?>(null);
  }

  /// <inheritdoc />
  public Task<IReadOnlyList<FeedbackAttachment>> ListByItemAsync(
    FeedbackItemId itemId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult<IReadOnlyList<FeedbackAttachment>>([]);
  }

  /// <inheritdoc />
  public Task<int> CountUnlinkedByOwnerAsync(Guid ownerPrincipalId, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(0);
  }

  /// <inheritdoc />
  public Task<bool> TryLinkAsync(
    FeedbackAttachmentId id,
    Guid ownerPrincipalId,
    FeedbackItemId itemId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(false);
  }

  /// <inheritdoc />
  public Task UnlinkAllAsync(FeedbackItemId itemId, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task<IReadOnlyList<FeedbackAttachment>> RemoveExpiredUnlinkedAsync(
    Guid ownerPrincipalId,
    DateTimeOffset uploadedBefore,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult<IReadOnlyList<FeedbackAttachment>>([]);
  }

  /// <inheritdoc />
  public Task RemoveAsync(FeedbackAttachmentId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task PutAsync(
    string key,
    byte[] content,
    string contentType,
    CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ArgumentNullException.ThrowIfNull(content);
    throw new InvalidOperationException(Unavailable);
  }

  /// <inheritdoc />
  public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult<Stream?>(null);
  }

  /// <inheritdoc />
  public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(false);
  }

  /// <inheritdoc />
  public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    cancellationToken.ThrowIfCancellationRequested();
    return Task.CompletedTask;
  }
}
