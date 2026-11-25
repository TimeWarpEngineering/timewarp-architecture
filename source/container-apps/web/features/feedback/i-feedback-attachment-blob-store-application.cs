#region Purpose
// Application port for feedback attachment bytes.
#endregion

#region Design
// Two stores, selected Azure, then Postgres (tasks 352 and 522): Azure Blob Storage when
// FeedbackAttachments:ConnectionString is set, otherwise Postgres (a bytea table beside the
// attachment rows). There is no in-memory store: a lossy store is what left rows without bytes
// before task 352. A host with neither gets UnavailableFeedbackStorage, which keeps nothing. Keys are opaque strings produced by FeedbackAttachment.Create. OpenRead
// returns a stream the caller owns, or null when the bytes are gone; the download handler
// reports that as a 404, never a 500. Exists answers the same question without reading the
// bytes, so the item views can report a missing file (task 522) without a download per file.
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

  /// <summary>True when the object is stored. Does not read the bytes.</summary>
  Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

  /// <summary>Deletes the object. Missing keys are a no-op.</summary>
  Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
