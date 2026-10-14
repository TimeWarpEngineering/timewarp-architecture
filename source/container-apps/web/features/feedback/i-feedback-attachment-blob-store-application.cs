#region Purpose
// Application port for feedback attachment bytes.
#endregion

#region Design
// Three implementations, selected Azure, then Postgres, then in-memory (task 352): Azure Blob
// Storage when FeedbackAttachments:ConnectionString is set; Postgres (a bytea table beside the
// attachment rows) when only a database is configured; otherwise the in-memory store for tests,
// mock mode, and apps generated without postgres. Keys are opaque strings produced by
// FeedbackAttachment.Create. OpenRead returns a stream the caller owns, or null when the bytes
// are gone; the download handler reports that as a 404, never a 500.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

/// <summary>Byte store for feedback attachments.</summary>
public interface IFeedbackAttachmentBlobStore
{
  /// <summary>Writes the bytes at <paramref name="key"/>, replacing any previous object.</summary>
  Task PutAsync(
    string key,
    byte[] content,
    string contentType,
    CancellationToken cancellationToken = default);

  /// <summary>Opens the object, or null when it is missing. The caller disposes the stream.</summary>
  Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

  /// <summary>Deletes the object. Missing keys are a no-op.</summary>
  Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
