#region Purpose
// Header name used to carry a file name on a raw-body upload.
#endregion

#region Design
// The body is the file, so the name cannot live in multipart disposition. One header keeps
// the client and the server binder on the same wire contract.
#endregion

namespace TimeWarp.Foundation.Features;

/// <summary>HTTP header names for <see cref="IFileUploadRequest"/>.</summary>
public static class FileUploadHeaders
{
  /// <summary>Percent-encoded file name.</summary>
  public const string FileName = "X-File-Name";
}
