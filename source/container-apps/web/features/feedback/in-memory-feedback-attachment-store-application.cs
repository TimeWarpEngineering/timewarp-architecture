#region Purpose
// Thread-safe in-memory IFeedbackAttachmentStore for hosts without Postgres.
#endregion

#region Design
// Process-lifetime singleton, matching InMemoryFeedbackStore. PostgresDbModule swaps it
// for scoped EfFeedbackAttachmentStore when a connection string is present. Link mutates
// the stored instance. TryLink, Unlink, and RemoveExpiredUnlinked check and write under one
// lock so the conditional writes are atomic. List order is UploadedAt then id.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using System.Collections.Concurrent;
using TimeWarp.Architecture.Features.Feedback.Domain;

/// <summary>In-memory feedback attachment rows.</summary>
public sealed class InMemoryFeedbackAttachmentStore : IFeedbackAttachmentStore
{
  private readonly ConcurrentDictionary<FeedbackAttachmentId, FeedbackAttachment> Items = new();
  private readonly Lock Gate = new();

  /// <inheritdoc />
  public Task AddAsync(FeedbackAttachment attachment, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(attachment);
    cancellationToken.ThrowIfCancellationRequested();
    if (!Items.TryAdd(attachment.Id, attachment))
    {
      throw new InvalidOperationException($"Feedback attachment '{attachment.Id}' already exists.");
    }

    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task<FeedbackAttachment?> FindAsync(
    FeedbackAttachmentId id,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    Items.TryGetValue(id, out FeedbackAttachment? attachment);
    return Task.FromResult(attachment);
  }

  /// <inheritdoc />
  public Task<IReadOnlyList<FeedbackAttachment>> ListByItemAsync(
    FeedbackItemId itemId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var items = Items.Values
      .Where(attachment => attachment.FeedbackItemId == itemId)
      .OrderBy(attachment => attachment.UploadedAt)
      .ThenBy(attachment => attachment.Id.Value)
      .ToList();
    return Task.FromResult<IReadOnlyList<FeedbackAttachment>>(items);
  }

  /// <inheritdoc />
  public Task<int> CountUnlinkedByOwnerAsync(
    Guid ownerPrincipalId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    int count = Items.Values.Count(attachment =>
      attachment.OwnerPrincipalId == ownerPrincipalId && attachment.FeedbackItemId is null);
    return Task.FromResult(count);
  }

  /// <inheritdoc />
  public Task<bool> TryLinkAsync(
    FeedbackAttachmentId id,
    Guid ownerPrincipalId,
    FeedbackItemId itemId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    lock (Gate)
    {
      if (!Items.TryGetValue(id, out FeedbackAttachment? attachment)
        || attachment.OwnerPrincipalId != ownerPrincipalId
        || attachment.FeedbackItemId is not null)
      {
        return Task.FromResult(false);
      }

      attachment.Link(itemId);
      return Task.FromResult(true);
    }
  }

  /// <inheritdoc />
  public Task UnlinkAsync(
    FeedbackAttachmentId id,
    FeedbackItemId itemId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    lock (Gate)
    {
      if (Items.TryGetValue(id, out FeedbackAttachment? attachment) && attachment.FeedbackItemId == itemId)
      {
        attachment.Unlink(itemId);
      }
    }

    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task<IReadOnlyList<FeedbackAttachment>> RemoveExpiredUnlinkedAsync(
    Guid ownerPrincipalId,
    DateTimeOffset uploadedBefore,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    List<FeedbackAttachment> removed = [];
    lock (Gate)
    {
      foreach (FeedbackAttachment attachment in Items.Values)
      {
        if (attachment.OwnerPrincipalId == ownerPrincipalId
          && attachment.FeedbackItemId is null
          && attachment.UploadedAt < uploadedBefore
          && Items.TryRemove(attachment.Id, out _))
        {
          removed.Add(attachment);
        }
      }
    }

    return Task.FromResult<IReadOnlyList<FeedbackAttachment>>(removed);
  }

  /// <inheritdoc />
  public Task RemoveAsync(FeedbackAttachmentId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    lock (Gate)
    {
      Items.TryRemove(id, out _);
    }

    return Task.CompletedTask;
  }
}
