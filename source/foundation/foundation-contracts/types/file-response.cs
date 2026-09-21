#region Purpose
// Non-JSON (binary/stream) result arm of IApiService.GetResponse's OneOf.
#endregion

#region Design
// Exists so file downloads flow through the same generic API pipeline as typed DTOs instead of
// a parallel code path; callers distinguish it by pattern-matching the OneOf.
// Wraps the response Stream unbuffered — the consumer decides whether to copy or stream, and
// owns disposal.
#endregion

namespace TimeWarp.Foundation.Types;

/// <summary>
/// Binary / stream download arm of <c>IApiService.GetResponse</c>'s OneOf result.
/// </summary>
public class FileResponse
{
  /// <summary>
  /// Response body stream; the caller owns disposal.
  /// </summary>
  public Stream FileStream { get; }
  /// <summary>
  /// Suggested download file name when the server provided one.
  /// </summary>
  public string? FileName { get; init; }
  /// <summary>
  /// MIME content type of <see cref="FileStream"/> when known.
  /// </summary>
  public string? ContentType { get; init; }

  /// <summary>
  /// Wraps an unbuffered response stream for the file-download OneOf arm.
  /// </summary>
  public FileResponse(Stream fileStream)
  {
    FileStream = fileStream;
  }
}
