#region Purpose
// Marks an API request whose body is a file stream rather than a JSON document.
#endregion

#region Design
// HttpApiService JSON-serializes every POST/PUT/PATCH. A file upload cannot survive that:
// the bytes and the file name would be lost inside a DTO. Implementing this interface opts
// that one request into a raw StreamContent send. The file name travels in a request header
// because the content type is already the file's media type, not multipart metadata.
#endregion

namespace TimeWarp.Foundation.Features;

/// <summary>
/// API request whose HTTP body is the file itself.
/// </summary>
public interface IFileUploadRequest : IApiRequest
{
  /// <summary>File bytes. The transport reads this stream while sending.</summary>
  Stream Content { get; }

  /// <summary>Original file name, without a directory.</summary>
  string FileName { get; }

  /// <summary>Media type of <see cref="Content"/>, without parameters.</summary>
  string ContentType { get; }
}
