#region Purpose
// Azure Blob Storage implementation of IFeedbackAttachmentBlobStore.
#endregion

#region Design
// One container client, created from FeedbackAttachments:ConnectionString. The container is
// created on first use, private, and the create is serialized so concurrent uploads share it.
// BlobContainerClient itself is safe to share. Keys are the strings FeedbackAttachment.Create
// assigns. Missing objects read as null and delete as a no-op.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Infrastructure;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using TimeWarp.Architecture.Features.Feedback.Application;

/// <summary>Azure Blob Storage bytes for feedback attachments.</summary>
public sealed class AzureFeedbackAttachmentBlobStore : IFeedbackAttachmentBlobStore, IDisposable
{
  private readonly BlobContainerClient Container;
  private readonly SemaphoreSlim Gate = new(1, 1);
  private int Ready;

  public AzureFeedbackAttachmentBlobStore(string connectionString, string containerName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
    ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
    Container = new BlobContainerClient(connectionString, containerName);
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
    ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
    await EnsureContainerAsync(cancellationToken).ConfigureAwait(false);
    BlobClient blob = Container.GetBlobClient(key);
    await using MemoryStream stream = new(content, writable: false);
    BlobUploadOptions options = new()
    {
      HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
    };
    await blob.UploadAsync(stream, options, cancellationToken).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    await EnsureContainerAsync(cancellationToken).ConfigureAwait(false);
    BlobClient blob = Container.GetBlobClient(key);
    Azure.Response<bool> exists = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
    if (!exists.Value)
    {
      return null;
    }

    return await blob.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    await EnsureContainerAsync(cancellationToken).ConfigureAwait(false);
    BlobClient blob = Container.GetBlobClient(key);
    await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
  }

  private async Task EnsureContainerAsync(CancellationToken cancellationToken)
  {
    if (Volatile.Read(ref Ready) == 1)
    {
      return;
    }

    await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      if (Ready == 1)
      {
        return;
      }

      await Container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken)
        .ConfigureAwait(false);
      Volatile.Write(ref Ready, 1);
    }
    finally
    {
      Gate.Release();
    }
  }

  /// <inheritdoc />
  public void Dispose() => Gate.Dispose();
}
