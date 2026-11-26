#region Purpose
// Endpoint metadata: the media types and limits of a file an endpoint reads or writes as raw bytes.
#endregion

#region Design
// OpenApiBinaryContent publishes a file body as binary. It finds those endpoints from the contract
// markers (IFileUploadRequest, a Stream on the response), but the values it needs live in the
// feature: foundation-server cannot reference a feature's rules class. The endpoint adds this
// metadata from those rules, so the document and the handler read the same constants.
// ContentTypes is the download's media types. An upload's media types are its accepts metadata,
// which is also what the host checks for 415, so the upload does not repeat them here.
// MaxFileNameLength is the decoded X-File-Name limit. MaxBytes is the file size limit the 413
// names. Either is null when the endpoint has none.
#endregion

namespace TimeWarp.Foundation;

/// <summary>
/// Media types and limits of a raw file body, for the OpenAPI document.
/// </summary>
/// <param name="ContentTypes">Media types a download may carry. Empty for an upload.</param>
/// <param name="MaxFileNameLength">Longest file name after percent-decoding, or null.</param>
/// <param name="MaxBytes">Largest accepted file in bytes, or null.</param>
public sealed record FileContentMetadata
(
  IReadOnlyList<string> ContentTypes,
  int? MaxFileNameLength = null,
  long? MaxBytes = null
);
