#region Purpose
// Safe Content-Type and Content-Disposition values for an attachment download.
#endregion

#region Design
// The download endpoint writes raw bytes, so the header strings live here where the
// co-located tests can call them without referencing the server project. Images are
// inline so the item page can use img src. PDF and text are attachments. The media type
// is the allow-list spelling, never the request header. The file name is stripped of
// quotes, backslashes, and line breaks before it is placed in the disposition. Kestrel
// refuses a non-ASCII response header, so filename= carries an ASCII fallback with every
// character outside printable ASCII replaced by an underscore, and the real UTF-8 name
// travels only percent-encoded in filename*. DecodeFileNameHeader is the upload binder's
// X-File-Name decoding; it lives here so the co-located tests can run it.
// TooLarge is the 413 problem the upload endpoint writes when Kestrel stops the body first.
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
    string ascii = string.Create(safe.Length, safe, static (span, source) =>
    {
      for (int index = 0; index < source.Length; index++)
      {
        char character = source[index];
        span[index] = character is >= ' ' and <= '~' ? character : '_';
      }
    });
    string kind = IsInline(contentType) ? "inline" : "attachment";
    string encoded = Uri.EscapeDataString(safe);
    return $"{kind}; filename=\"{ascii}\"; filename*=UTF-8''{encoded}";
  }

  /// <summary>The X-File-Name header value, percent-decoded. Empty when the header is empty.</summary>
  public static string DecodeFileNameHeader(string? rawName) =>
    string.IsNullOrEmpty(rawName) ? string.Empty : Uri.UnescapeDataString(rawName);

  /// <summary>The 413 problem for a body over <see cref="FeedbackAttachmentRules.MaxBytes"/>.</summary>
  public static SharedProblemDetails TooLarge() => FeedbackProblems.TooLarge();
}
