#region Purpose
// Application port for feedback attachment bytes.
#endregion

#region Design
// Azure Blob Storage is the real backing store when FeedbackAttachments:ConnectionString
// is set. Otherwise the in-memory implementation is the process store for tests, mock
// mode, and hosts that have not configured a container. Keys are opaque strings produced
// by FeedbackAttachment.Create. OpenRead returns a stream the caller owns.
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
