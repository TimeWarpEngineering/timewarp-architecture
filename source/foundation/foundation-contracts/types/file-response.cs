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

public class FileResponse
{
  public Stream FileStream { get; }
  public string? FileName { get; init; }
  public string? ContentType { get; init; }

  public FileResponse(Stream fileStream)
  {
    FileStream = fileStream;
  }
}
