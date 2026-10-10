#region Purpose
// Safe Content-Type and Content-Disposition values for an attachment download.
#endregion

#region Design
// The download endpoint writes raw bytes, so the header strings live here where the
// co-located tests can call them without referencing the server project. Images are
// inline so the item page can use img src. PDF and text are attachments. The media type
// is the allow-list spelling, never the request header. The file name is stripped of
// quotes and line breaks before it is placed in the disposition.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using System.Net.Mime;
using TimeWarp.Architecture.Features.Feedback;

/// <summary>Download header values for a feedback attachment.</summary>
public static class FeedbackAttachmentHttp
{
  /// <summary>Allow-listed media type, or application/octet-stream.</summary>
  public static string SafeContentType(string? contentType) =>
    FeedbackAttachmentRules.CanonicalContentType(contentType) ?? MediaTypeNames.Application.Octet;

  /// <summary>True when the browser may display the type inline.</summary>
  public static bool IsInline(string? contentType) =>
    SafeContentType(contentType).StartsWith("image/", StringComparison.Ordinal);

  /// <summary>Content-Disposition with an ASCII filename and an RFC 5987 filename*.</summary>
  public static string ContentDisposition(string fileName, string? contentType)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
    string safe = fileName
      .Replace("\"", string.Empty, StringComparison.Ordinal)
      .Replace("\r", string.Empty, StringComparison.Ordinal)
      .Replace("\n", string.Empty, StringComparison.Ordinal)
      .Replace("\\", string.Empty, StringComparison.Ordinal);
    string kind = IsInline(contentType) ? "inline" : "attachment";
    string encoded = Uri.EscapeDataString(safe);
    return $"{kind}; filename=\"{safe}\"; filename*=UTF-8''{encoded}";
  }
}
