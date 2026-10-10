#region Purpose
// Process-local blob store for tests and hosts without Azure Blob Storage.
#endregion

#region Design
// OpenRead returns a new MemoryStream over a copy so the caller can dispose it without
// touching the stored bytes. Put replaces. Delete of a missing key is a no-op.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using System.Collections.Concurrent;

/// <summary>In-memory bytes for feedback attachments.</summary>
public sealed class InMemoryFeedbackAttachmentBlobStore : IFeedbackAttachmentBlobStore
{
  private readonly ConcurrentDictionary<string, byte[]> Objects = new(StringComparer.Ordinal);

  /// <inheritdoc />
  public Task PutAsync(
    string key,
    byte[] content,
    string contentType,
    CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    ArgumentNullException.ThrowIfNull(content);
    _ = contentType;
    cancellationToken.ThrowIfCancellationRequested();
    Objects[key] = content.ToArray();
    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    if (!Objects.TryGetValue(key, out byte[]? content))
    {
      return Task.FromResult<Stream?>(null);
    }

    return Task.FromResult<Stream?>(new MemoryStream(content.ToArray(), writable: false));
  }

  /// <inheritdoc />
  public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    Objects.TryRemove(key, out _);
    return Task.CompletedTask;
  }
}
